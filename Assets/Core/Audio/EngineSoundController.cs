using UnityEngine;

public class EngineSoundController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional. If not assigned, the controller will search for ShipStatuses on the parent object first, then in the scene.")]
    [SerializeField] private ShipStatuses shipStatuses;
    [Tooltip("Optional. If not assigned, the controller will search on the current object, then on parents.")]
    [SerializeField] private ShipMovementController movementController;
    [Tooltip("Optional. Overrides the EngineSoundConfig taken from ShipConfig for this controller only.")]
    [SerializeField] private EngineSoundConfig audioConfigOverride;

    [Header("Audio Sources")]
    [Tooltip("Optional. If not assigned, the controller creates a child 2D AudioSource automatically at runtime.")]
    [SerializeField] private AudioSource idleAudioSource;
    [Tooltip("Optional. If not assigned, the controller creates a child 2D AudioSource automatically at runtime.")]
    [SerializeField] private AudioSource aheadLowAudioSource;
    [Tooltip("Optional. If not assigned, the controller creates a child 2D AudioSource automatically at runtime.")]
    [SerializeField] private AudioSource aheadHighAudioSource;
    [Tooltip("Optional. If not assigned, the controller creates a child 2D AudioSource automatically at runtime.")]
    [SerializeField] private AudioSource asternAudioSource;

    [Header("Runtime Blend (Read Only)")]
    [SerializeField, ReadOnlyInspector] private float order01;
    [SerializeField, ReadOnlyInspector] private float speed01;
    [SerializeField, ReadOnlyInspector] private float load01;
    [SerializeField, ReadOnlyInspector] private float astern01;
    [SerializeField, ReadOnlyInspector] private float virtualRpm01;
    [SerializeField, ReadOnlyInspector] private float idleTargetVolume;
    [SerializeField, ReadOnlyInspector] private float aheadLowTargetVolume;
    [SerializeField, ReadOnlyInspector] private float aheadHighTargetVolume;
    [SerializeField, ReadOnlyInspector] private float asternTargetVolume;

    [Header("Debug")]
    [Tooltip("Enables warning logs when references or audio clips are missing.")]
    [SerializeField] private bool logDebugInfo = false;

    private const string IdleAudioChildName = "Engine Idle Audio";
    private const string AheadLowAudioChildName = "Engine Ahead Low Audio";
    private const string AheadHighAudioChildName = "Engine Ahead High Audio";
    private const string AsternAudioChildName = "Engine Astern Audio";

    private bool missingIdleClipLogged;
    private bool missingAheadLowClipLogged;
    private bool missingAheadHighClipLogged;
    private bool missingAsternClipLogged;

    private void OnEnable()
    {
        ResolveReferences();
        EnsureAudioSources();
        ResetAudioSourceState();
        SyncAudioSourcesWithConfig(true);
    }

    private void OnDisable()
    {
        StopAudioSource(idleAudioSource);
        StopAudioSource(aheadLowAudioSource);
        StopAudioSource(aheadHighAudioSource);
        StopAudioSource(asternAudioSource);
    }

    private void OnValidate()
    {
        ConfigureAudioSourceIfPresent(idleAudioSource);
        ConfigureAudioSourceIfPresent(aheadLowAudioSource);
        ConfigureAudioSourceIfPresent(aheadHighAudioSource);
        ConfigureAudioSourceIfPresent(asternAudioSource);
    }

    private void Update()
    {
        EngineSoundConfig activeConfig = GetActiveAudioConfig();

        if (activeConfig == null || movementController == null)
        {
            FadeOutAllAudioSources();
            return;
        }

        SyncAudioSourcesWithConfig(false);
        UpdateRuntimeSignals(activeConfig);
        UpdateLayerTargets(activeConfig);
        ApplyLayers(activeConfig);
    }

    private void ResolveReferences()
    {
        if (shipStatuses == null)
        {
            shipStatuses = GetComponentInParent<ShipStatuses>();
        }

        if (shipStatuses == null)
        {
            shipStatuses = FindFirstObjectByType<ShipStatuses>();
        }

        if (movementController == null)
        {
            movementController = GetComponent<ShipMovementController>();
        }

        if (movementController == null)
        {
            movementController = GetComponentInParent<ShipMovementController>();
        }

        if (movementController == null)
        {
            movementController = FindFirstObjectByType<ShipMovementController>();
        }
    }

    private void EnsureAudioSources()
    {
        if (idleAudioSource == null)
        {
            idleAudioSource = CreateChildAudioSource(IdleAudioChildName);
        }

        if (aheadLowAudioSource == null)
        {
            aheadLowAudioSource = CreateChildAudioSource(AheadLowAudioChildName);
        }

        if (aheadHighAudioSource == null)
        {
            aheadHighAudioSource = CreateChildAudioSource(AheadHighAudioChildName);
        }

        if (asternAudioSource == null)
        {
            asternAudioSource = CreateChildAudioSource(AsternAudioChildName);
        }

        ConfigureAudioSourceIfPresent(idleAudioSource);
        ConfigureAudioSourceIfPresent(aheadLowAudioSource);
        ConfigureAudioSourceIfPresent(aheadHighAudioSource);
        ConfigureAudioSourceIfPresent(asternAudioSource);
    }

    private AudioSource CreateChildAudioSource(string childName)
    {
        Transform child = transform.Find(childName);
        GameObject audioObject;

        if (child != null)
        {
            audioObject = child.gameObject;
        }
        else
        {
            audioObject = new GameObject(childName);
            audioObject.transform.SetParent(transform, false);
        }

        AudioSource audioSource = audioObject.GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = audioObject.AddComponent<AudioSource>();
        }

        return audioSource;
    }

    private static void ConfigureAudioSourceIfPresent(AudioSource audioSource)
    {
        if (audioSource == null)
            return;

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
    }

    private void ResetAudioSourceState()
    {
        ResetAudioSource(idleAudioSource);
        ResetAudioSource(aheadLowAudioSource);
        ResetAudioSource(aheadHighAudioSource);
        ResetAudioSource(asternAudioSource);
    }

    private void SyncAudioSourcesWithConfig(bool forceRestart)
    {
        EngineSoundConfig activeConfig = GetActiveAudioConfig();

        if (activeConfig == null)
            return;

        SyncAudioSourceClip(idleAudioSource, activeConfig.IdleLayer, forceRestart);
        SyncAudioSourceClip(aheadLowAudioSource, activeConfig.AheadLowLayer, forceRestart);
        SyncAudioSourceClip(aheadHighAudioSource, activeConfig.AheadHighLayer, forceRestart);
        SyncAudioSourceClip(asternAudioSource, activeConfig.AsternLayer, forceRestart);
    }

    private void SyncAudioSourceClip(
        AudioSource audioSource,
        EngineSoundLayerSettings layerSettings,
        bool forceRestart
    )
    {
        if (audioSource == null || layerSettings == null)
            return;

        AudioClip targetClip = layerSettings.Clip;
        ref bool missingClipLogged = ref GetMissingClipFlag(audioSource);

        if (audioSource.clip != targetClip)
        {
            audioSource.clip = targetClip;
            forceRestart = true;
            missingClipLogged = false;
        }

        if (targetClip == null)
        {
            StopAudioSource(audioSource);

            if (logDebugInfo && !missingClipLogged)
            {
                Debug.LogWarning("EngineSoundController: an engine layer clip is not assigned.", this);
                missingClipLogged = true;
            }

            return;
        }

        missingClipLogged = false;

        if (forceRestart)
        {
            audioSource.Stop();
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private void UpdateRuntimeSignals(EngineSoundConfig activeConfig)
    {
        ShipConfig shipConfig = shipStatuses != null ? shipStatuses.ShipConfig : null;
        float maxSpeedKnots = shipConfig != null ? shipConfig.MaxEngineTelegraphSpeedKnots : 0f;

        if (maxSpeedKnots <= 0.01f)
        {
            maxSpeedKnots = 1f;
        }

        float currentSpeedKnots = movementController.CurrentSpeedKnots;
        float effectiveOrderSpeedKnots = movementController.EffectiveEngineOrderSpeedKnots;
        float appliedAccelerationKnotsPerSecond = movementController.AppliedAccelerationKnotsPerSecond;

        order01 = Mathf.Clamp01(Mathf.Abs(effectiveOrderSpeedKnots) / maxSpeedKnots);
        speed01 = Mathf.Clamp01(Mathf.Abs(currentSpeedKnots) / maxSpeedKnots);

        float speedGap01 = Mathf.Clamp01(
            Mathf.Max(0f, Mathf.Abs(effectiveOrderSpeedKnots) - Mathf.Abs(currentSpeedKnots)) / maxSpeedKnots
        );
        float accelerationLoad01 = Mathf.Clamp01(
            Mathf.Abs(appliedAccelerationKnotsPerSecond) / activeConfig.AccelerationReferenceKnotsPerSecond
        );

        load01 = Mathf.Clamp01(Mathf.Lerp(speedGap01, accelerationLoad01, activeConfig.AccelerationLoadWeight));
        astern01 = effectiveOrderSpeedKnots < -0.001f || currentSpeedKnots < -0.001f ? 1f : 0f;

        float rpmTarget01 = Mathf.Clamp01(
            speed01 * activeConfig.SpeedInfluence
            + order01 * activeConfig.OrderInfluence
            + load01 * activeConfig.LoadInfluence
        );

        if (movementController.PropulsionState == ShipPropulsionState.AcceleratingWithThrust)
        {
            rpmTarget01 = Mathf.Clamp01(rpmTarget01 + load01 * 0.1f);
        }

        float rpmResponseSpeed = rpmTarget01 >= virtualRpm01
            ? activeConfig.RpmRiseSpeed
            : activeConfig.RpmFallSpeed;

        virtualRpm01 = Mathf.MoveTowards(virtualRpm01, rpmTarget01, rpmResponseSpeed * Time.deltaTime);
    }

    private void UpdateLayerTargets(EngineSoundConfig activeConfig)
    {
        float forwardBlend01 = 1f - astern01;
        idleTargetVolume = Mathf.Clamp01(1f - speed01 / activeConfig.IdleFadeOutAtSpeed01);

        float aheadLowShape01 = 1f - Mathf.Abs(virtualRpm01 - activeConfig.AheadLowPeakRpm01) / activeConfig.AheadLowBlendWidth01;
        aheadLowTargetVolume = forwardBlend01 * Mathf.Clamp01(aheadLowShape01);

        float highBlendRange = Mathf.Max(0.01f, 1f - activeConfig.AheadHighStartRpm01);
        aheadHighTargetVolume = forwardBlend01
            * Mathf.Clamp01((virtualRpm01 - activeConfig.AheadHighStartRpm01) / highBlendRange);

        asternTargetVolume = astern01
            * Mathf.Clamp01(speed01 * activeConfig.AsternSpeedWeight + load01 * activeConfig.AsternLoadWeight);
    }

    private void ApplyLayers(EngineSoundConfig activeConfig)
    {
        ApplyLayer(
            idleAudioSource,
            activeConfig.IdleLayer,
            idleTargetVolume,
            virtualRpm01,
            activeConfig.FadeSpeed
        );
        ApplyLayer(
            aheadLowAudioSource,
            activeConfig.AheadLowLayer,
            aheadLowTargetVolume,
            virtualRpm01,
            activeConfig.FadeSpeed
        );
        ApplyLayer(
            aheadHighAudioSource,
            activeConfig.AheadHighLayer,
            aheadHighTargetVolume,
            virtualRpm01,
            activeConfig.FadeSpeed
        );
        ApplyLayer(
            asternAudioSource,
            activeConfig.AsternLayer,
            asternTargetVolume,
            Mathf.Max(speed01, virtualRpm01),
            activeConfig.FadeSpeed
        );
    }

    private void ApplyLayer(
        AudioSource audioSource,
        EngineSoundLayerSettings layerSettings,
        float targetVolume01,
        float rpm01,
        float fadeSpeed
    )
    {
        if (audioSource == null || layerSettings == null)
            return;

        float targetVolume = Mathf.Clamp01(targetVolume01) * layerSettings.VolumeMultiplier;
        float targetPitch = Mathf.Lerp(layerSettings.MinPitch, layerSettings.MaxPitch, Mathf.Clamp01(rpm01));

        audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetVolume, fadeSpeed * Time.deltaTime);
        audioSource.pitch = Mathf.MoveTowards(audioSource.pitch, targetPitch, fadeSpeed * Time.deltaTime);
    }

    private void FadeOutAllAudioSources()
    {
        EngineSoundConfig activeConfig = GetActiveAudioConfig();
        float fadeSpeed = activeConfig != null ? activeConfig.FadeSpeed : 1f;

        FadeOutAudioSource(idleAudioSource, fadeSpeed);
        FadeOutAudioSource(aheadLowAudioSource, fadeSpeed);
        FadeOutAudioSource(aheadHighAudioSource, fadeSpeed);
        FadeOutAudioSource(asternAudioSource, fadeSpeed);
    }

    private void FadeOutAudioSource(AudioSource audioSource, float fadeSpeed)
    {
        if (audioSource == null)
            return;

        audioSource.volume = Mathf.MoveTowards(audioSource.volume, 0f, fadeSpeed * Time.deltaTime);
    }

    private static void StopAudioSource(AudioSource audioSource)
    {
        if (audioSource == null)
            return;

        audioSource.Stop();
    }

    private static void ResetAudioSource(AudioSource audioSource)
    {
        if (audioSource == null)
            return;

        audioSource.volume = 0f;
        audioSource.pitch = 1f;
    }

    private ref bool GetMissingClipFlag(AudioSource audioSource)
    {
        if (audioSource == idleAudioSource)
            return ref missingIdleClipLogged;

        if (audioSource == aheadLowAudioSource)
            return ref missingAheadLowClipLogged;

        if (audioSource == aheadHighAudioSource)
            return ref missingAheadHighClipLogged;

        return ref missingAsternClipLogged;
    }

    private EngineSoundConfig GetActiveAudioConfig()
    {
        if (audioConfigOverride != null)
            return audioConfigOverride;

        if (shipStatuses == null || shipStatuses.ShipConfig == null)
            return null;

        return shipStatuses.ShipConfig.EngineSoundConfig;
    }
}
