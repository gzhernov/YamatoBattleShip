using System;
using UnityEngine;
using UnityEngine.Serialization;

public class ShipStatuses : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private ShipConfig shipConfig;

    [Header("Debug")]
    [SerializeField] private bool logChanges = true;

    [Header("Rudder Command (Read Only)")]
    [SerializeField, ReadOnlyInspector] private int rudderPosition;
    [SerializeField, ReadOnlyInspector] private float targetRudderNormalizedValue;
    [SerializeField, ReadOnlyInspector] private float targetRudderSignedValue;

    [Header("Actual Rudder (Read Only)")]
    [SerializeField, ReadOnlyInspector] private float actualRudderNormalizedValue;
    [SerializeField, ReadOnlyInspector] private float actualRudderSignedValue;

    [Space]
    [SerializeField, ReadOnlyInspector] private EngineTelegraphSector engineTelegraphSector;
    [SerializeField, ReadOnlyInspector] private int engineTelegraphSectorIndex;
    [SerializeField, ReadOnlyInspector] private string engineTelegraphSectorName = "UNKNOWN";
    [SerializeField, ReadOnlyInspector] private int engineTelegraphReducerPercent = 100;

    [FormerlySerializedAs("engineTelegraphSpeedValue")]
    [SerializeField, ReadOnlyInspector] private float engineTelegraphSpeedKnots;

    [SerializeField, ReadOnlyInspector] private float engineTelegraphAngle;

    [Header("Navigation Status (Read Only)")]
    [SerializeField, ReadOnlyInspector] private float currentSpeedKnots;
    [SerializeField, ReadOnlyInspector] private float currentCourseDegrees;

    public ShipConfig ShipConfig => shipConfig;

    public int RudderPosition => rudderPosition;
    public int RudderTotalPositions => shipConfig != null ? shipConfig.RudderTotalPositions : 2;

    // Backward-compatible names now mean the physical/current rudder value.
    public float RudderNormalizedValue => actualRudderNormalizedValue;
    public float RudderSignedValue => actualRudderSignedValue;

    public float TargetRudderNormalizedValue => targetRudderNormalizedValue;
    public float TargetRudderSignedValue => targetRudderSignedValue;
    public float ActualRudderNormalizedValue => actualRudderNormalizedValue;
    public float ActualRudderSignedValue => actualRudderSignedValue;
    public bool IsRudderMoving => !Mathf.Approximately(actualRudderSignedValue, targetRudderSignedValue);

    public EngineTelegraphConfig EngineTelegraphConfig => shipConfig != null ? shipConfig.EngineTelegraphConfig : null;
    public EngineTelegraphSector CurrentEngineTelegraphSector => engineTelegraphSector;
    public int EngineTelegraphSectorCount => EngineTelegraphConfig == null ? 0 : EngineTelegraphConfig.SectorCount;

    public int EngineTelegraphSectorIndex => engineTelegraphSectorIndex;
    public string EngineTelegraphSectorName => engineTelegraphSectorName;
    public int EngineTelegraphReducerPercent => engineTelegraphReducerPercent;
    public float EngineTelegraphSpeedKnots => engineTelegraphSpeedKnots;
    public float EngineTelegraphAngle => engineTelegraphAngle;
    public float CurrentSpeedKnots => currentSpeedKnots;
    public float CurrentCourseDegrees => currentCourseDegrees;

    [Obsolete("Use EngineTelegraphSpeedKnots instead.")]
    public float EngineTelegraphSpeedValue => engineTelegraphSpeedKnots;

    public event Action<int> OnRudderPositionChanged;
    public event Action<float> OnTargetRudderValueChanged;
    public event Action<float> OnActualRudderValueChanged;
    public event Action<EngineTelegraphSector, EngineTelegraphSectorData> OnEngineTelegraphChanged;
    public event Action<int> OnEngineTelegraphReducerChanged;
    public event Action<float, float> OnNavigationStatusChanged;

    private void Awake()
    {
        InitializeRudderStatus();
        InitializeEngineTelegraphStatus();
        RefreshNavigationRuntimeStatus();
    }

    private void Update()
    {
        UpdateActualRudder(Time.deltaTime);
    }

    private void InitializeRudderStatus()
    {
        if (shipConfig == null)
        {
            Debug.LogError("ShipStatuses: ShipConfig не назначен.", this);
            rudderPosition = 0;
            RefreshTargetRudderRuntimeStatus();
            SetActualRudderImmediate(targetRudderSignedValue, false);
            return;
        }

        rudderPosition = Mathf.Clamp(
            shipConfig.RudderDefaultPosition,
            0,
            shipConfig.RudderTotalPositions - 1
        );

        RefreshTargetRudderRuntimeStatus();
        SetActualRudderImmediate(targetRudderSignedValue, false);
    }

    private void InitializeEngineTelegraphStatus()
    {
        EngineTelegraphConfig engineTelegraphConfig = EngineTelegraphConfig;
        engineTelegraphReducerPercent = 100;

        if (shipConfig == null)
        {
            Debug.LogError("ShipStatuses: ShipConfig не назначен.", this);
            RefreshEngineTelegraphRuntimeStatus();
            return;
        }

        if (engineTelegraphConfig == null)
        {
            Debug.LogError("ShipStatuses: EngineTelegraphConfig не назначен в ShipConfig.", shipConfig);
            RefreshEngineTelegraphRuntimeStatus();
            return;
        }

        if (!engineTelegraphConfig.HasSectors)
        {
            Debug.LogError("ShipStatuses: в EngineTelegraphConfig нет секторов.", engineTelegraphConfig);
            RefreshEngineTelegraphRuntimeStatus();
            return;
        }

        engineTelegraphSector = engineTelegraphConfig.GetValidOrFallbackSector(
            shipConfig.InitialEngineTelegraphSector
        );

        RefreshEngineTelegraphRuntimeStatus();
    }

    public void SetRudderPosition(int newPosition)
    {
        if (shipConfig == null)
        {
            Debug.LogError("ShipStatuses: нельзя изменить руль — ShipConfig не назначен.", this);
            return;
        }

        int clampedPosition = Mathf.Clamp(newPosition, 0, shipConfig.RudderTotalPositions - 1);

        if (rudderPosition == clampedPosition)
            return;

        rudderPosition = clampedPosition;
        RefreshTargetRudderRuntimeStatus();

        if (!Application.isPlaying)
        {
            SetActualRudderImmediate(targetRudderSignedValue, false);
        }

        if (logChanges)
        {
            Debug.Log(
                $"ShipStatuses: команда руля -> позиция {rudderPosition}, " +
                $"target {targetRudderSignedValue:F2}",
                this
            );
        }

        OnRudderPositionChanged?.Invoke(rudderPosition);
        OnTargetRudderValueChanged?.Invoke(targetRudderSignedValue);
    }

    public void MoveRudderLeft()
    {
        SetRudderPosition(rudderPosition - 1);
    }

    public void MoveRudderRight()
    {
        SetRudderPosition(rudderPosition + 1);
    }

    public void ResetRudderToCenter()
    {
        if (shipConfig == null)
            return;

        int centerPosition = Mathf.FloorToInt((shipConfig.RudderTotalPositions - 1) / 2f);
        SetRudderPosition(centerPosition);
    }

    public float GetRudderNormalizedValue()
    {
        return GetActualRudderNormalizedValue();
    }

    public float GetRudderSignedValue()
    {
        return GetActualRudderSignedValue();
    }

    public float GetTargetRudderNormalizedValue()
    {
        if (shipConfig == null || shipConfig.RudderTotalPositions <= 1)
            return 0.5f;

        return (float)rudderPosition / (shipConfig.RudderTotalPositions - 1);
    }

    public float GetTargetRudderSignedValue()
    {
        return Mathf.Lerp(-1f, 1f, GetTargetRudderNormalizedValue());
    }

    public float GetActualRudderNormalizedValue()
    {
        return Mathf.InverseLerp(-1f, 1f, actualRudderSignedValue);
    }

    public float GetActualRudderSignedValue()
    {
        return actualRudderSignedValue;
    }

    public void IncreaseEngineTelegraphSector()
    {
        SetEngineTelegraphSectorByIndex(GetEngineTelegraphSectorIndex(engineTelegraphSector) + 1);
    }

    public void DecreaseEngineTelegraphSector()
    {
        SetEngineTelegraphSectorByIndex(GetEngineTelegraphSectorIndex(engineTelegraphSector) - 1);
    }

    public void SetEngineTelegraphSector(EngineTelegraphSector newSector)
    {
        EngineTelegraphConfig engineTelegraphConfig = EngineTelegraphConfig;

        if (engineTelegraphConfig == null)
        {
            Debug.LogError("ShipStatuses: EngineTelegraphConfig не назначен.", this);
            return;
        }

        EngineTelegraphSectorData sectorData = engineTelegraphConfig.GetSectorData(newSector);

        if (sectorData == null)
        {
            Debug.LogWarning($"ShipStatuses: сектор машинного телеграфа не найден в конфиге: {newSector}", engineTelegraphConfig);
            return;
        }

        if (engineTelegraphSector == newSector)
            return;

        engineTelegraphSector = newSector;
        RefreshEngineTelegraphRuntimeStatus();

        if (logChanges)
        {
            Debug.Log($"ShipStatuses: машинный телеграф -> {sectorData.displayName}, скорость {sectorData.speedKnots:F1} узл.", this);
        }

        OnEngineTelegraphChanged?.Invoke(engineTelegraphSector, sectorData);
    }

    public void SetEngineTelegraphReducerPercent(int percent)
    {
        int clampedPercent = Mathf.Clamp(percent, 0, 100);

        if (engineTelegraphReducerPercent == clampedPercent)
            return;

        engineTelegraphReducerPercent = clampedPercent;

        if (logChanges)
        {
            Debug.Log(
                $"ShipStatuses: редуктор телеграфа -> {engineTelegraphReducerPercent}%",
                this
            );
        }

        OnEngineTelegraphReducerChanged?.Invoke(engineTelegraphReducerPercent);
    }

    public void SetEngineTelegraphSectorByIndex(int index)
    {
        EngineTelegraphConfig engineTelegraphConfig = EngineTelegraphConfig;

        if (engineTelegraphConfig == null || !engineTelegraphConfig.HasSectors)
            return;

        EngineTelegraphSectorData sectorData = engineTelegraphConfig.GetSectorDataByIndex(index);

        if (sectorData != null)
        {
            SetEngineTelegraphSector(sectorData.sector);
        }
    }

    public void SetEngineTelegraphSectorByAngle(float angle)
    {
        EngineTelegraphSectorData closestSectorData = GetClosestEngineTelegraphSectorByAngle(angle);

        if (closestSectorData != null)
        {
            SetEngineTelegraphSector(closestSectorData.sector);
        }
    }

    public void SetEngineTelegraphSectorBySpeedKnots(float speedKnots)
    {
        EngineTelegraphSectorData closestSectorData = GetClosestEngineTelegraphSectorBySpeedKnots(speedKnots);

        if (closestSectorData != null)
        {
            SetEngineTelegraphSector(closestSectorData.sector);
        }
    }

    [Obsolete("Use SetEngineTelegraphSectorBySpeedKnots instead.")]
    public void SetEngineTelegraphSectorBySpeed(float speedValue)
    {
        SetEngineTelegraphSectorBySpeedKnots(speedValue);
    }

    public EngineTelegraphSectorData GetCurrentEngineTelegraphSectorData()
    {
        return GetEngineTelegraphSectorData(engineTelegraphSector);
    }

    public EngineTelegraphSectorData GetEngineTelegraphSectorData(EngineTelegraphSector sector)
    {
        EngineTelegraphConfig engineTelegraphConfig = EngineTelegraphConfig;

        if (engineTelegraphConfig == null)
            return null;

        return engineTelegraphConfig.GetSectorData(sector);
    }

    public int GetEngineTelegraphSectorIndex(EngineTelegraphSector sector)
    {
        EngineTelegraphConfig engineTelegraphConfig = EngineTelegraphConfig;

        if (engineTelegraphConfig == null)
            return -1;

        return engineTelegraphConfig.GetSectorIndex(sector);
    }

    public float GetCurrentEngineTelegraphAngle()
    {
        EngineTelegraphSectorData sectorData = GetCurrentEngineTelegraphSectorData();
        return sectorData != null ? sectorData.angle : 0f;
    }

    public float GetCurrentEngineTelegraphSpeedKnots()
    {
        EngineTelegraphSectorData sectorData = GetCurrentEngineTelegraphSectorData();
        return sectorData != null ? sectorData.speedKnots : 0f;
    }

    [Obsolete("Use GetCurrentEngineTelegraphSpeedKnots instead.")]
    public float GetCurrentEngineTelegraphSpeedValue()
    {
        return GetCurrentEngineTelegraphSpeedKnots();
    }

    public string GetCurrentEngineTelegraphSectorName()
    {
        EngineTelegraphSectorData sectorData = GetCurrentEngineTelegraphSectorData();
        return sectorData != null ? sectorData.displayName : "UNKNOWN";
    }

    public void SetNavigationStatus(float speedKnots, float courseDegrees)
    {
        float normalizedCourse = NormalizeCourseDegrees(courseDegrees);

        if (Mathf.Approximately(currentSpeedKnots, speedKnots)
            && Mathf.Approximately(currentCourseDegrees, normalizedCourse))
        {
            return;
        }

        currentSpeedKnots = speedKnots;
        currentCourseDegrees = normalizedCourse;
        OnNavigationStatusChanged?.Invoke(currentSpeedKnots, currentCourseDegrees);
    }

    private void UpdateActualRudder(float deltaTime)
    {
        if (shipConfig == null)
            return;

        float previousActualRudderSignedValue = actualRudderSignedValue;
        float shiftSpeed = shipConfig.RudderShiftSpeedSignedUnitsPerSecond;

        if (float.IsInfinity(shiftSpeed) || shiftSpeed <= 0f)
        {
            actualRudderSignedValue = targetRudderSignedValue;
        }
        else
        {
            actualRudderSignedValue = Mathf.MoveTowards(
                actualRudderSignedValue,
                targetRudderSignedValue,
                shiftSpeed * deltaTime
            );
        }

        RefreshActualRudderRuntimeStatus();

        if (!Mathf.Approximately(previousActualRudderSignedValue, actualRudderSignedValue))
        {
            OnActualRudderValueChanged?.Invoke(actualRudderSignedValue);
        }
    }

    private void SetActualRudderImmediate(float signedValue, bool notify)
    {
        actualRudderSignedValue = Mathf.Clamp(signedValue, -1f, 1f);
        RefreshActualRudderRuntimeStatus();

        if (notify)
        {
            OnActualRudderValueChanged?.Invoke(actualRudderSignedValue);
        }
    }

    private EngineTelegraphSectorData GetClosestEngineTelegraphSectorByAngle(float angle)
    {
        EngineTelegraphConfig engineTelegraphConfig = EngineTelegraphConfig;

        if (engineTelegraphConfig == null)
            return null;

        return engineTelegraphConfig.GetClosestSectorByAngle(angle);
    }

    private EngineTelegraphSectorData GetClosestEngineTelegraphSectorBySpeedKnots(float speedKnots)
    {
        EngineTelegraphConfig engineTelegraphConfig = EngineTelegraphConfig;

        if (engineTelegraphConfig == null)
            return null;

        return engineTelegraphConfig.GetClosestSectorBySpeedKnots(speedKnots);
    }

    private void RefreshTargetRudderRuntimeStatus()
    {
        targetRudderNormalizedValue = GetTargetRudderNormalizedValue();
        targetRudderSignedValue = GetTargetRudderSignedValue();
    }

    private void RefreshActualRudderRuntimeStatus()
    {
        actualRudderSignedValue = Mathf.Clamp(actualRudderSignedValue, -1f, 1f);
        actualRudderNormalizedValue = GetActualRudderNormalizedValue();
    }

    private void RefreshEngineTelegraphRuntimeStatus()
    {
        EngineTelegraphSectorData sectorData = GetCurrentEngineTelegraphSectorData();

        if (sectorData == null)
        {
            engineTelegraphSectorIndex = -1;
            engineTelegraphSectorName = "UNKNOWN";
            engineTelegraphSpeedKnots = 0f;
            engineTelegraphAngle = 0f;
            return;
        }

        engineTelegraphSectorIndex = GetEngineTelegraphSectorIndex(engineTelegraphSector);
        engineTelegraphSectorName = sectorData.displayName;
        engineTelegraphSpeedKnots = sectorData.speedKnots;
        engineTelegraphAngle = sectorData.angle;
    }

    private void RefreshNavigationRuntimeStatus()
    {
        currentSpeedKnots = 0f;
        currentCourseDegrees = NormalizeCourseDegrees(transform.eulerAngles.y);
    }

    private static float NormalizeCourseDegrees(float courseDegrees)
    {
        float normalizedCourse = Mathf.Repeat(courseDegrees, 360f);
        return Mathf.Approximately(normalizedCourse, 360f) ? 0f : normalizedCourse;
    }

    private void OnValidate()
    {
        if (shipConfig == null)
            return;

        if (!Application.isPlaying)
        {
            rudderPosition = Mathf.Clamp(
                shipConfig.RudderDefaultPosition,
                0,
                shipConfig.RudderTotalPositions - 1
            );

            RefreshTargetRudderRuntimeStatus();
            SetActualRudderImmediate(targetRudderSignedValue, false);

            EngineTelegraphConfig engineTelegraphConfig = shipConfig.EngineTelegraphConfig;

            if (engineTelegraphConfig != null && engineTelegraphConfig.HasSectors)
            {
                engineTelegraphSector = engineTelegraphConfig.GetValidOrFallbackSector(
                    shipConfig.InitialEngineTelegraphSector
                );
            }
        }

        engineTelegraphReducerPercent = Mathf.Clamp(engineTelegraphReducerPercent, 0, 100);

        RefreshTargetRudderRuntimeStatus();
        RefreshActualRudderRuntimeStatus();
        RefreshEngineTelegraphRuntimeStatus();
        RefreshNavigationRuntimeStatus();
    }
}
