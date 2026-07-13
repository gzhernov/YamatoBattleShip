using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class BearingPanelView : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Абсолютный индикатор направления на цель в градусах 0..360.")]
    [SerializeField] private RadialSlider targetBearingGauge;

    [Tooltip("Относительный индикатор направления на цель относительно курса в формате P/S.")]
    [SerializeField] private PortStarboardRadialSlider portStarboardBearingGauge;

    [Tooltip("Текстовое поле с текущим target bearing.")]
    [SerializeField] private TMP_Text targetBearingText;

    [Tooltip("Контроллер платформы, из которого читается текущий курс.")]
    [SerializeField] private PlatformController platformController;

    [Tooltip("Подсистема главной цели, в которой хранится target bearing.")]
    [SerializeField] private MainGunTargetSubSystem mainGunTargetSubSystem;

    [Header("Настройки")]
    [Tooltip("Если включено, компонент пишет ошибки конфигурации в лог.")]
    [SerializeField] private bool logConfigurationErrors = true;

    private bool isSubscribed;
    private bool isSynchronizing;
    private float lastObservedCourse = float.NaN;
    private float lastObservedTargetBearing = float.NaN;

    private void Awake()
    {
        ValidateReferences();
    }

    private void OnEnable()
    {
        ValidateReferences();
        Subscribe();
        RefreshFromPlatformState();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnValidate()
    {
        ValidateReferences();
    }

    private void Update()
    {
        if (platformController == null || mainGunTargetSubSystem == null || isSynchronizing)
        {
            return;
        }

        float currentCourse = platformController.GetCurrentCourse();
        float currentTargetBearing = mainGunTargetSubSystem.GetTargetBearing();

        if (Mathf.Abs(Mathf.DeltaAngle(lastObservedCourse, currentCourse)) <= 0.001f
            && Mathf.Abs(Mathf.DeltaAngle(lastObservedTargetBearing, currentTargetBearing)) <= 0.001f)
        {
            return;
        }

        RefreshFromPlatformState();
    }

    public void RefreshFromPlatformState()
    {
        if (platformController == null || mainGunTargetSubSystem == null)
        {
            LogConfigurationError("BearingPanelView: не назначены обязательные ссылки на PlatformController и MainGunTargetSubSystem.");
            return;
        }

        ApplyAbsoluteBearing(mainGunTargetSubSystem.GetTargetBearing(), false);
    }

    private void Subscribe()
    {
        if (isSubscribed)
        {
            return;
        }

        if (targetBearingGauge != null)
        {
            targetBearingGauge.OnValueChanged += HandleTargetBearingGaugeValueChanged;
        }

        if (portStarboardBearingGauge != null)
        {
            portStarboardBearingGauge.OnValueChanged += HandlePortStarboardBearingGaugeValueChanged;
        }

        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (targetBearingGauge != null)
        {
            targetBearingGauge.OnValueChanged -= HandleTargetBearingGaugeValueChanged;
        }

        if (portStarboardBearingGauge != null)
        {
            portStarboardBearingGauge.OnValueChanged -= HandlePortStarboardBearingGaugeValueChanged;
        }

        isSubscribed = false;
    }

    private void HandleTargetBearingGaugeValueChanged(float value)
    {
        if (isSynchronizing)
        {
            return;
        }

        ApplyAbsoluteBearing(value, true);
    }

    private void HandlePortStarboardBearingGaugeValueChanged(string value)
    {
        if (isSynchronizing)
        {
            return;
        }

        if (platformController == null)
        {
            LogConfigurationError("BearingPanelView: не назначена ссылка на PlatformController.");
            return;
        }

        if (!TryParsePortStarboardValue(value, out float signedDelta))
        {
            LogConfigurationError($"BearingPanelView: значение относительного bearing '{value}' имеет неверный формат.");
            return;
        }

        float course = platformController.GetCurrentCourse();
        float targetBearing = NormalizeAngle(course + signedDelta);
        ApplyAbsoluteBearing(targetBearing, true);
    }

    private void ApplyAbsoluteBearing(float absoluteBearing, bool updateController)
    {
        float normalizedBearing = NormalizeAngle(absoluteBearing);

        if (updateController)
        {
            if (mainGunTargetSubSystem == null)
            {
                LogConfigurationError("BearingPanelView: не назначена ссылка на MainGunTargetSubSystem.");
            }
            else
            {
                mainGunTargetSubSystem.SetTargetBearing(normalizedBearing);
            }
        }

        float currentCourse = platformController != null
            ? platformController.GetCurrentCourse()
            : 0f;

        string relativeBearing = ConvertAbsoluteBearingToPortStarboard(currentCourse, normalizedBearing);

        isSynchronizing = true;

        if (targetBearingGauge != null)
        {
            targetBearingGauge.SetValueWithoutNotify(normalizedBearing);
        }

        if (portStarboardBearingGauge != null)
        {
            portStarboardBearingGauge.SetValueWithoutNotify(relativeBearing);
        }

        if (targetBearingText != null)
        {
            targetBearingText.text = FormatBearingText(normalizedBearing);
        }

        isSynchronizing = false;
        lastObservedCourse = currentCourse;
        lastObservedTargetBearing = normalizedBearing;
    }

    private string ConvertAbsoluteBearingToPortStarboard(float course, float targetBearing)
    {
        float signedDelta = Mathf.DeltaAngle(course, targetBearing);

        if (Mathf.Approximately(Mathf.Abs(signedDelta), 180f))
        {
            return "S180";
        }

        int magnitude = Mathf.Clamp(Mathf.RoundToInt(Mathf.Abs(signedDelta)), 0, 180);
        return signedDelta < 0f
            ? $"P{magnitude.ToString(CultureInfo.InvariantCulture)}"
            : $"S{magnitude.ToString(CultureInfo.InvariantCulture)}";
    }

    private bool TryParsePortStarboardValue(string value, out float signedDelta)
    {
        signedDelta = 0f;

        if (string.IsNullOrEmpty(value) || value.Length < 2)
        {
            return false;
        }

        char prefix = char.ToUpperInvariant(value[0]);
        string magnitudeText = value.Substring(1);

        if (!int.TryParse(magnitudeText, NumberStyles.None, CultureInfo.InvariantCulture, out int magnitude))
        {
            return false;
        }

        if (magnitude < 0 || magnitude > 180)
        {
            return false;
        }

        if (prefix == 'P')
        {
            signedDelta = -magnitude;
            return true;
        }

        if (prefix == 'S')
        {
            signedDelta = magnitude;
            return true;
        }

        return false;
    }

    private void ValidateReferences()
    {
        if (targetBearingGauge == null)
        {
            LogConfigurationError("BearingPanelView: не назначена ссылка на TargetBearingGauge.");
        }

        if (portStarboardBearingGauge == null)
        {
            LogConfigurationError("BearingPanelView: не назначена ссылка на PortStarboardBearingGauge.");
        }

        if (targetBearingText == null)
        {
            LogConfigurationError("BearingPanelView: не назначена ссылка на текст bearing.");
        }

        if (platformController == null)
        {
            LogConfigurationError("BearingPanelView: не назначена ссылка на PlatformController.");
        }

        if (mainGunTargetSubSystem == null)
        {
            LogConfigurationError("BearingPanelView: не назначена ссылка на MainGunTargetSubSystem.");
        }
    }

    private string FormatBearingText(float bearing)
    {
        return Mathf.RoundToInt(bearing).ToString(CultureInfo.InvariantCulture);
    }

    private static float NormalizeAngle(float value)
    {
        return Mathf.Repeat(value, 360f);
    }

    private void LogConfigurationError(string message)
    {
        if (logConfigurationErrors)
        {
            Debug.LogError(message, this);
        }
    }
}
