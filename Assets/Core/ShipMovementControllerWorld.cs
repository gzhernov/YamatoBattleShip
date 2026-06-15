using UnityEngine;
using World;

[RequireComponent(typeof(ShipStatuses))]
[RequireComponent(typeof(WorldAgent))]
public class ShipMovementControllerWorld : MonoBehaviour
{
    private const float StationarySpeedEpsilon = 0.0001f;

    [Header("References")]
    [SerializeField] private ShipStatuses shipStatuses;
    [SerializeField] private WorldAgent worldAgent;

    [Header("Runtime Speed (Read Only)")]
    [SerializeField, ReadOnlyInspector] private float currentSpeedKnots;
    [SerializeField, ReadOnlyInspector] private float targetSpeedKnots;
    [SerializeField, ReadOnlyInspector] private float currentSpeedUnityUnitsPerSecond;
    [SerializeField, ReadOnlyInspector] private float targetSpeedUnityUnitsPerSecond;
    [SerializeField, ReadOnlyInspector] private float effectiveEngineOrderSpeedKnots;
    [SerializeField, ReadOnlyInspector] private float effectiveEngineOrderSpeedUnityUnitsPerSecond;
    [SerializeField, ReadOnlyInspector] private float appliedAccelerationKnotsPerSecond;
    [SerializeField, ReadOnlyInspector] private float rudderDragEffectiveness;
    [SerializeField, ReadOnlyInspector] private float rudderDragDecelerationKnotsPerSecond;
    [SerializeField, ReadOnlyInspector] private ShipPropulsionState propulsionState;
    [SerializeField, ReadOnlyInspector] private float knotsToUnityUnitsPerSecond;
    [SerializeField, ReadOnlyInspector] private ShipMovementDirection movementDirection;

    [Header("Runtime Steering (Read Only)")]
    [SerializeField, ReadOnlyInspector] private float actualRudderSignedValue;
    [SerializeField, ReadOnlyInspector] private float targetYawRateDegreesPerSecond;
    [SerializeField, ReadOnlyInspector] private float currentYawRateDegreesPerSecond;
    [SerializeField, ReadOnlyInspector] private float targetTurnRadius;
    [SerializeField, ReadOnlyInspector] private float speedEffectiveness;
    [SerializeField, ReadOnlyInspector] private float rudderEffectiveness;
    [SerializeField, ReadOnlyInspector] private float turnEffectiveness;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    public float CurrentSpeedKnots => currentSpeedKnots;

    // Р’ РЅРѕРІРѕР№ РјРѕРґРµР»Рё СЌС‚Рѕ РЅРµ "С†РµР»РµРІР°СЏ СЃРєРѕСЂРѕСЃС‚СЊ РєРѕСЂРїСѓСЃР°", Р° СЃРєРѕСЂРѕСЃС‚СЊ СѓСЃС‚Р°РЅРѕРІРёРІС€РµРіРѕСЃСЏ СЂРµР¶РёРјР° РґРІРёРіР°С‚РµР»СЏ/С‚РµР»РµРіСЂР°С„Р°.
    public float TargetSpeedKnots => targetSpeedKnots;
    public float EngineOrderSpeedKnots => targetSpeedKnots;

    public float CurrentAbsSpeedKnots => Mathf.Abs(currentSpeedKnots);
    public float CurrentSpeedUnityUnitsPerSecond => currentSpeedUnityUnitsPerSecond;
    public float TargetSpeedUnityUnitsPerSecond => targetSpeedUnityUnitsPerSecond;
    public float EffectiveEngineOrderSpeedKnots => effectiveEngineOrderSpeedKnots;
    public float EffectiveEngineOrderSpeedUnityUnitsPerSecond => effectiveEngineOrderSpeedUnityUnitsPerSecond;
    public float CurrentAbsSpeedUnityUnitsPerSecond => Mathf.Abs(currentSpeedUnityUnitsPerSecond);
    public float CurrentSpeedNormalized => GetCurrentSpeedNormalized();
    public float AppliedAccelerationKnotsPerSecond => appliedAccelerationKnotsPerSecond;
    public float RudderDragEffectiveness => rudderDragEffectiveness;
    public float RudderDragDecelerationKnotsPerSecond => rudderDragDecelerationKnotsPerSecond;
    public ShipPropulsionState PropulsionState => propulsionState;

    public float CurrentYawRateDegreesPerSecond => currentYawRateDegreesPerSecond;
    public float TargetYawRateDegreesPerSecond => targetYawRateDegreesPerSecond;
    public float TargetTurnRadius => targetTurnRadius;
    public float SpeedEffectiveness => speedEffectiveness;
    public float RudderEffectiveness => rudderEffectiveness;
    public float TurnEffectiveness => turnEffectiveness;

    private ShipConfig Config => shipStatuses != null ? shipStatuses.ShipConfig : null;

    private float KnotsToUnityUnitsPerSecond => Config != null
        ? Config.KnotsToUnityUnitsPerSecond
        : 0f;

    private float MovementDirectionMultiplier => Config != null
        ? Config.MovementDirectionMultiplier
        : 1f;

    private void Awake()
    {
        ResolveReferences();
        RefreshRuntimeStatus();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (shipStatuses == null)
        {
            Debug.LogError("ShipMovementControllerWorld: ShipStatuses не найден.", this);
            enabled = false;
            return;
        }

        if (worldAgent == null)
        {
            Debug.LogError("ShipMovementControllerWorld: WorldAgent не найден.", this);
            enabled = false;
            return;
        }

        if (shipStatuses.ShipConfig == null)
        {
            Debug.LogError("ShipMovementControllerWorld: ShipConfig не назначен в ShipStatuses.", this);
            enabled = false;
            return;
        }

        shipStatuses.OnEngineTelegraphChanged += OnEngineTelegraphChanged;
        shipStatuses.OnEngineTelegraphReducerChanged += OnEngineTelegraphReducerChanged;
        RefreshTargetSpeedFromStatuses();
        PublishNavigationStatus();
        RefreshRuntimeStatus();
    }

    private void OnDisable()
    {
        if (shipStatuses != null)
        {
            shipStatuses.OnEngineTelegraphChanged -= OnEngineTelegraphChanged;
            shipStatuses.OnEngineTelegraphReducerChanged -= OnEngineTelegraphReducerChanged;
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        UpdateSpeed(deltaTime);
        UpdateSteering(deltaTime);
        MoveShip(deltaTime);
        PublishNavigationStatus();
        RefreshRuntimeStatus();
    }

    private void ResolveReferences()
    {
        if (shipStatuses == null)
        {
            shipStatuses = GetComponent<ShipStatuses>();
        }

        if (worldAgent == null)
        {
            worldAgent = GetComponent<WorldAgent>();
        }
    }

    private void OnEngineTelegraphChanged(
        EngineTelegraphSector sector,
        EngineTelegraphSectorData sectorData
    )
    {
        RefreshTargetSpeedFromStatuses();
    }

    private void OnEngineTelegraphReducerChanged(int reducerPercent)
    {
        RefreshTargetSpeedFromStatuses();
    }

    private void RefreshTargetSpeedFromStatuses()
    {
        if (shipStatuses == null)
            return;

        EngineTelegraphConfig engineTelegraphConfig = shipStatuses.EngineTelegraphConfig;

        if (engineTelegraphConfig == null || !engineTelegraphConfig.HasSectors)
        {
            SetTargetSpeedKnots(0f);
            return;
        }

        EngineTelegraphSector currentSector = shipStatuses.CurrentEngineTelegraphSector;
        EngineTelegraphSectorData currentSectorData = shipStatuses.GetCurrentEngineTelegraphSectorData();

        if (currentSectorData == null)
        {
            SetTargetSpeedKnots(0f);
            return;
        }

        if (currentSector == EngineTelegraphSector.Stop)
        {
            SetTargetSpeedKnots(0f);
            return;
        }

        int currentSectorIndex = shipStatuses.GetEngineTelegraphSectorIndex(currentSector);
        EngineTelegraphSectorData previousSectorData = engineTelegraphConfig.GetSectorDataByIndex(currentSectorIndex - 1);

        float reducer01 = shipStatuses.EngineTelegraphReducerPercent / 100f;
        float fromSpeed = previousSectorData != null
            ? previousSectorData.speedKnots
            : currentSectorData.speedKnots;
        float toSpeed = currentSectorData.speedKnots;
        float effectiveSpeed = Mathf.Lerp(fromSpeed, toSpeed, reducer01);

        SetTargetSpeedKnots(effectiveSpeed);
    }

    private void SetTargetSpeedKnots(float newTargetSpeedKnots)
    {
        targetSpeedKnots = newTargetSpeedKnots;
        RefreshConvertedSpeeds();

        if (showDebugInfo)
        {
            Debug.Log(
                $"ShipMovementControllerWorld: режим двигателя {targetSpeedKnots:F1} узл. " +
                $"({targetSpeedUnityUnitsPerSecond:F2} units/sec в установившемся режиме)",
                this
            );
        }
    }

    private void UpdateSpeed(float deltaTime)
    {
        ShipConfig config = Config;

        if (config == null)
            return;

        actualRudderSignedValue = shipStatuses != null
            ? shipStatuses.ActualRudderSignedValue
            : 0f;

        ShipPropulsionResult propulsionResult = ShipPropulsionModel.Calculate(
            config,
            currentSpeedKnots,
            targetSpeedKnots,
            actualRudderSignedValue,
            deltaTime
        );

        currentSpeedKnots = propulsionResult.NewSpeedKnots;
        effectiveEngineOrderSpeedKnots = propulsionResult.EffectiveEngineOrderSpeedKnots;
        appliedAccelerationKnotsPerSecond = propulsionResult.AppliedAccelerationKnotsPerSecond;
        rudderDragEffectiveness = propulsionResult.RudderDragEffectiveness;
        rudderDragDecelerationKnotsPerSecond = propulsionResult.RudderDragDecelerationKnotsPerSecond;
        propulsionState = propulsionResult.State;

        RefreshConvertedSpeeds();
    }

    private void UpdateSteering(float deltaTime)
    {
        ShipConfig config = Config;

        if (config == null || shipStatuses == null || worldAgent == null)
        {
            targetYawRateDegreesPerSecond = 0f;
            currentYawRateDegreesPerSecond = 0f;
            return;
        }

        actualRudderSignedValue = shipStatuses.ActualRudderSignedValue;

        ShipManeuveringResult maneuveringResult = ShipManeuveringModel.Calculate(
            config,
            currentSpeedKnots,
            currentSpeedUnityUnitsPerSecond,
            actualRudderSignedValue
        );

        targetYawRateDegreesPerSecond = maneuveringResult.TargetYawRateDegreesPerSecond;
        targetTurnRadius = maneuveringResult.TargetTurnRadius;
        speedEffectiveness = maneuveringResult.SpeedEffectiveness;
        rudderEffectiveness = maneuveringResult.RudderEffectiveness;
        turnEffectiveness = maneuveringResult.TurnEffectiveness;

        currentYawRateDegreesPerSecond = Mathf.MoveTowards(
            currentYawRateDegreesPerSecond,
            targetYawRateDegreesPerSecond,
            config.TurnAcceleration * deltaTime
        );

        if (Mathf.Abs(currentSpeedUnityUnitsPerSecond) <= StationarySpeedEpsilon)
        {
            currentYawRateDegreesPerSecond = 0f;
            return;
        }

        if (Mathf.Approximately(currentYawRateDegreesPerSecond, 0f))
            return;

        RotateShipAroundWorldPivot(config, currentYawRateDegreesPerSecond * deltaTime);
    }

    private void MoveShip(float deltaTime)
    {
        if (Mathf.Approximately(currentSpeedUnityUnitsPerSecond, 0f) || worldAgent == null)
            return;

        Quaternion worldRotation = GetWorldRotation();
        Vector3 movementDirectionVector = worldRotation * Vector3.forward * MovementDirectionMultiplier;
        WorldVector3D nextWorldPosition = GetWorldPosition() + movementDirectionVector * currentSpeedUnityUnitsPerSecond * deltaTime;
        worldAgent.SetWorldPosition(nextWorldPosition);
    }

    private Vector3 GetTurnPivotWorldPosition(ShipConfig config)
    {
        Quaternion worldRotation = GetWorldRotation();
        Vector3 actualForwardDirection = worldRotation * Vector3.forward * MovementDirectionMultiplier;
        return GetWorldPosition().ToUnityVector3() + actualForwardDirection * config.TurnPivotForwardOffset;
    }

    private void RefreshConvertedSpeeds()
    {
        float conversion = KnotsToUnityUnitsPerSecond;
        currentSpeedUnityUnitsPerSecond = currentSpeedKnots * conversion;
        targetSpeedUnityUnitsPerSecond = targetSpeedKnots * conversion;
        effectiveEngineOrderSpeedUnityUnitsPerSecond = effectiveEngineOrderSpeedKnots * conversion;
    }

    private float GetCurrentSpeedNormalized()
    {
        ShipConfig config = Config;

        if (config == null)
            return 0f;

        return Mathf.InverseLerp(
            0f,
            config.MaxEngineTelegraphSpeedKnots,
            Mathf.Abs(currentSpeedKnots)
        );
    }

    private void PublishNavigationStatus()
    {
        if (shipStatuses == null || worldAgent == null)
        {
            return;
        }

        shipStatuses.SetNavigationStatus(currentSpeedKnots, GetWorldRotation().eulerAngles.y);
    }

    private void RefreshRuntimeStatus()
    {
        knotsToUnityUnitsPerSecond = KnotsToUnityUnitsPerSecond;
        movementDirection = Config != null
            ? Config.MovementDirection
            : ShipMovementDirection.LocalForward;

        if (shipStatuses != null)
        {
            actualRudderSignedValue = shipStatuses.ActualRudderSignedValue;
        }

        RefreshConvertedSpeeds();
    }

    private WorldVector3D GetWorldPosition()
    {
        return worldAgent.WorldTransform.Position;
    }

    private Quaternion GetWorldRotation()
    {
        return worldAgent.WorldTransform.Rotation;
    }

    private void RotateShipAroundWorldPivot(ShipConfig config, float rotationDeltaDegrees)
    {
        Vector3 currentWorldPosition = GetWorldPosition().ToUnityVector3();
        Vector3 pivotWorldPosition = GetTurnPivotWorldPosition(config);
        Quaternion deltaRotation = Quaternion.AngleAxis(rotationDeltaDegrees, Vector3.up);
        Vector3 rotatedOffset = deltaRotation * (currentWorldPosition - pivotWorldPosition);
        WorldVector3D nextWorldPosition = new WorldVector3D(
            pivotWorldPosition.x + rotatedOffset.x,
            currentWorldPosition.y,
            pivotWorldPosition.z + rotatedOffset.z
        );
        Quaternion nextWorldRotation = deltaRotation * GetWorldRotation();

        worldAgent.SetWorldPosition(nextWorldPosition);
        worldAgent.SetWorldRotation(nextWorldRotation);
    }
}
