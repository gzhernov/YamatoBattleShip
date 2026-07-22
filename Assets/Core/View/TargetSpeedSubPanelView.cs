using System.Globalization;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class TargetSpeedSubPanelView : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Радиальный переключатель скорости цели, из которого считывается текущее положение.")]
    [SerializeField] private RadialSwitcher speedGauge;

    [Tooltip("Текстовое поле, показывающее текущую скорость цели.")]
    [SerializeField] private TMP_Text speedValueText;

    [Tooltip("Подсистема главной цели, в которую записывается целевая скорость.")]
    [SerializeField] private MainGunTargetSubSystem mainGunTargetSubSystem;

    [Header("Настройки")]
    [Tooltip("Если включено, компонент пишет ошибки конфигурации в лог.")]
    [SerializeField] private bool logConfigurationErrors = true;

    private bool isSubscribed;

    private void Awake()
    {
        ValidateReferences();
    }

    private void OnEnable()
    {
        ValidateReferences();

        if (speedGauge != null && !isSubscribed)
        {
            speedGauge.OnPositionChanged += HandleSpeedValueChanged;
            isSubscribed = true;
        }

        RefreshFromGauge();
    }

    private void OnDisable()
    {
        if (speedGauge != null && isSubscribed)
        {
            speedGauge.OnPositionChanged -= HandleSpeedValueChanged;
        }

        isSubscribed = false;
    }

    private void OnValidate()
    {
        ValidateReferences();
    }

    public void RefreshFromGauge()
    {
        if (speedGauge == null)
        {
            LogConfigurationError("TargetSpeedSubPanelView: не назначена ссылка на SpeedGauge.");
            return;
        }

        ApplySpeedValue(speedGauge.CurrentPositionId);
    }

    private void HandleSpeedValueChanged(string value)
    {
        ApplySpeedValue(value);
    }

    private void ApplySpeedValue(string value)
    {
        if (!TryParseSpeed(value, out float normalizedSpeed))
        {
            LogConfigurationError($"TargetSpeedSubPanelView: значение скорости '{value}' имеет неверный формат.");
            return;
        }

        if (speedValueText != null)
        {
            speedValueText.text = FormatSpeedText(normalizedSpeed);
        }
        else
        {
            LogConfigurationError("TargetSpeedSubPanelView: не назначена ссылка на speedValueText.");
        }

        if (mainGunTargetSubSystem != null)
        {
            mainGunTargetSubSystem.SetTargetSpeed(normalizedSpeed);
        }
        else
        {
            LogConfigurationError("TargetSpeedSubPanelView: не назначена ссылка на MainGunTargetSubSystem.");
        }
    }

    private bool TryParseSpeed(string value, out float speed)
    {
        bool hasParsed = float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out speed);
        speed = Mathf.Max(0f, speed);
        return hasParsed;
    }

    private string FormatSpeedText(float value)
    {
        return Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);
    }

    private void ValidateReferences()
    {
        if (speedGauge == null)
        {
            LogConfigurationError("TargetSpeedSubPanelView: не назначена ссылка на SpeedGauge.");
        }

        if (speedValueText == null)
        {
            LogConfigurationError("TargetSpeedSubPanelView: не назначена ссылка на speedValueText.");
        }

        if (mainGunTargetSubSystem == null)
        {
            LogConfigurationError("TargetSpeedSubPanelView: не назначена ссылка на MainGunTargetSubSystem.");
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
