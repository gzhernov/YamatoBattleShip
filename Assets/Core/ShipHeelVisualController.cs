using UnityEngine;

[DefaultExecutionOrder(100)]
public class ShipHeelVisualController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Отдельный дочерний pivot для визуального крена. Не указывай сюда ShipRoot. Лучше: ShipRoot -> ManeuverHeelPivot -> WavePivot/Model.")]
    [SerializeField] private Transform heelVisualRoot;

    [SerializeField] private ShipStatuses shipStatuses;
    [SerializeField] private ShipMovementController movementController;

    [Header("Maneuver Heel")]
    [SerializeField] private bool enableManeuverHeel = true;

    [Tooltip("Включи, если при повороте корабль кренится не наружу, а внутрь циркуляции.")]
    [SerializeField] private bool invertHeelDirection = false;

    [Tooltip("Возвращать визуальный pivot в исходный rotation при отключении компонента.")]
    [SerializeField] private bool resetPoseOnDisable = true;

    [Header("Runtime Status (Read Only)")]
    [SerializeField, ReadOnlyInspector] private float currentHeelAngle;
    [SerializeField, ReadOnlyInspector] private float targetHeelAngle;
    [SerializeField, ReadOnlyInspector] private float normalizedYawRate;
    [SerializeField, ReadOnlyInspector] private float speedEffectiveness;

    private Quaternion startLocalRotation;
    private bool hasCachedStartRotation;

    private ShipConfig Config => shipStatuses != null ? shipStatuses.ShipConfig : null;

    private void Awake()
    {
        ResolveReferences();
        CacheStartRotation();
    }

    private void OnEnable()
    {
        ResolveReferences();
        CacheStartRotation();
    }

    private void OnDisable()
    {
        if (resetPoseOnDisable && heelVisualRoot != null && hasCachedStartRotation)
        {
            currentHeelAngle = 0f;
            targetHeelAngle = 0f;
            heelVisualRoot.localRotation = startLocalRotation;
        }
    }

    private void LateUpdate()
    {
        if (heelVisualRoot == null)
            return;

        if (!enableManeuverHeel || Config == null || movementController == null)
        {
            ReturnToStartPose();
            return;
        }

        UpdateTargetHeelAngle();
        UpdateCurrentHeelAngle();
        ApplyHeelRotation();
    }

    private void ResolveReferences()
    {
        if (shipStatuses == null)
        {
            shipStatuses = GetComponent<ShipStatuses>();

            if (shipStatuses == null)
            {
                shipStatuses = GetComponentInParent<ShipStatuses>();
            }
        }

        if (movementController == null)
        {
            movementController = GetComponent<ShipMovementController>();

            if (movementController == null)
            {
                movementController = GetComponentInParent<ShipMovementController>();
            }
        }

        if (heelVisualRoot == null)
        {
            Debug.LogWarning(
                "ShipHeelVisualController: Heel Visual Root не назначен. " +
                "Создай отдельный дочерний объект ManeuverHeelPivot и назначь его сюда.",
                this
            );
        }
    }

    private void CacheStartRotation()
    {
        if (heelVisualRoot == null)
            return;

        startLocalRotation = heelVisualRoot.localRotation;
        hasCachedStartRotation = true;
    }

    private void UpdateTargetHeelAngle()
    {
        ShipConfig config = Config;
        float yawRateForFullHeel = Mathf.Max(0.01f, config.YawRateForFullHeel);

        normalizedYawRate = Mathf.Clamp(
            movementController.CurrentYawRateDegreesPerSecond / yawRateForFullHeel,
            -1f,
            1f
        );

        float speed01 = movementController.CurrentSpeedNormalized;
        speedEffectiveness = config.SpeedToHeelEffectiveness != null
            ? Mathf.Clamp01(config.SpeedToHeelEffectiveness.Evaluate(speed01))
            : speed01;

        // Минус нужен для крена наружу циркуляции при стандартном знаке yaw.
        // Если у конкретной модели визуально получается наоборот, включи Invert Heel Direction.
        float direction = invertHeelDirection ? 1f : -1f;
        targetHeelAngle = normalizedYawRate * config.MaxManeuverHeelAngle * speedEffectiveness * direction;
    }

    private void UpdateCurrentHeelAngle()
    {
        ShipConfig config = Config;
        bool returningToCenter = Mathf.Abs(targetHeelAngle) < Mathf.Abs(currentHeelAngle);
        float smoothSpeed = returningToCenter
            ? config.HeelRecoverySpeed
            : config.HeelResponseSpeed;

        currentHeelAngle = Damp(
            currentHeelAngle,
            targetHeelAngle,
            smoothSpeed,
            Time.deltaTime
        );
    }

    private void ApplyHeelRotation()
    {
        if (!hasCachedStartRotation)
        {
            CacheStartRotation();
        }

        heelVisualRoot.localRotation = startLocalRotation * Quaternion.Euler(0f, 0f, currentHeelAngle);
    }

    private void ReturnToStartPose()
    {
        ShipConfig config = Config;
        float smoothSpeed = config != null
            ? config.HeelRecoverySpeed
            : 2f;

        targetHeelAngle = 0f;
        normalizedYawRate = 0f;
        speedEffectiveness = 0f;

        currentHeelAngle = Damp(
            currentHeelAngle,
            0f,
            smoothSpeed,
            Time.deltaTime
        );

        ApplyHeelRotation();
    }

    private static float Damp(float current, float target, float smoothSpeed, float deltaTime)
    {
        if (smoothSpeed <= 0f)
            return target;

        float t = 1f - Mathf.Exp(-smoothSpeed * deltaTime);
        return Mathf.Lerp(current, target, t);
    }
}
