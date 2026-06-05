using UnityEngine;

[RequireComponent(typeof(ShipStatuses))]
public class ShipMovementController : MonoBehaviour
{
    private const float StationarySpeedEpsilon = 0.0001f;

    [Header("References")]
    [SerializeField] private ShipStatuses shipStatuses;

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

    // В новой модели это не "целевая скорость корпуса", а скорость установившегося режима двигателя/телеграфа.
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
            Debug.LogError("ShipMovementController: ShipStatuses не найден.", this);
            enabled = false;
            return;
        }

        if (shipStatuses.ShipConfig == null)
        {
            Debug.LogError("ShipMovementController: ShipConfig не назначен в ShipStatuses.", this);
            enabled = false;
            return;
        }

        shipStatuses.OnEngineTelegraphChanged += OnEngineTelegraphChanged;
        RefreshTargetSpeedFromStatuses();
        PublishNavigationStatus();
        RefreshRuntimeStatus();
    }

    private void OnDisable()
    {
        if (shipStatuses != null)
        {
            shipStatuses.OnEngineTelegraphChanged -= OnEngineTelegraphChanged;
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
    }

    private void OnEngineTelegraphChanged(
        EngineTelegraphSector sector,
        EngineTelegraphSectorData sectorData
    )
    {
        if (sectorData == null)
            return;

        SetTargetSpeedKnots(sectorData.speedKnots);
    }

    private void RefreshTargetSpeedFromStatuses()
    {
        if (shipStatuses == null)
            return;

        SetTargetSpeedKnots(shipStatuses.GetCurrentEngineTelegraphSpeedKnots());
    }

    private void SetTargetSpeedKnots(float newTargetSpeedKnots)
    {
        targetSpeedKnots = newTargetSpeedKnots;
        RefreshConvertedSpeeds();

        if (showDebugInfo)
        {
            Debug.Log(
                $"ShipMovementController: режим двигателя {targetSpeedKnots:F1} узл. " +
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

        if (config == null || shipStatuses == null)
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

        transform.RotateAround(
            GetTurnPivotWorldPosition(config),
            Vector3.up,
            currentYawRateDegreesPerSecond * deltaTime
        );
    }

    private void MoveShip(float deltaTime)
    {
        if (Mathf.Approximately(currentSpeedUnityUnitsPerSecond, 0f))
            return;

        Vector3 movementDirectionVector = transform.forward * MovementDirectionMultiplier;
        transform.position += movementDirectionVector * currentSpeedUnityUnitsPerSecond * deltaTime;
    }

    private Vector3 GetTurnPivotWorldPosition(ShipConfig config)
    {
        Vector3 actualForwardDirection = transform.forward * MovementDirectionMultiplier;
        return transform.position + actualForwardDirection * config.TurnPivotForwardOffset;
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
        if (shipStatuses == null)
        {
            return;
        }

        shipStatuses.SetNavigationStatus(currentSpeedKnots, transform.eulerAngles.y);
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
}
