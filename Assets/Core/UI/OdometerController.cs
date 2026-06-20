using System;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class OdometerController : MonoBehaviour
{
    [Header("Разряды")]
    [Tooltip("Количество цифровых разрядов одометра. Значение меньше 1 будет автоматически исправлено.")]
    [SerializeField, Min(1)] private int sectorCount = 4;

    [Tooltip("Список текстовых элементов по разрядам слева направо. Каждый элемент отображает одну цифру.")]
    [SerializeField] private TMP_Text[] digitTexts = Array.Empty<TMP_Text>();

    [Tooltip("Стартовое значение одометра, которое применяется без звука при инициализации.")]
    [SerializeField, Min(0)] private int startValue;

    [Tooltip("Текущее тестовое значение для просмотра одометра в инспекторе и редакторе.")]
    [SerializeField] private int currentTestValue;

    [Header("Звук")]
    [Tooltip("Источник звука для щелчка одометра. Если не задан, будет использован AudioSource с этого объекта.")]
    [SerializeField] private AudioSource changeAudioSource;

    [Tooltip("Клип, который проигрывается при фактическом изменении значения одометра.")]
    [SerializeField] private AudioClip changeAudioClip;

    [Tooltip("Громкость звука изменения значения.")]
    [SerializeField, Range(0f, 1f)] private float changeAudioVolume = 1f;

    [Header("Отладка")]
    [Tooltip("Если включено, компонент будет писать предупреждения о неполной конфигурации и переполнении разрядов.")]
    [SerializeField] private bool logConfigurationErrors = true;

    private int currentValue;
    private bool hasInitialized;
    private int lastValidatedTestValue = -1;

    public int Value => currentValue;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (!hasInitialized)
        {
            Initialize();
            return;
        }

        RefreshVisuals();
    }

    private void OnValidate()
    {
        sectorCount = Mathf.Max(1, sectorCount);
        startValue = Mathf.Max(0, startValue);
        currentTestValue = Mathf.Max(0, currentTestValue);

        if (changeAudioSource == null)
        {
            changeAudioSource = GetComponent<AudioSource>();
        }

        if (lastValidatedTestValue != currentTestValue || !hasInitialized)
        {
            ApplyInspectorTestValue();
        }
    }

    public void SetValue(int value)
    {
        SetValueInternal(value, true);
    }

    public void SetValueWithoutNotify(int value)
    {
        SetValueInternal(value, false);
    }

    private void Initialize()
    {
        if (changeAudioSource == null)
        {
            changeAudioSource = GetComponent<AudioSource>();
        }

        currentValue = Mathf.Max(0, startValue);
        currentTestValue = currentValue;
        lastValidatedTestValue = currentValue;
        hasInitialized = true;
        RefreshVisuals();
    }

    private void SetValueInternal(int value, bool playSound)
    {
        int clampedValue = Mathf.Max(0, value);

        if (currentValue == clampedValue)
        {
            return;
        }

        currentValue = clampedValue;
        currentTestValue = currentValue;
        lastValidatedTestValue = currentValue;
        RefreshVisuals();

        if (playSound)
        {
            PlayChangeSound();
        }
    }

    private void RefreshVisuals()
    {
        string formattedValue = FormatValue(currentValue);
        int writableDigitCount = Mathf.Min(sectorCount, digitTexts != null ? digitTexts.Length : 0);

        if (digitTexts == null || digitTexts.Length < sectorCount)
        {
            LogConfigurationWarning(
                $"OdometerController: для {sectorCount} разрядов назначено только {digitTexts?.Length ?? 0} текстовых элементов.");
        }

        for (int i = 0; i < writableDigitCount; i++)
        {
            TMP_Text digitText = digitTexts[i];

            if (digitText == null)
            {
                LogConfigurationWarning(
                    $"OdometerController: отсутствует ссылка на TMP_Text для разряда {i}.");
                continue;
            }

            digitText.text = formattedValue[i].ToString();
        }
    }

    private string FormatValue(int value)
    {
        string digitsOnly = Mathf.Max(0, value).ToString();

        if (digitsOnly.Length > sectorCount)
        {
            LogConfigurationWarning(
                $"OdometerController: значение {digitsOnly} не помещается в {sectorCount} разряд(а/ов). Будут показаны младшие цифры.");
            digitsOnly = digitsOnly.Substring(digitsOnly.Length - sectorCount, sectorCount);
        }

        if (digitsOnly.Length < sectorCount)
        {
            digitsOnly = digitsOnly.PadLeft(sectorCount, '0');
        }

        return digitsOnly;
    }

    private void PlayChangeSound()
    {
        if (changeAudioClip == null)
        {
            return;
        }

        if (changeAudioSource == null)
        {
            changeAudioSource = GetComponent<AudioSource>();
        }

        if (changeAudioSource == null)
        {
            LogConfigurationWarning("OdometerController: не найден AudioSource для проигрывания звука изменения.");
            return;
        }

        changeAudioSource.PlayOneShot(changeAudioClip, changeAudioVolume);
    }

    private void ApplyInspectorTestValue()
    {
        lastValidatedTestValue = currentTestValue;

        if (Application.isPlaying && hasInitialized)
        {
            SetValue(currentTestValue);
            return;
        }

        currentValue = currentTestValue;
        RefreshVisuals();
    }

    private void LogConfigurationWarning(string message)
    {
        if (logConfigurationErrors)
        {
            Debug.LogWarning(message, this);
        }
    }
}
