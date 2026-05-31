using UnityEngine;

public class RudderAudioController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional. If not assigned, the controller will search for ShipStatuses on the parent object first, then in the scene.")]
    [SerializeField] private ShipStatuses shipStatuses;
    [Tooltip("Optional. Overrides the RudderAudioConfig taken from ShipConfig for this controller only.")]
    [SerializeField] private RudderAudioConfig audioConfigOverride;

    [Header("Audio Sources")]
    [Tooltip("Optional. If not assigned, the controller creates a child 2D AudioSource automatically at runtime.")]
    [SerializeField] private AudioSource switchAudioSource;

    [Header("Debug")]
    [Tooltip("Enables warning logs when the audio config or clip is missing.")]
    [SerializeField] private bool logDebugInfo = false;

    private void OnEnable()
    {
        ResolveReferences();
        EnsureAudioSources();

        if (shipStatuses != null)
        {
            shipStatuses.OnRudderPositionChanged += OnRudderPositionChanged;
        }
    }

    private void OnDisable()
    {
        if (shipStatuses != null)
        {
            shipStatuses.OnRudderPositionChanged -= OnRudderPositionChanged;
        }
    }

    private void OnValidate()
    {
        if (switchAudioSource != null)
        {
            ConfigureAudioSource(switchAudioSource);
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
            switchAudioSource = CreateChildAudioSource("Rudder Switch Audio");
        }

        ConfigureAudioSource(switchAudioSource);
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

    private void OnRudderPositionChanged(int _)
    {
        RudderAudioConfig activeConfig = GetActiveAudioConfig();

        if (activeConfig == null)
        {
            if (logDebugInfo)
            {
                Debug.LogWarning("RudderAudioController: audio config is not assigned.", this);
            }

            return;
        }

        if (switchAudioSource == null || activeConfig.SwitchClip == null)
        {
            if (logDebugInfo && activeConfig.SwitchClip == null)
            {
                Debug.LogWarning("RudderAudioController: switch clip is not assigned.", this);
            }

            return;
        }

        switchAudioSource.pitch = GetPitch(activeConfig);
        switchAudioSource.PlayOneShot(activeConfig.SwitchClip, activeConfig.SwitchVolume);
    }

    private RudderAudioConfig GetActiveAudioConfig()
    {
        if (audioConfigOverride != null)
            return audioConfigOverride;

        if (shipStatuses == null || shipStatuses.ShipConfig == null)
            return null;

        return shipStatuses.ShipConfig.RudderAudioConfig;
    }

    private static float GetPitch(RudderAudioConfig activeConfig)
    {
        if (activeConfig.PitchRandomization <= 0f)
            return 1f;

        return 1f + Random.Range(-activeConfig.PitchRandomization, activeConfig.PitchRandomization);
    }
}
