using UnityEngine;

public class ShipPropellerVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShipStatuses shipStatuses;
    [SerializeField] private Transform[] propellers;

    [Header("Rotation Axis")]
    [Tooltip("Локальная ось, вокруг которой вращается винт. Обычно Z. Если винт крутится неправильно — попробуй X или Y.")]
    [SerializeField] private Vector3 localRotationAxis = Vector3.forward;

    [Tooltip("Если винтов несколько, каждый второй будет вращаться в обратную сторону.")]
    [SerializeField] private bool alternatePropellerDirections = true;

    [Tooltip("При заднем ходе визуальное вращение винтов меняет направление.")]
    [SerializeField] private bool reverseWhenMovingBackward = true;

    [Header("Visual Speed")]
    [Tooltip("При какой скорости режима в узлах винты достигают максимальной визуальной скорости вращения.")]
    [SerializeField] private float speedForFullEffectKnots = 12f;

    [Tooltip("Максимальная визуальная скорость вращения винтов, градусов в секунду. Это не физика, а только видимость.")]
    [SerializeField] private float maxRotationSpeedDegreesPerSecond = 1080f;

    [Tooltip("Минимальная заметная скорость вращения, если есть ненулевая команда двигателя.")]
    [SerializeField] private float minMovingRotationSpeedDegreesPerSecond = 120f;

    [Tooltip("Скорость плавного изменения вращения винтов при смене режима двигателя.")]
    [SerializeField] private float rotationSmoothSpeed = 8f;

    [Header("Runtime Status")]
    [SerializeField, ReadOnlyInspector] private float targetRotationSpeedDegreesPerSecond;
    [SerializeField, ReadOnlyInspector] private float currentRotationSpeedDegreesPerSecond;

    public float CurrentRotationSpeedDegreesPerSecond => currentRotationSpeedDegreesPerSecond;

    private void Awake()
    {
        ResolveReferences();
    }

    private void LateUpdate()
    {
        if (shipStatuses == null)
        {
            ResolveReferences();
        }

        if (shipStatuses == null || propellers == null || propellers.Length == 0)
            return;

        UpdateTargetRotationSpeed();
        UpdateCurrentRotationSpeed();
        RotatePropellers();
    }

    private void ResolveReferences()
    {
        if (shipStatuses == null)
        {
            shipStatuses = GetComponent<ShipStatuses>();
        }

        if (shipStatuses == null)
        {
            shipStatuses = GetComponentInParent<ShipStatuses>();
        }
    }

    private void UpdateTargetRotationSpeed()
    {
        float engineOrderSpeedKnots = GetEngineOrderSpeedKnots();
        float absoluteEngineOrderSpeedKnots = Mathf.Abs(engineOrderSpeedKnots);
        bool hasEngineOrderMotion = absoluteEngineOrderSpeedKnots > 0.01f;

        float absoluteTargetRotationSpeed;

        if (hasEngineOrderMotion)
        {
            float speedFactor = Mathf.InverseLerp(
                0f,
                speedForFullEffectKnots,
                absoluteEngineOrderSpeedKnots
            );

            absoluteTargetRotationSpeed = Mathf.Lerp(
                minMovingRotationSpeedDegreesPerSecond,
                maxRotationSpeedDegreesPerSecond,
                speedFactor
            );
        }
        else
        {
            absoluteTargetRotationSpeed = 0f;
        }

        float direction = 1f;

        if (reverseWhenMovingBackward && engineOrderSpeedKnots < -0.01f)
        {
            direction = -1f;
        }

        targetRotationSpeedDegreesPerSecond = absoluteTargetRotationSpeed * direction;
    }

    private float GetEngineOrderSpeedKnots()
    {
        if (shipStatuses == null)
            return 0f;

        EngineTelegraphConfig engineTelegraphConfig = shipStatuses.EngineTelegraphConfig;

        if (engineTelegraphConfig == null || !engineTelegraphConfig.HasSectors)
            return 0f;

        EngineTelegraphSector currentSector = shipStatuses.CurrentEngineTelegraphSector;
        EngineTelegraphSectorData currentSectorData = shipStatuses.GetCurrentEngineTelegraphSectorData();

        if (currentSectorData == null || currentSector == EngineTelegraphSector.Stop)
            return 0f;

        int currentSectorIndex = shipStatuses.GetEngineTelegraphSectorIndex(currentSector);
        EngineTelegraphSectorData previousSectorData = engineTelegraphConfig.GetSectorDataByIndex(currentSectorIndex - 1);
        float reducer01 = shipStatuses.EngineTelegraphReducerPercent / 100f;
        float fromSpeed = previousSectorData != null
            ? previousSectorData.speedKnots
            : currentSectorData.speedKnots;

        return Mathf.Lerp(fromSpeed, currentSectorData.speedKnots, reducer01);
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
    }
}
