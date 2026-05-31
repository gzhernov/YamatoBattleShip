using UnityEngine;

public class ShipWaveMotionController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Дочерний объект с визуальной моделью корабля. Не указывай сюда root-объект корабля.")]
    [SerializeField] private Transform visualRoot;

    [SerializeField] private ShipStatuses shipStatuses;
    [SerializeField] private ShipMovementController movementController;

    [Header("Wave Motion")]
    [SerializeField] private bool enableWaveMotion = true;

    [Header("Runtime Status (Read Only)")]
    [SerializeField, ReadOnlyInspector] private float currentWaveInfluence;
    [SerializeField, ReadOnlyInspector] private float currentHeave;
    [SerializeField, ReadOnlyInspector] private float currentRoll;
    [SerializeField, ReadOnlyInspector] private float currentPitch;

    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;

    private ShipConfig Config => shipStatuses != null ? shipStatuses.ShipConfig : null;

    private void Awake()
    {
        ResolveReferences();
        CacheStartTransform();
    }

    private void OnEnable()
    {
        ResolveReferences();
        CacheStartTransform();
    }

    private void LateUpdate()
    {
        if (visualRoot == null)
            return;

        if (!enableWaveMotion || Config == null)
        {
            ReturnToStartPose();
            return;
        }

        UpdateWaveInfluence();
        ApplyWaveMotion();
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

        if (visualRoot == null)
        {
            Debug.LogWarning(
                "ShipWaveMotionController: Visual Root не назначен. " +
                "Создай дочерний объект VisualRoot с моделью корабля и назначь его сюда.",
                this
            );
        }
    }

    private void CacheStartTransform()
    {
        if (visualRoot == null)
            return;

        startLocalPosition = visualRoot.localPosition;
        startLocalRotation = visualRoot.localRotation;
    }

    private void UpdateWaveInfluence()
    {
        float speedKnots = movementController != null
            ? Mathf.Abs(movementController.CurrentSpeedKnots)
            : 0f;

        float targetInfluence = Mathf.InverseLerp(
            0f,
            Config.SpeedForFullWaveEffectKnots,
            speedKnots
        );

        currentWaveInfluence = Mathf.MoveTowards(
            currentWaveInfluence,
            targetInfluence,
            Config.WaveInfluenceSmoothSpeed * Time.deltaTime
        );
    }

    private void ApplyWaveMotion()
    {
        float time = Time.time * Config.WaveFrequency;
        float fullCircle = Mathf.PI * 2f;

        currentHeave = Mathf.Sin(time * fullCircle) * Config.HeaveAmplitude * currentWaveInfluence;
        currentRoll = Mathf.Sin(time * fullCircle + 1.4f) * Config.RollAmplitude * currentWaveInfluence;
        currentPitch = Mathf.Sin(time * fullCircle * 0.7f + 0.8f) * Config.PitchAmplitude * currentWaveInfluence;

        visualRoot.localPosition = startLocalPosition + new Vector3(0f, currentHeave, 0f);
        visualRoot.localRotation = startLocalRotation * Quaternion.Euler(currentPitch, 0f, currentRoll);
    }

    private void ReturnToStartPose()
    {
        currentWaveInfluence = Mathf.MoveTowards(
            currentWaveInfluence,
            0f,
            Config != null ? Config.WaveInfluenceSmoothSpeed * Time.deltaTime : Time.deltaTime * 2f
        );

        currentHeave = Mathf.Lerp(currentHeave, 0f, Time.deltaTime * 4f);
        currentRoll = Mathf.Lerp(currentRoll, 0f, Time.deltaTime * 4f);
        currentPitch = Mathf.Lerp(currentPitch, 0f, Time.deltaTime * 4f);

        visualRoot.localPosition = startLocalPosition + new Vector3(0f, currentHeave, 0f);
        visualRoot.localRotation = startLocalRotation * Quaternion.Euler(currentPitch, 0f, currentRoll);
    }
}
