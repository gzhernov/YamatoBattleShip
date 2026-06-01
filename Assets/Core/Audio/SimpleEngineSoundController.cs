using UnityEngine;

public class SimpleEngineSoundController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Необязательно. Если ссылка не задана, контроллер сначала ищет ShipStatuses у родителя, потом в сцене.")]
    [SerializeField] private ShipStatuses shipStatuses;

    [Tooltip("Необязательно. Если ссылка не задана, контроллер создаст или найдёт дочерний AudioSource автоматически.")]
    [SerializeField] private AudioSource engineAudioSource;

    [Tooltip("Необязательно. Локально переопределяет SimpleEngineSoundConfig из ShipConfig.")]
    [SerializeField] private SimpleEngineSoundConfig audioConfigOverride;

    [Header("Runtime (Read Only)")]
    [SerializeField, ReadOnlyInspector] private EngineTelegraphSector currentSector;
    [SerializeField, ReadOnlyInspector] private float currentVolume;
    [SerializeField, ReadOnlyInspector] private float targetVolume;
    [SerializeField, ReadOnlyInspector] private float currentPitch = 1f;
    [SerializeField, ReadOnlyInspector] private float targetPitch = 1f;

    [Header("Debug")]
    [Tooltip("Включает предупреждения в консоли, если не найдены ссылки или записи конфига.")]
    [SerializeField] private bool logDebugInfo = false;

    private const string AudioChildName = "Simple Engine Audio";

    private bool missingClipLogged;
    private bool missingSectorEntryLogged;
    private bool isSubscribed;

    private void OnEnable()
    {
        ResolveReferences();
        EnsureAudioSource();
        ApplyCurrentSectorImmediate();
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();

        if (engineAudioSource != null)
        {
            engineAudioSource.Stop();
        }
    }

    private void OnValidate()
    {
        ConfigureAudioSourceIfPresent(engineAudioSource);
    }

    private void Update()
    {
        SimpleEngineSoundConfig activeConfig = GetActiveAudioConfig();

        if (engineAudioSource == null)
            return;

        SyncClip(activeConfig);
        UpdateTargetsIfNeeded(activeConfig);
        UpdateRuntimeState(activeConfig);
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
    }

    private void EnsureAudioSource()
    {
        if (engineAudioSource == null)
        {
            Transform child = transform.Find(AudioChildName);
            GameObject audioObject = child != null ? child.gameObject : new GameObject(AudioChildName);

            if (child == null)
            {
                audioObject.transform.SetParent(transform, false);
            }

            engineAudioSource = audioObject.GetComponent<AudioSource>();

            if (engineAudioSource == null)
            {
                engineAudioSource = audioObject.AddComponent<AudioSource>();
            }
        }

        ConfigureAudioSourceIfPresent(engineAudioSource);
    }

    private void SubscribeToEvents()
    {
        if (shipStatuses == null || isSubscribed)
            return;

        shipStatuses.OnEngineTelegraphChanged += OnEngineTelegraphChanged;
        isSubscribed = true;
    }

    private void UnsubscribeFromEvents()
    {
        if (shipStatuses == null || !isSubscribed)
            return;

        shipStatuses.OnEngineTelegraphChanged -= OnEngineTelegraphChanged;
        isSubscribed = false;
    }

    private void OnEngineTelegraphChanged(EngineTelegraphSector sector, EngineTelegraphSectorData sectorData)
    {
        currentSector = sector;
        RefreshTargetsFromSector();
    }

    private void ApplyCurrentSectorImmediate()
    {
        currentSector = shipStatuses != null
            ? shipStatuses.CurrentEngineTelegraphSector
            : EngineTelegraphSector.Stop;

        RefreshTargetsFromSector();
        currentVolume = targetVolume;
        currentPitch = targetPitch;

        if (engineAudioSource != null)
        {
            engineAudioSource.volume = currentVolume;
            engineAudioSource.pitch = currentPitch;
        }
    }

    private void RefreshTargetsFromSector()
    {
        SimpleEngineSoundConfig activeConfig = GetActiveAudioConfig();

        if (activeConfig == null)
        {
            targetVolume = 0f;
            targetPitch = 1f;
            missingSectorEntryLogged = false;
            return;
        }

        if (!activeConfig.TryGetEntry(currentSector, out SimpleEngineSoundSectorEntry entry))
        {
            targetVolume = 0f;
            targetPitch = 1f;

            if (logDebugInfo && !missingSectorEntryLogged)
            {
                Debug.LogWarning($"SimpleEngineSoundController: отсутствует запись для сектора '{currentSector}'.", this);
                missingSectorEntryLogged = true;
            }

            return;
        }

        missingSectorEntryLogged = false;
        targetVolume = entry.targetVolume;
        targetPitch = entry.targetPitch;
    }

    private void SyncClip(SimpleEngineSoundConfig activeConfig)
    {
        AudioClip targetClip = activeConfig != null ? activeConfig.AudioClip : null;

        if (engineAudioSource.clip != targetClip)
        {
            engineAudioSource.clip = targetClip;
            missingClipLogged = false;
        }

        if (targetClip == null)
        {
            if (engineAudioSource.isPlaying)
            {
                engineAudioSource.Stop();
            }

            if (logDebugInfo && activeConfig != null && !missingClipLogged)
            {
                Debug.LogWarning("SimpleEngineSoundController: не назначен AudioClip в SimpleEngineSoundConfig.", this);
                missingClipLogged = true;
            }

            return;
        }

        missingClipLogged = false;

        if (!engineAudioSource.isPlaying)
        {
            engineAudioSource.Play();
        }
    }

    private void UpdateTargetsIfNeeded(SimpleEngineSoundConfig activeConfig)
    {
        if (activeConfig == null)
        {
            targetVolume = 0f;
            targetPitch = 1f;
            return;
        }

        RefreshTargetsFromSector();
    }

    private void UpdateRuntimeState(SimpleEngineSoundConfig activeConfig)
    {
        float transitionTime = activeConfig != null ? activeConfig.TransitionTimeSeconds : 1f;
        float volumeStep = GetStepTowardsTarget(currentVolume, targetVolume, transitionTime);
        float pitchStep = GetStepTowardsTarget(currentPitch, targetPitch, transitionTime);

        currentVolume = Mathf.MoveTowards(currentVolume, targetVolume, volumeStep);
        currentPitch = Mathf.MoveTowards(currentPitch, targetPitch, pitchStep);

        engineAudioSource.volume = currentVolume;
        engineAudioSource.pitch = currentPitch;
    }

    private static float GetStepTowardsTarget(float currentValue, float targetValue, float transitionTime)
    {
        if (transitionTime <= 0.01f)
            return Mathf.Abs(targetValue - currentValue);

        return Mathf.Abs(targetValue - currentValue) * Time.deltaTime / transitionTime;
    }

    private SimpleEngineSoundConfig GetActiveAudioConfig()
    {
        if (audioConfigOverride != null)
            return audioConfigOverride;

        if (shipStatuses == null || shipStatuses.ShipConfig == null)
            return null;

        return shipStatuses.ShipConfig.SimpleEngineSoundConfig;
    }

    private static void ConfigureAudioSourceIfPresent(AudioSource audioSource)
    {
        if (audioSource == null)
            return;

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
    }
}
