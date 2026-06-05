using UnityEngine;

public enum ShipPropellerRotationSource
{
    EngineTelegraphOrder,
    ShipSpeed
}

public class ShipPropellerVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShipMovementController movementController;
    [SerializeField] private Transform[] propellers;

    [Header("Rotation Source")]
    [Tooltip("Engine Telegraph Order — винты крутятся от команды двигателя. Ship Speed — старое поведение, от скорости корпуса.")]
    [SerializeField] private ShipPropellerRotationSource rotationSource = ShipPropellerRotationSource.EngineTelegraphOrder;

    [Header("Rotation Axis")]
    [Tooltip("Локальная ось, вокруг которой вращается винт. Обычно Z. Если винт крутится неправильно — попробуй X или Y.")]
    [SerializeField] private Vector3 localRotationAxis = Vector3.forward;

    [Tooltip("Если винтов несколько, каждый второй будет вращаться в обратную сторону.")]
    [SerializeField] private bool alternatePropellerDirections = true;

    [Tooltip("При заднем ходе визуальное вращение винтов меняет направление.")]
    [SerializeField] private bool reverseWhenMovingBackward = true;

    [Header("Visual Speed")]
    [Tooltip("При какой скорости режима/корпуса в узлах винты достигают максимальной визуальной скорости вращения.")]
    [SerializeField] private float speedForFullEffectKnots = 12f;

    [Tooltip("Максимальная визуальная скорость вращения винтов, градусов в секунду. Это не физика, а только видимость.")]
    [SerializeField] private float maxRotationSpeedDegreesPerSecond = 1080f;

    [Tooltip("Минимальная заметная скорость вращения, если есть ненулевая команда двигателя/скорость корпуса.")]
    [SerializeField] private float minMovingRotationSpeedDegreesPerSecond = 120f;

    [Tooltip("Скорость плавного изменения вращения винтов при смене режима двигателя.")]
    [SerializeField] private float rotationSmoothSpeed = 8f;

    [Header("Idle")]
    [Tooltip("Крутить ли винты на малых оборотах, когда источник вращения равен нулю.")]
    [SerializeField] private bool spinWhenStopped = false;

    [Tooltip("Визуальная скорость вращения на месте, если Spin When Stopped включён.")]
    [SerializeField] private float idleRotationSpeedDegreesPerSecond = 60f;

    [Header("Runtime Status")]
    [SerializeField, ReadOnlyInspector] private float currentShipSpeedKnots;
    [SerializeField, ReadOnlyInspector] private float engineOrderSpeedKnots;
    [SerializeField, ReadOnlyInspector] private float sourceSpeedKnots;
    [SerializeField, ReadOnlyInspector] private float targetRotationSpeedDegreesPerSecond;
    [SerializeField, ReadOnlyInspector] private float currentRotationSpeedDegreesPerSecond;

    public float CurrentShipSpeedKnots => currentShipSpeedKnots;
    public float EngineOrderSpeedKnots => engineOrderSpeedKnots;
    public float SourceSpeedKnots => sourceSpeedKnots;
    public float CurrentRotationSpeedDegreesPerSecond => currentRotationSpeedDegreesPerSecond;

    private void Awake()
    {
        ResolveReferences();
    }

    private void LateUpdate()
    {
        if (movementController == null)
        {
            ResolveReferences();
        }

        if (movementController == null || propellers == null || propellers.Length == 0)
            return;

        UpdateTargetRotationSpeed();
        UpdateCurrentRotationSpeed();
        RotatePropellers();
    }

    private void ResolveReferences()
    {
        if (movementController == null)
        {
            movementController = GetComponent<ShipMovementController>();
        }

        if (movementController == null)
        {
            movementController = GetComponentInParent<ShipMovementController>();
        }
    }

    private void UpdateTargetRotationSpeed()
    {
        currentShipSpeedKnots = movementController.CurrentSpeedKnots;
        engineOrderSpeedKnots = movementController.EngineOrderSpeedKnots;

        sourceSpeedKnots = rotationSource == ShipPropellerRotationSource.EngineTelegraphOrder
            ? engineOrderSpeedKnots
            : currentShipSpeedKnots;

        float absoluteSourceSpeedKnots = Mathf.Abs(sourceSpeedKnots);
        bool hasSourceMotion = absoluteSourceSpeedKnots > 0.01f;

        float absoluteTargetRotationSpeed;

        if (hasSourceMotion)
        {
            float speedFactor = Mathf.InverseLerp(
                0f,
                speedForFullEffectKnots,
                absoluteSourceSpeedKnots
            );

            absoluteTargetRotationSpeed = Mathf.Lerp(
                minMovingRotationSpeedDegreesPerSecond,
                maxRotationSpeedDegreesPerSecond,
                speedFactor
            );
        }
        else
        {
            absoluteTargetRotationSpeed = spinWhenStopped
                ? idleRotationSpeedDegreesPerSecond
                : 0f;
        }

        float direction = 1f;

        if (reverseWhenMovingBackward && sourceSpeedKnots < -0.01f)
        {
            direction = -1f;
        }

        targetRotationSpeedDegreesPerSecond = absoluteTargetRotationSpeed * direction;
    }

    private void UpdateCurrentRotationSpeed()
    {
        float lerpFactor = 1f - Mathf.Exp(-rotationSmoothSpeed * Time.deltaTime);

        currentRotationSpeedDegreesPerSecond = Mathf.Lerp(
            currentRotationSpeedDegreesPerSecond,
            targetRotationSpeedDegreesPerSecond,
            lerpFactor
        );
    }

    private void RotatePropellers()
    {
        if (Mathf.Abs(currentRotationSpeedDegreesPerSecond) < 0.01f)
            return;

        Vector3 rotationAxis = localRotationAxis.sqrMagnitude > 0.0001f
            ? localRotationAxis.normalized
            : Vector3.forward;

        float rotationDelta = currentRotationSpeedDegreesPerSecond * Time.deltaTime;

        for (int i = 0; i < propellers.Length; i++)
        {
            Transform propeller = propellers[i];

            if (propeller == null)
                continue;

            float propellerDirection = 1f;

            if (alternatePropellerDirections && i % 2 == 1)
            {
                propellerDirection = -1f;
            }

            propeller.Rotate(
                rotationAxis,
                rotationDelta * propellerDirection,
                Space.Self
            );
        }
    }

    private void OnValidate()
    {
        speedForFullEffectKnots = Mathf.Max(0.01f, speedForFullEffectKnots);
        maxRotationSpeedDegreesPerSecond = Mathf.Max(0f, maxRotationSpeedDegreesPerSecond);
        minMovingRotationSpeedDegreesPerSecond = Mathf.Max(0f, minMovingRotationSpeedDegreesPerSecond);
        rotationSmoothSpeed = Mathf.Max(0f, rotationSmoothSpeed);
        idleRotationSpeedDegreesPerSecond = Mathf.Max(0f, idleRotationSpeedDegreesPerSecond);
    }
}
