using UnityEngine;

[DisallowMultipleComponent]
public class TargetInfoPanelView : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Подсистема главной цели, из которой читается bearing цели.")]
    [SerializeField] private MainGunTargetSubSystem mainGunTargetSubSystem;

    [Tooltip("Одометр, отображающий абсолютный bearing цели.")]
    [SerializeField] private OdometerController bearingOdometer;

    [Tooltip("Одометр, отображающий дистанцию до цели.")]
    [SerializeField] private OdometerController distanceOdometer;

    [Tooltip("Одометр, отображающий курс цели.")]
    [SerializeField] private OdometerController courseOdometer;

    [Tooltip("Одометр, отображающий скорость цели.")]
    [SerializeField] private OdometerController speedOdometer;

    [Header("Настройки")]
    [Tooltip("Если включено, компонент пишет ошибки конфигурации в лог.")]
    [SerializeField] private bool logConfigurationErrors = true;

    private int lastDisplayedBearing = int.MinValue;
    private int lastDisplayedDistance = int.MinValue;
    private int lastDisplayedCourse = int.MinValue;
    private int lastDisplayedSpeed = int.MinValue;

    private void Awake()
    {
        ValidateReferences();
    }

    private void OnEnable()
    {
        ValidateReferences();
        Refresh();
    }

    private void OnValidate()
    {
        ValidateReferences();
    }

    private void Update()
    {
        if (mainGunTargetSubSystem == null)
        {
            return;
        }

        int roundedBearing = Mathf.RoundToInt(mainGunTargetSubSystem.GetTargetBearing());
        int roundedDistance = Mathf.RoundToInt(mainGunTargetSubSystem.GetTargetDistanse());
        int roundedCourse = Mathf.RoundToInt(mainGunTargetSubSystem.GetTargetCourse());
        int roundedSpeed = Mathf.RoundToInt(mainGunTargetSubSystem.GetTargetSpeed());

        if (bearingOdometer != null && lastDisplayedBearing != roundedBearing)
        {
            ApplyBearing(roundedBearing);
        }

        if (distanceOdometer != null && lastDisplayedDistance != roundedDistance)
        {
            ApplyDistance(roundedDistance);
        }

        if (courseOdometer != null && lastDisplayedCourse != roundedCourse)
        {
            ApplyCourse(roundedCourse);
        }

        if (speedOdometer != null && lastDisplayedSpeed != roundedSpeed)
        {
            ApplySpeed(roundedSpeed);
        }
    }

    public void Refresh()
    {
        if (mainGunTargetSubSystem == null)
        {
            LogConfigurationError("TargetInfoPanelView: не назначена ссылка на MainGunTargetSubSystem.");
            return;
        }

        RefreshBearing();
        RefreshDistance();
        RefreshCourse();
        RefreshSpeed();
    }

    private void ApplyBearing(int bearing)
    {
        bearingOdometer.SetValueWithoutNotify(bearing);
        lastDisplayedBearing = bearing;
    }

    private void ApplyDistance(int distance)
    {
        distanceOdometer.SetValueWithoutNotify(distance);
        lastDisplayedDistance = distance;
    }

    private void ApplyCourse(int course)
    {
        courseOdometer.SetValueWithoutNotify(course);
        lastDisplayedCourse = course;
    }

    private void ApplySpeed(int speed)
    {
        speedOdometer.SetValueWithoutNotify(speed);
        lastDisplayedSpeed = speed;
    }

    private void RefreshBearing()
    {
        if (bearingOdometer == null)
        {
            LogConfigurationError("TargetInfoPanelView: не назначена ссылка на BearingOdometr.");
            return;
        }

        ApplyBearing(Mathf.RoundToInt(mainGunTargetSubSystem.GetTargetBearing()));
    }

    private void RefreshDistance()
    {
        if (distanceOdometer == null)
        {
            LogConfigurationError("TargetInfoPanelView: не назначена ссылка на DistanceOdometr.");
            return;
        }

        ApplyDistance(Mathf.RoundToInt(mainGunTargetSubSystem.GetTargetDistanse()));
    }

    private void RefreshCourse()
    {
        if (courseOdometer == null)
        {
            LogConfigurationError("TargetInfoPanelView: не назначена ссылка на CourseOdometr.");
            return;
        }

        ApplyCourse(Mathf.RoundToInt(mainGunTargetSubSystem.GetTargetCourse()));
    }

    private void RefreshSpeed()
    {
        if (speedOdometer == null)
        {
            LogConfigurationError("TargetInfoPanelView: не назначена ссылка на SpeedOdometr.");
            return;
        }

        ApplySpeed(Mathf.RoundToInt(mainGunTargetSubSystem.GetTargetSpeed()));
    }

    private void ValidateReferences()
    {
        if (mainGunTargetSubSystem == null)
        {
            LogConfigurationError("TargetInfoPanelView: не назначена ссылка на MainGunTargetSubSystem.");
        }

        if (bearingOdometer == null)
        {
            LogConfigurationError("TargetInfoPanelView: не назначена ссылка на BearingOdometr.");
        }

        if (distanceOdometer == null)
        {
            LogConfigurationError("TargetInfoPanelView: не назначена ссылка на DistanceOdometr.");
        }

        if (courseOdometer == null)
        {
            LogConfigurationError("TargetInfoPanelView: не назначена ссылка на CourseOdometr.");
        }

        if (speedOdometer == null)
        {
            LogConfigurationError("TargetInfoPanelView: не назначена ссылка на SpeedOdometr.");
        }
    }

    private void LogConfigurationError(string message)
    {
        if (logConfigurationErrors)
        {
            Debug.LogError(message, this);
        }
    }
}
