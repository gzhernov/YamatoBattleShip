using System.Collections;
using UnityEngine;

public class EngineTelegraphAudioController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShipStatuses shipStatuses;
    [SerializeField] private EngineTelegraphAudioConfig audioConfigOverride;

    [Header("Audio Sources")]
    [Tooltip("Optional. If not assigned, the controller creates a child 2D AudioSource automatically at runtime.")]
    [SerializeField] private AudioSource switchAudioSource;
    [Tooltip("Optional. If not assigned, the controller creates a child 2D AudioSource automatically at runtime.")]
    [SerializeField] private AudioSource confirmAudioSource;

    [Header("Debug")]
    [SerializeField] private bool logDebugInfo = false;

    private Coroutine pendingConfirmCoroutine;
    private EngineTelegraphSector latestRequestedSector;

    private void OnEnable()
    {
        ResolveReferences();
        EnsureAudioSources();

        if (shipStatuses != null)
        {
            shipStatuses.OnEngineTelegraphChanged += OnEngineTelegraphChanged;
        }
    }

    private void OnDisable()
    {
        if (shipStatuses != null)
        {
            shipStatuses.OnEngineTelegraphChanged -= OnEngineTelegraphChanged;
        }

        CancelPendingConfirm();
    }

    private void OnValidate()
    {
        if (switchAudioSource != null)
        {
            ConfigureAudioSource(switchAudioSource);
        }

        if (confirmAudioSource != null)
        {
            ConfigureAudioSource(confirmAudioSource);
        }
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

    private void EnsureAudioSources()
    {
        if (switchAudioSource == null)
        {
            switchAudioSource = CreateChildAudioSource("Telegraph Switch Audio");
        }

        if (confirmAudioSource == null)
        {
            confirmAudioSource = CreateChildAudioSource("Telegraph Confirm Audio");
        }

        ConfigureAudioSource(switchAudioSource);
        ConfigureAudioSource(confirmAudioSource);
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

    private static void ConfigureAudioSource(AudioSource audioSource)
    {
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnEngineTelegraphChanged(EngineTelegraphSector sector, EngineTelegraphSectorData sectorData)
    {
        EngineTelegraphAudioConfig activeConfig = GetActiveAudioConfig();

        if (activeConfig == null)
        {
            if (logDebugInfo)
            {
                Debug.LogWarning("EngineTelegraphAudioController: audio config is not assigned.", this);
            }

            return;
        }

        latestRequestedSector = sector;
        CancelPendingConfirm();

        if (!activeConfig.TryGetEntry(sector, out EngineTelegraphAudioSectorEntry entry))
        {
            if (logDebugInfo)
            {
                Debug.LogWarning($"EngineTelegraphAudioController: no audio entry found for sector '{sector}'.", this);
            }

            return;
        }

        PlaySwitch(entry, activeConfig);
        pendingConfirmCoroutine = StartCoroutine(PlayConfirmAfterDelay(sector, entry, activeConfig));
    }

    private EngineTelegraphAudioConfig GetActiveAudioConfig()
    {
        if (audioConfigOverride != null)
            return audioConfigOverride;

        if (shipStatuses == null || shipStatuses.ShipConfig == null)
            return null;

        return shipStatuses.ShipConfig.EngineTelegraphAudioConfig;
    }

    private void PlaySwitch(EngineTelegraphAudioSectorEntry entry, EngineTelegraphAudioConfig activeConfig)
    {
        if (switchAudioSource == null || entry.switchClip == null)
            return;

        switchAudioSource.pitch = GetPitch(entry);
        switchAudioSource.PlayOneShot(entry.switchClip, GetSwitchVolume(entry, activeConfig));
    }

    private IEnumerator PlayConfirmAfterDelay(
        EngineTelegraphSector sector,
        EngineTelegraphAudioSectorEntry entry,
        EngineTelegraphAudioConfig activeConfig
    )
    {
        float delay = entry.confirmDelay > 0f ? entry.confirmDelay : activeConfig.DefaultConfirmDelay;

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (latestRequestedSector != sector)
            yield break;

        if (confirmAudioSource != null && entry.confirmClip != null)
        {
            confirmAudioSource.pitch = GetPitch(entry);
            confirmAudioSource.PlayOneShot(entry.confirmClip, GetConfirmVolume(entry, activeConfig));
        }

        pendingConfirmCoroutine = null;
    }

    private void CancelPendingConfirm()
    {
        if (pendingConfirmCoroutine == null)
            return;

        StopCoroutine(pendingConfirmCoroutine);
        pendingConfirmCoroutine = null;
    }

    private static float GetSwitchVolume(EngineTelegraphAudioSectorEntry entry, EngineTelegraphAudioConfig activeConfig)
    {
        return entry.switchVolume > 0f ? entry.switchVolume : activeConfig.DefaultSwitchVolume;
    }

    private static float GetConfirmVolume(EngineTelegraphAudioSectorEntry entry, EngineTelegraphAudioConfig activeConfig)
    {
        return entry.confirmVolume > 0f ? entry.confirmVolume : activeConfig.DefaultConfirmVolume;
    }

    private static float GetPitch(EngineTelegraphAudioSectorEntry entry)
    {
        if (entry.pitchRandomization <= 0f)
            return 1f;

        return 1f + Random.Range(-entry.pitchRandomization, entry.pitchRandomization);
    }
}
