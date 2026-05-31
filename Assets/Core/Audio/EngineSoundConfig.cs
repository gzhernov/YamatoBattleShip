using System;
using UnityEngine;

[Serializable]
public class EngineSoundLayerSettings
{
    [Tooltip("Зацикленный клип для этого слоя двигателя.")]
    [SerializeField] private AudioClip clip;

    [Tooltip("Дополнительный множитель громкости этого слоя поверх расчётной runtime-громкости.")]
    [Range(0f, 1f)]
    [SerializeField] private float volumeMultiplier = 1f;

    [Tooltip("Минимальный pitch этого слоя при низких виртуальных оборотах.")]
    [Min(0f)]
    [SerializeField] private float minPitch = 0.95f;

    [Tooltip("Максимальный pitch этого слоя при высоких виртуальных оборотах.")]
    [Min(0f)]
    [SerializeField] private float maxPitch = 1.05f;

    public AudioClip Clip => clip;
    public float VolumeMultiplier => volumeMultiplier;
    public float MinPitch => minPitch;
    public float MaxPitch => maxPitch;

    public void Validate()
    {
        volumeMultiplier = Mathf.Clamp01(volumeMultiplier);
        minPitch = Mathf.Max(0f, minPitch);
        maxPitch = Mathf.Max(minPitch, maxPitch);
    }
}

[CreateAssetMenu(
    fileName = "EngineSoundConfig",
    menuName = "Ship/Engine Sound Config",
    order = 4
)]
public class EngineSoundConfig : ScriptableObject
{
    [Header("Layers")]
    [Tooltip("Базовый звук работающей машины на стоянке или почти без хода.")]
    [SerializeField] private EngineSoundLayerSettings idleLayer = new EngineSoundLayerSettings();
    [Tooltip("Слой переднего хода для малого и среднего режима, который подмешивается раньше heavy-слоя.")]
    [SerializeField] private EngineSoundLayerSettings aheadLowLayer = new EngineSoundLayerSettings();
    [Tooltip("Слой мощного переднего хода, который должен доминировать только после реального разгона.")]
    [SerializeField] private EngineSoundLayerSettings aheadHighLayer = new EngineSoundLayerSettings();
    [Tooltip("Отдельный слой заднего хода/реверса.")]
    [SerializeField] private EngineSoundLayerSettings asternLayer = new EngineSoundLayerSettings();

    [Header("Response")]
    [Tooltip("Скорость, с которой громкость слоёв приближается к целевым значениям. Больше значение = быстрее реакция.")]
    [Min(0f)]
    [SerializeField] private float fadeSpeed = 1.5f;

    [Tooltip("Скорость роста виртуальных оборотов при разгоне. Чем больше значение, тем быстрее двигатель 'раскручивается' на слух.")]
    [Min(0f)]
    [SerializeField] private float rpmRiseSpeed = 0.8f;

    [Tooltip("Скорость падения виртуальных оборотов при сбросе хода. Меньшие значения делают спад более тяжёлым и инерционным.")]
    [Min(0f)]
    [SerializeField] private float rpmFallSpeed = 0.45f;

    [Header("RPM Mix")]
    [Tooltip("Вес фактической скорости корабля в расчёте виртуальных оборотов. Обычно это главный источник для плавного звука.")]
    [Range(0f, 1f)]
    [SerializeField] private float speedInfluence = 0.8f;

    [Tooltip("Вес команды хода/тяги в расчёте виртуальных оборотов. Небольшое значение даёт отклик на приказ без резкого скачка.")]
    [Range(0f, 1f)]
    [SerializeField] private float orderInfluence = 0.1f;

    [Tooltip("Вес нагрузки двигателя в расчёте виртуальных оборотов. Помогает слышать усилие машины до полного набора скорости.")]
    [Range(0f, 1f)]
    [SerializeField] private float loadInfluence = 0.1f;

    [Header("Load")]
    [Tooltip("Опорное ускорение, при котором вклад ускорения в нагрузку считается близким к максимальному.")]
    [Min(0.01f)]
    [SerializeField] private float accelerationReferenceKnotsPerSecond = 1.5f;

    [Tooltip("Насколько сильно ускорение влияет на вычисление нагрузки по сравнению с разницей между приказом и фактической скоростью.")]
    [Range(0f, 1f)]
    [SerializeField] private float accelerationLoadWeight = 0.35f;

    [Header("Layer Blend")]
    [Tooltip("Нормализованная скорость, к которой базовый idle-слой почти исчезает. Меньше значение = idle уходит раньше.")]
    [Min(0.01f)]
    [SerializeField] private float idleFadeOutAtSpeed01 = 0.85f;

    [Tooltip("Точка виртуальных оборотов, около которой слой aheadLow звучит сильнее всего.")]
    [Range(0f, 1f)]
    [SerializeField] private float aheadLowPeakRpm01 = 0.35f;

    [Tooltip("Ширина зоны, в которой слой aheadLow плавно нарастает и затухает вокруг своей пиковой точки.")]
    [Min(0.01f)]
    [SerializeField] private float aheadLowBlendWidth01 = 0.35f;

    [Tooltip("Порог виртуальных оборотов, после которого начинает заметно появляться heavy-слой aheadHigh.")]
    [Range(0f, 1f)]
    [SerializeField] private float aheadHighStartRpm01 = 0.45f;

    [Tooltip("Вес фактической скорости в громкости слоя заднего хода.")]
    [Range(0f, 1f)]
    [SerializeField] private float asternSpeedWeight = 0.7f;

    [Tooltip("Вес нагрузки двигателя в громкости слоя заднего хода.")]
    [Range(0f, 1f)]
    [SerializeField] private float asternLoadWeight = 0.3f;

    [Tooltip("Включает предупреждения в консоли, если для каких-то слоёв не назначены клипы.")]
    [SerializeField] private bool logValidationWarnings = true;

    public EngineSoundLayerSettings IdleLayer => idleLayer;
    public EngineSoundLayerSettings AheadLowLayer => aheadLowLayer;
    public EngineSoundLayerSettings AheadHighLayer => aheadHighLayer;
    public EngineSoundLayerSettings AsternLayer => asternLayer;

    public float FadeSpeed => fadeSpeed;
    public float RpmRiseSpeed => rpmRiseSpeed;
    public float RpmFallSpeed => rpmFallSpeed;
    public float SpeedInfluence => speedInfluence;
    public float OrderInfluence => orderInfluence;
    public float LoadInfluence => loadInfluence;
    public float AccelerationReferenceKnotsPerSecond => accelerationReferenceKnotsPerSecond;
    public float AccelerationLoadWeight => accelerationLoadWeight;
    public float IdleFadeOutAtSpeed01 => idleFadeOutAtSpeed01;
    public float AheadLowPeakRpm01 => aheadLowPeakRpm01;
    public float AheadLowBlendWidth01 => aheadLowBlendWidth01;
    public float AheadHighStartRpm01 => aheadHighStartRpm01;
    public float AsternSpeedWeight => asternSpeedWeight;
    public float AsternLoadWeight => asternLoadWeight;

    public void Validate(UnityEngine.Object context)
    {
        if (!logValidationWarnings)
            return;

        ValidateLayer(idleLayer, "idle", context);
        ValidateLayer(aheadLowLayer, "aheadLow", context);
        ValidateLayer(aheadHighLayer, "aheadHigh", context);
        ValidateLayer(asternLayer, "astern", context);
    }

    private void OnValidate()
    {
        idleLayer?.Validate();
        aheadLowLayer?.Validate();
        aheadHighLayer?.Validate();
        asternLayer?.Validate();

        fadeSpeed = Mathf.Max(0f, fadeSpeed);
        rpmRiseSpeed = Mathf.Max(0f, rpmRiseSpeed);
        rpmFallSpeed = Mathf.Max(0f, rpmFallSpeed);

        speedInfluence = Mathf.Clamp01(speedInfluence);
        orderInfluence = Mathf.Clamp01(orderInfluence);
        loadInfluence = Mathf.Clamp01(loadInfluence);

        accelerationReferenceKnotsPerSecond = Mathf.Max(0.01f, accelerationReferenceKnotsPerSecond);
        accelerationLoadWeight = Mathf.Clamp01(accelerationLoadWeight);

        idleFadeOutAtSpeed01 = Mathf.Max(0.01f, idleFadeOutAtSpeed01);
        aheadLowPeakRpm01 = Mathf.Clamp01(aheadLowPeakRpm01);
        aheadLowBlendWidth01 = Mathf.Max(0.01f, aheadLowBlendWidth01);
        aheadHighStartRpm01 = Mathf.Clamp01(aheadHighStartRpm01);
        asternSpeedWeight = Mathf.Clamp01(asternSpeedWeight);
        asternLoadWeight = Mathf.Clamp01(asternLoadWeight);

        Validate(this);
    }

    private void ValidateLayer(EngineSoundLayerSettings layer, string layerName, UnityEngine.Object context)
    {
        if (layer == null || layer.Clip != null)
            return;

        Debug.LogWarning($"EngineSoundConfig '{name}': {layerName} clip is not assigned.", context);
    }
}
