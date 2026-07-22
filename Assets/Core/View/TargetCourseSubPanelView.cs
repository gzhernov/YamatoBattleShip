using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class TargetCourseSubPanelView : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Круговой контроллер курса цели, из которого считывается текущее значение.")]
    [SerializeField] private RadialSlider courseGauge;

    [Tooltip("Текстовое поле, показывающее текущий курс цели.")]
    [SerializeField] private TMP_Text courseValueText;

    [Tooltip("Подсистема главной цели, в которую записывается целевой курс.")]
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

        if (courseGauge != null && !isSubscribed)
        {
            courseGauge.OnValueChanged += HandleCourseValueChanged;
            isSubscribed = true;
        }

        RefreshFromGauge();
    }

    private void OnDisable()
    {
        if (courseGauge != null && isSubscribed)
        {
            courseGauge.OnValueChanged -= HandleCourseValueChanged;
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
            LogConfigurationError("TargetCourseSubPanelView: не назначена ссылка на CourseGauge.");
            return;
        }

        ApplyCourseValue(courseGauge.Value);
    }

    private void HandleCourseValueChanged(float value)
    {
        ApplyCourseValue(value);
    }

    private void ApplyCourseValue(float value)
    {
        float normalizedCourse = NormalizeCourse(value);

        if (courseValueText != null)
        {
            courseValueText.text = FormatCourseText(normalizedCourse);
        }
        else
        {
            LogConfigurationError("TargetCourseSubPanelView: не назначена ссылка на courseValueText.");
        }

        if (mainGunTargetSubSystem != null)
        {
            mainGunTargetSubSystem.SetTargetCourse(normalizedCourse);
        }
        else
        {
            LogConfigurationError("TargetCourseSubPanelView: не назначена ссылка на MainGunTargetSubSystem.");
        }
    }

    private void ValidateReferences()
    {
        if (courseGauge == null)
        {
            LogConfigurationError("TargetCourseSubPanelView: не назначена ссылка на CourseGauge.");
        }

        if (courseValueText == null)
        {
            LogConfigurationError("TargetCourseSubPanelView: не назначена ссылка на courseValueText.");
        }

        if (mainGunTargetSubSystem == null)
        {
            LogConfigurationError("TargetCourseSubPanelView: не назначена ссылка на MainGunTargetSubSystem.");
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
