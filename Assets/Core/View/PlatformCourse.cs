using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PlatformCourse : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Круговой контроллер курса, из которого считывается текущее значение.")]
    [SerializeField] private RadialSlider courseGauge;

    [Tooltip("Текстовое поле для отображения текущего курса.")]
    [SerializeField] private TMP_Text currentCourseText;

    [FormerlySerializedAs("turrentPlatformController")]
    [FormerlySerializedAs("platformController")]
    [Tooltip("Контроллер платформы, который получает новый курс.")]
    [SerializeField] private TurrentPlatformController turretPlatformController;

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

        if (courseGauge != null && !isSubscribed)
        {
            courseGauge.OnValueChanged += HandleCourseGaugeValueChanged;
            isSubscribed = true;
        }

        RefreshFromGauge();
    }

    private void OnDisable()
    {
        if (courseGauge != null && isSubscribed)
        {
            courseGauge.OnValueChanged -= HandleCourseGaugeValueChanged;
        }

        isSubscribed = false;
    }

    private void OnValidate()
    {
        ValidateReferences();
    }

    public void RefreshFromGauge()
    {
        if (courseGauge == null)
        {
            LogConfigurationError("PlatformCourse: не назначен CourseGauge.");
            return;
        }

        ApplyCourseValue(courseGauge.Value);
    }

    private void HandleCourseGaugeValueChanged(float value)
    {
        ApplyCourseValue(value);
    }

    private void ApplyCourseValue(float value)
    {
        float normalizedCourse = NormalizeCourse(value);

        if (currentCourseText != null)
        {
            currentCourseText.text = FormatCourseText(normalizedCourse);
        }

        if (turretPlatformController != null)
        {
            turretPlatformController.SetCourse(normalizedCourse);
        }
        else
        {
            LogConfigurationError("PlatformCourse: не назначен PlatformController.");
        }
    }

    private void ValidateReferences()
    {
        if (courseGauge == null)
        {
            LogConfigurationError("PlatformCourse: не назначена ссылка на CourseGauge.");
        }

        if (currentCourseText == null)
        {
            LogConfigurationError("PlatformCourse: не назначена ссылка на currentCourse.");
        }

        if (turretPlatformController == null)
        {
            LogConfigurationError("PlatformCourse: не назначена ссылка на PlatformController.");
        }
    }

    private string FormatCourseText(float value)
    {
        return Mathf.RoundToInt(value).ToString();
    }

    private static float NormalizeCourse(float value)
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
