using UnityEngine;

[DisallowMultipleComponent]
public class CompassView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShipStatuses shipStatuses;
    [SerializeField] private RectTransform compassNeedleRect;

    [Header("Behavior")]
    [SerializeField, Min(0f)] private float visualZeroOffsetDegrees = 90f;

    private bool isSubscribed;

    private void OnEnable()
    {
        ResolveReferences();

        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }

        Subscribe();
        ApplyCurrentCourse();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnValidate()
    {
        if (compassNeedleRect == null)
        {
            compassNeedleRect = GetComponent<RectTransform>();
        }
    }

    private void ResolveReferences()
    {
        if (shipStatuses == null)
        {
            shipStatuses = FindFirstObjectByType<ShipStatuses>();
        }

        if (compassNeedleRect == null)
        {
            compassNeedleRect = GetComponent<RectTransform>();
        }
    }

    private bool ValidateSetup()
    {
        if (shipStatuses == null)
        {
            Debug.LogError("CompassView: ShipStatuses not found.", this);
            return false;
        }

        if (compassNeedleRect == null)
        {
            Debug.LogError("CompassView: Compass needle RectTransform is not assigned.", this);
            return false;
        }

        return true;
    }

    private void Subscribe()
    {
        if (isSubscribed || shipStatuses == null)
        {
            return;
        }

        shipStatuses.OnNavigationStatusChanged += OnNavigationStatusChanged;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || shipStatuses == null)
        {
            return;
        }

        shipStatuses.OnNavigationStatusChanged -= OnNavigationStatusChanged;
        isSubscribed = false;
    }

    private void ApplyCurrentCourse()
    {
        ApplyCourse(shipStatuses.CurrentCourseDegrees);
    }

    private void OnNavigationStatusChanged(float speedKnots, float courseDegrees)
    {
        ApplyCourse(courseDegrees);
    }

    private void ApplyCourse(float courseDegrees)
    {
        if (compassNeedleRect == null)
        {
            return;
        }

        float normalizedCourse = NormalizeCourse(courseDegrees);
        float targetRotationZ = visualZeroOffsetDegrees - normalizedCourse;

        Vector3 localEulerAngles = compassNeedleRect.localEulerAngles;
        localEulerAngles.z = targetRotationZ;
        compassNeedleRect.localEulerAngles = localEulerAngles;
    }

    private static float NormalizeCourse(float courseDegrees)
    {
        if (float.IsNaN(courseDegrees) || float.IsInfinity(courseDegrees))
        {
            return 0f;
        }

        float normalizedCourse = Mathf.Repeat(courseDegrees, 360f);
        return Mathf.Approximately(normalizedCourse, 360f) ? 0f : normalizedCourse;
    }
}
