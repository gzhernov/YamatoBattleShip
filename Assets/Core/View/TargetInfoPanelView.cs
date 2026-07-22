using UnityEngine;

[DisallowMultipleComponent]
public class TargetInfoPanelView : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Подсистема главной цели, из которой читается bearing цели.")]
    [SerializeField] private MainGunTargetSubSystem mainGunTargetSubSystem;

    [Tooltip("Одометр, отображающий абсолютный bearing цели.")]
    [SerializeField] private OdometerController bearingOdometer;

    [Header("Настройки")]
    [Tooltip("Если включено, компонент пишет ошибки конфигурации в лог.")]
    [SerializeField] private bool logConfigurationErrors = true;

    private int lastDisplayedBearing = int.MinValue;

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
        if (mainGunTargetSubSystem == null || bearingOdometer == null)
        {
            return;
        }

        int roundedBearing = Mathf.RoundToInt(mainGunTargetSubSystem.GetTargetBearing());

        if (lastDisplayedBearing == roundedBearing)
        {
            return;
        }

        ApplyBearing(roundedBearing);
    }

    public void Refresh()
    {
        if (mainGunTargetSubSystem == null)
        {
            LogConfigurationError("TargetInfoPanelController: не назначена ссылка на MainGunTargetSubSystem.");
            return;
        }

        if (bearingOdometer == null)
        {
            LogConfigurationError("TargetInfoPanelController: не назначена ссылка на BearingOdometr.");
            return;
        }

        ApplyBearing(Mathf.RoundToInt(mainGunTargetSubSystem.GetTargetBearing()));
    }

    private void ApplyBearing(int bearing)
    {
        bearingOdometer.SetValueWithoutNotify(bearing);
        lastDisplayedBearing = bearing;
    }

    private void ValidateReferences()
    {
        if (mainGunTargetSubSystem == null)
        {
            LogConfigurationError("TargetInfoPanelController: не назначена ссылка на MainGunTargetSubSystem.");
        }

        if (bearingOdometer == null)
        {
            LogConfigurationError("TargetInfoPanelController: не назначена ссылка на BearingOdometr.");
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
