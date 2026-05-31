using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WakeMaterialEntry
{
    [SerializeField] private Material material;
    [SerializeField] private float maxStrength = 1f;
    [SerializeField] private string strengthPropertyName = "_Strength";
    [SerializeField] private AnimationCurve strengthResponseCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(1f, 1f)
    );

    public Material Material => material;
    public float MaxStrength => maxStrength;
    public string StrengthPropertyName => string.IsNullOrWhiteSpace(strengthPropertyName)
        ? "_Strength"
        : strengthPropertyName;
    public AnimationCurve StrengthResponseCurve => strengthResponseCurve;
}

public class ShipWakeController : MonoBehaviour
{
    private const float FallbackSpeedStepKnots = 0.1f;

    [Header("References")]
    [SerializeField] private ShipStatuses shipStatuses;

    [Header("Wake")]
    [SerializeField] private bool enableWake = true;
    [SerializeField] private float speedStepKnots = 0.5f;
    [SerializeField] private List<WakeMaterialEntry> wakeMaterials = new List<WakeMaterialEntry>();

    [Header("Runtime Status (Read Only)")]
    [SerializeField, ReadOnlyInspector] private float currentSpeedKnots;
    [SerializeField, ReadOnlyInspector] private float quantizedSpeedKnots;
    [SerializeField, ReadOnlyInspector] private float currentSpeedNormalized;

    private readonly HashSet<string> warnedPropertyNames = new HashSet<string>();
    private readonly HashSet<Material> warnedMaterials = new HashSet<Material>();

    private bool hasInitializedWake;
    private float lastAppliedQuantizedSpeedKnots = float.MinValue;
    private bool hasWarnedInvalidSpeedStep;

    private ShipConfig Config => shipStatuses != null ? shipStatuses.ShipConfig : null;

    private void Awake()
    {
        ResolveReferences();
        ValidateWakeMaterials();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ValidateWakeMaterials();

        if (shipStatuses == null)
        {
            Debug.LogError("ShipWakeController: ShipStatuses РЅРµ РЅР°Р№РґРµРЅ.", this);
            enabled = false;
            return;
        }

        shipStatuses.OnNavigationStatusChanged += OnNavigationStatusChanged;
        SyncWake(forceApply: true);
    }

    private void OnDisable()
    {
        if (shipStatuses != null)
        {
            shipStatuses.OnNavigationStatusChanged -= OnNavigationStatusChanged;
        }

        ResetWakeStrengths();
        ResetRuntimeState();
    }

    private void ResolveReferences()
    {
        if (shipStatuses == null)
        {
            shipStatuses = GetComponent<ShipStatuses>();

            if (shipStatuses == null)
            {
                shipStatuses = GetComponentInParent<ShipStatuses>();
            }
        }
    }

    private void ValidateWakeMaterials()
    {
        warnedPropertyNames.Clear();
        warnedMaterials.Clear();

        for (int i = 0; i < wakeMaterials.Count; i++)
        {
            WakeMaterialEntry entry = wakeMaterials[i];

            if (entry == null)
                continue;

            Material material = entry.Material;
            if (material == null)
                continue;

            string propertyName = entry.StrengthPropertyName;
            if (!material.HasProperty(propertyName))
            {
                WarnMissingProperty(material, propertyName);
            }
        }
    }

    private void OnNavigationStatusChanged(float speedKnots, float courseDegrees)
    {
        currentSpeedKnots = Mathf.Abs(speedKnots);
        SyncWake(forceApply: false);
    }

    private void SyncWake(bool forceApply)
    {
        currentSpeedKnots = shipStatuses != null
            ? Mathf.Abs(shipStatuses.CurrentSpeedKnots)
            : 0f;

        if (!enableWake || Config == null)
        {
            ResetWakeStrengths();
            ResetRuntimeState(keepCurrentSpeed: true);
            return;
        }

        float effectiveSpeedStepKnots = GetEffectiveSpeedStepKnots();
        float newQuantizedSpeedKnots = QuantizeSpeed(currentSpeedKnots, effectiveSpeedStepKnots);

        if (!forceApply && hasInitializedWake && Mathf.Approximately(lastAppliedQuantizedSpeedKnots, newQuantizedSpeedKnots))
        {
            return;
        }

        quantizedSpeedKnots = newQuantizedSpeedKnots;
        currentSpeedNormalized = Mathf.InverseLerp(
            0f,
            Config.MaxEngineTelegraphSpeedKnots,
            quantizedSpeedKnots
        );

        ApplyWakeStrengths(currentSpeedNormalized);
        lastAppliedQuantizedSpeedKnots = newQuantizedSpeedKnots;
        hasInitializedWake = true;
    }

    private float GetEffectiveSpeedStepKnots()
    {
        if (speedStepKnots > 0f)
            return speedStepKnots;

        if (!hasWarnedInvalidSpeedStep)
        {
            Debug.LogWarning(
                $"ShipWakeController: speedStepKnots РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ Р±РѕР»СЊС€Рµ 0. РСЃРїРѕР»СЊР·СѓРµС‚СЃСЏ fallback {FallbackSpeedStepKnots:F1}.",
                this
            );
            hasWarnedInvalidSpeedStep = true;
        }

        return FallbackSpeedStepKnots;
    }

    private static float QuantizeSpeed(float speedKnots, float stepKnots)
    {
        if (stepKnots <= 0f)
            return speedKnots;

        return Mathf.Floor(speedKnots / stepKnots) * stepKnots;
    }

    private void ApplyWakeStrengths(float speed01)
    {
        for (int i = 0; i < wakeMaterials.Count; i++)
        {
            WakeMaterialEntry entry = wakeMaterials[i];

            if (entry == null)
                continue;

            Material material = entry.Material;
            if (material == null)
                continue;

            string propertyName = entry.StrengthPropertyName;
            if (!material.HasProperty(propertyName))
            {
                WarnMissingProperty(material, propertyName);
                continue;
            }

            float strengthFactor = EvaluateStrengthFactor(entry, speed01);
            float targetStrength = Mathf.Max(0f, entry.MaxStrength) * strengthFactor;
            material.SetFloat(propertyName, targetStrength);
        }
    }

    private void ResetWakeStrengths()
    {
        for (int i = 0; i < wakeMaterials.Count; i++)
        {
            WakeMaterialEntry entry = wakeMaterials[i];

            if (entry == null)
                continue;

            Material material = entry.Material;
            if (material == null)
                continue;

            string propertyName = entry.StrengthPropertyName;
            if (!material.HasProperty(propertyName))
            {
                WarnMissingProperty(material, propertyName);
                continue;
            }

            material.SetFloat(propertyName, 0f);
        }
    }

    private void ResetRuntimeState(bool keepCurrentSpeed = false)
    {
        if (!keepCurrentSpeed)
        {
            currentSpeedKnots = 0f;
        }

        quantizedSpeedKnots = 0f;
        currentSpeedNormalized = 0f;
        lastAppliedQuantizedSpeedKnots = float.MinValue;
        hasInitializedWake = false;
    }

    private void WarnMissingProperty(Material material, string propertyName)
    {
        if (material == null)
            return;

        string warningKey = $"{material.GetInstanceID()}::{propertyName}";
        if (!warnedPropertyNames.Add(warningKey))
            return;

        warnedMaterials.Add(material);
        Debug.LogWarning(
            $"ShipWakeController: РјР°С‚РµСЂРёР°Р» '{material.name}' РЅРµ СЃРѕРґРµСЂР¶РёС‚ float property '{propertyName}'.",
            this
        );
    }

    private static float EvaluateStrengthFactor(WakeMaterialEntry entry, float speed01)
    {
        float clampedSpeed01 = Mathf.Clamp01(speed01);

        if (entry == null)
            return clampedSpeed01;

        AnimationCurve curve = entry.StrengthResponseCurve;
        if (curve == null || curve.length == 0)
            return clampedSpeed01;

        return Mathf.Clamp01(curve.Evaluate(clampedSpeed01));
    }
}
