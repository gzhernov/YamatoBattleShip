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

    [Header("Настройки")]
    [Tooltip("Если включено, компонент пишет ошибки конфигурации в лог.")]
    [SerializeField] private bool logConfigurationErrors = true;

    private int lastDisplayedBearing = int.MinValue;
    private int lastDisplayedDistance = int.MinValue;

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

        if (bearingOdometer != null && lastDisplayedBearing != roundedBearing)
        {
            ApplyBearing(roundedBearing);
        }

        if (distanceOdometer != null && lastDisplayedDistance != roundedDistance)
        {
            ApplyDistance(roundedDistance);
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
    }

    private void LogConfigurationError(string message)
    {
        if (logConfigurationErrors)
        {
            Debug.LogError(message, this);
        }
    }
}
