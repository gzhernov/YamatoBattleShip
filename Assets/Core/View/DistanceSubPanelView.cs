using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DistanceSubPanelView : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Слайдер дистанции, из которого считывается текущее значение.")]
    [SerializeField] private Slider distanceSlider;

    [Tooltip("Текстовое поле, показывающее текущее значение дистанции.")]
    [SerializeField] private TMP_Text distanceValueText;

    [Tooltip("Подсистема главной цели, в которую записывается целевая дистанция.")]
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
            LogConfigurationError("DistancePanelView: не назначена ссылка на distanceValueText.");
        }

        if (mainGunTargetSubSystem != null)
        {
            mainGunTargetSubSystem.SetTargetDistanse(normalizedDistance);
        }
        else
        {
            LogConfigurationError("DistancePanelView: не назначена ссылка на MainGunTargetSubSystem.");
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
            LogConfigurationError("DistancePanelView: не назначена ссылка на distanceValueText.");
        }

        if (mainGunTargetSubSystem == null)
        {
            LogConfigurationError("DistancePanelView: не назначена ссылка на MainGunTargetSubSystem.");
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
