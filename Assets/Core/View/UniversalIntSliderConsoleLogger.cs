using UnityEngine;

[DisallowMultipleComponent]
public class UniversalIntSliderConsoleLogger : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Слайдер, за изменениями которого нужно следить.")]
    [SerializeField] private UniversalIntSlider slider;

    [Header("Логирование")]
    [Tooltip("Если включено, текущее значение будет выведено сразу при включении компонента.")]
    [SerializeField] private bool logCurrentValueOnEnable = true;

    [Tooltip("Префикс для сообщений в консоли.")]
    [SerializeField] private string logPrefix = "UniversalIntSliderConsoleLogger";

    private void OnEnable()
    {
        if (slider == null)
        {
            Debug.LogError("UniversalIntSliderConsoleLogger: Не назначен UniversalIntSlider.", this);
            return;
        }

        slider.OnValueChanged -= HandleSliderValueChanged;
        slider.OnValueChanged += HandleSliderValueChanged;

        if (logCurrentValueOnEnable)
        {
            LogSliderValue(slider.Value);
        }
    }

    private void OnDisable()
    {
        if (slider == null)
        {
            return;
        }

        slider.OnValueChanged -= HandleSliderValueChanged;
    }

    public void HandleSliderValueChanged(int newValue)
    {
        LogSliderValue(newValue);
    }

    private void LogSliderValue(int value)
    {
        Debug.Log($"{logPrefix}: значение слайдера = {value}", this);
    }
}
