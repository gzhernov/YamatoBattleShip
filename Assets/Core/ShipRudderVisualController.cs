using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ShipRudderVisualController : MonoBehaviour
{
    [Serializable]
    public class RudderBlade
    {
        [Tooltip("Transform pivot-объекта пера руля. Лучше назначать не сам mesh, а Empty GameObject, стоящий в точке оси вращения руля.")]
        public Transform pivot;

        [Tooltip("Локальная ось вращения руля. Обычно Vector3.up / ось Y.")]
        public Vector3 localRotationAxis = Vector3.up;

        [Tooltip("Включи, если конкретное перо поворачивается визуально в обратную сторону.")]
        public bool invertDirection = false;

        [Tooltip("Индивидуальный множитель угла для этого пера. Обычно 1.")]
        [Min(0f)] public float angleMultiplier = 1f;

        [NonSerialized] public Quaternion InitialLocalRotation = Quaternion.identity;

        public void CaptureInitialRotation()
        {
            if (pivot == null)
                return;

            InitialLocalRotation = pivot.localRotation;
        }

        public void Apply(float signedRudderValue, float maxAngleDegrees)
        {
            if (pivot == null)
                return;

            Vector3 axis = localRotationAxis.sqrMagnitude > 0.0001f
                ? localRotationAxis.normalized
                : Vector3.up;

            float direction = invertDirection ? -1f : 1f;
            float angle = signedRudderValue * maxAngleDegrees * angleMultiplier * direction;

            pivot.localRotation = InitialLocalRotation * Quaternion.AngleAxis(angle, axis);
        }
    }

    [Header("References")]
    [SerializeField] private ShipStatuses shipStatuses;

    [Tooltip("Опционально. Если не назначен, будет взят из ShipStatuses.ShipConfig.")]
    [SerializeField] private ShipConfig shipConfig;

    [Tooltip("Список визуальных перьев руля. Для Yamato обычно 2 элемента: левый и правый руль.")]
    [SerializeField] private RudderBlade[] rudders = Array.Empty<RudderBlade>();

    [Header("Rudder Visual")]
    [Tooltip("Обычно нужно true: перья руля показывают фактическое положение, которое догоняет команду с задержкой. false полезно только для отладки команды руля.")]
    [SerializeField] private bool useActualRudderValue = true;

    [Tooltip("Дополнительное сглаживание только для визуала. Обычно оставь false, потому что ActualRudderSignedValue уже плавный.")]
    [SerializeField] private bool smoothVisualValue = false;

    [Tooltip("Скорость дополнительного сглаживания визуала, если Smooth Visual Value включён.")]
    [SerializeField, Min(0f)] private float visualSmoothSpeed = 12f;

    [Header("Runtime (Read Only)")]
    [SerializeField, ReadOnlyInspector] private float sourceRudderSignedValue;
    [SerializeField, ReadOnlyInspector] private float visualRudderSignedValue;
    [SerializeField, ReadOnlyInspector] private float maxRudderAngleDegreesFromConfig;
    [SerializeField, ReadOnlyInspector] private float visualRudderAngleDegrees;

    public float SourceRudderSignedValue => sourceRudderSignedValue;
    public float VisualRudderSignedValue => visualRudderSignedValue;
    public float MaxRudderAngleDegreesFromConfig => maxRudderAngleDegreesFromConfig;
    public float VisualRudderAngleDegrees => visualRudderAngleDegrees;

    private void Awake()
    {
        ResolveReferences();
        CaptureInitialRotations();
        ApplyImmediate();
    }

    private void OnEnable()
    {
        ResolveReferences();
        CaptureInitialRotations();
        ApplyImmediate();
    }

    private void Update()
    {
        UpdateRudderVisual(Time.deltaTime);
    }

    private void ResolveReferences()
    {
        if (shipStatuses == null)
        {
            shipStatuses = GetComponentInParent<ShipStatuses>();

            if (shipStatuses == null)
            {
                shipStatuses = FindFirstObjectByType<ShipStatuses>();
            }
        }

        if (shipConfig == null && shipStatuses != null)
        {
            shipConfig = shipStatuses.ShipConfig;
        }
    }

    private void CaptureInitialRotations()
    {
        if (rudders == null)
            return;

        for (int i = 0; i < rudders.Length; i++)
        {
            rudders[i]?.CaptureInitialRotation();
        }
    }

    private void ApplyImmediate()
    {
        sourceRudderSignedValue = GetSourceRudderSignedValue();
        visualRudderSignedValue = sourceRudderSignedValue;
        ApplyToRudders(visualRudderSignedValue);
    }

    private void UpdateRudderVisual(float deltaTime)
    {
        ResolveReferences();

        sourceRudderSignedValue = GetSourceRudderSignedValue();

        if (smoothVisualValue && visualSmoothSpeed > 0f)
        {
            visualRudderSignedValue = Mathf.MoveTowards(
                visualRudderSignedValue,
                sourceRudderSignedValue,
                visualSmoothSpeed * deltaTime
            );
        }
        else
        {
            visualRudderSignedValue = sourceRudderSignedValue;
        }

        ApplyToRudders(visualRudderSignedValue);
    }

    private float GetSourceRudderSignedValue()
    {
        if (shipStatuses == null)
            return 0f;

        return useActualRudderValue
            ? shipStatuses.ActualRudderSignedValue
            : shipStatuses.TargetRudderSignedValue;
    }

    private float GetMaxRudderAngleDegrees()
    {
        if (shipConfig == null)
            return 0f;

        return Mathf.Max(0f, shipConfig.MaxRudderAngleDegrees);
    }

    private void ApplyToRudders(float signedRudderValue)
    {
        float clampedValue = Mathf.Clamp(signedRudderValue, -1f, 1f);
        maxRudderAngleDegreesFromConfig = GetMaxRudderAngleDegrees();
        visualRudderAngleDegrees = clampedValue * maxRudderAngleDegreesFromConfig;

        if (rudders == null)
            return;

        for (int i = 0; i < rudders.Length; i++)
        {
            rudders[i]?.Apply(clampedValue, maxRudderAngleDegreesFromConfig);
        }
    }

    private void OnValidate()
    {
        visualSmoothSpeed = Mathf.Max(0f, visualSmoothSpeed);

        if (!Application.isPlaying)
        {
            ResolveReferences();
        }
    }
}
