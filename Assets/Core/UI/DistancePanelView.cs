using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DistancePanelView : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Слайдер дистанции, из которого считывается текущее значение.")]
    [SerializeField] private Slider distanceSlider;

    [Tooltip("Текстовое поле, показывающее текущее значение дистанции.")]
    [SerializeField] private TMP_Text distanceValueText;

    [Tooltip("Контроллер платформы, в который записывается целевая дистанция.")]
    [SerializeField] private PlatformController platformController;

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

        if (distanceSlider != null && !isSubscribed)
        {
            distanceSlider.onValueChanged.AddListener(HandleDistanceValueChanged);
            isSubscribed = true;
        }

        RefreshFromSlider();
    }

    private void OnDisable()
    {
        if (distanceSlider != null && isSubscribed)
        {
            distanceSlider.onValueChanged.RemoveListener(HandleDistanceValueChanged);
        }

        isSubscribed = false;
    }

    private void OnValidate()
    {
        ValidateReferences();
    }

    public void RefreshFromSlider()
    {
        if (distanceSlider == null)
        {
            LogConfigurationError("DistancePanelView: не назначена ссылка на DistanceSlider.");
            return;
        }

        ApplyDistanceValue(distanceSlider.value);
    }

    private void HandleDistanceValueChanged(float value)
    {
        ApplyDistanceValue(value);
    }

    private void ApplyDistanceValue(float value)
    {
        float normalizedDistance = Mathf.Max(0f, value);

        if (distanceValueText != null)
        {
            distanceValueText.text = normalizedDistance.ToString("0.0");
        }
        else
        {
            LogConfigurationError("DistancePanelView: не назначена ссылка на distanceValue.");
        }

        if (platformController != null)
        {
            platformController.SetTargetDistanse(normalizedDistance);
        }
        else
        {
            LogConfigurationError("DistancePanelView: не назначена ссылка на PlatformController.");
        }
    }

    private void ValidateReferences()
    {
        if (distanceSlider == null)
        {
            LogConfigurationError("DistancePanelView: не назначена ссылка на DistanceSlider.");
        }

        if (distanceValueText == null)
        {
            LogConfigurationError("DistancePanelView: не назначена ссылка на distanceValue.");
        }

        if (platformController == null)
        {
            LogConfigurationError("DistancePanelView: не назначена ссылка на PlatformController.");
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
