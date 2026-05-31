using System.Globalization;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class NavigationView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShipStatuses shipStatuses;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text courseText;

    private bool subscribed;

    private void OnEnable()
    {
        ResolveReferences();

        if (shipStatuses == null)
        {
            Debug.LogError("NavigationView: ShipStatuses not found.", this);
            enabled = false;
            return;
        }

        Subscribe();
        ApplyCurrentStatus();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void ResolveReferences()
    {
        if (shipStatuses == null)
        {
            shipStatuses = FindFirstObjectByType<ShipStatuses>();
        }

        speedText = speedText != null ? speedText : FindTextByName("Speed");
        courseText = courseText != null ? courseText : FindTextByName("Course");
    }

    private TMP_Text FindTextByName(string objectName)
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (text != null && text.gameObject.name == objectName)
            {
                return text;
            }
        }

        return null;
    }

    private void Subscribe()
    {
        if (subscribed)
        {
            return;
        }

        shipStatuses.OnNavigationStatusChanged += OnNavigationStatusChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || shipStatuses == null)
        {
            return;
        }

        shipStatuses.OnNavigationStatusChanged -= OnNavigationStatusChanged;
        subscribed = false;
    }

    private void ApplyCurrentStatus()
    {
        ApplyNavigationStatus(
            shipStatuses.CurrentSpeedKnots,
            shipStatuses.CurrentCourseDegrees
        );
    }

    private void OnNavigationStatusChanged(float speedKnots, float courseDegrees)
    {
        ApplyNavigationStatus(speedKnots, courseDegrees);
    }

    private void ApplyNavigationStatus(float speedKnots, float courseDegrees)
    {
        SetText(speedText, FormatSpeed(speedKnots));
        SetText(courseText, FormatCourse(courseDegrees));
    }

    private static void SetText(TMP_Text label, string value)
    {
        if (label == null)
        {
            return;
        }

        label.text = value;
    }

    private static string FormatSpeed(float speedKnots)
    {
        if (float.IsNaN(speedKnots) || float.IsInfinity(speedKnots))
        {
            return "--.-";
        }

        return speedKnots.ToString("0.0", CultureInfo.InvariantCulture);
    }

    private static string FormatCourse(float courseDegrees)
    {
        if (float.IsNaN(courseDegrees) || float.IsInfinity(courseDegrees))
        {
            return "---";
        }

        int roundedCourse = Mathf.RoundToInt(Mathf.Repeat(courseDegrees, 360f)) % 360;
        return roundedCourse.ToString("000", CultureInfo.InvariantCulture);
    }
}
