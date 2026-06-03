using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class MusicManager : MonoBehaviour
{
    private static MusicManager instance;

    [Header("Настройки")]
    [Tooltip("Необязательно. Настройки путей и резервных значений для музыкальной системы.")]
    [SerializeField] private MusicPlaybackSettingsAsset playbackSettings;
    [Tooltip("Текущее музыкальное состояние. В первой версии его можно менять вручную прямо из инспектора во время Play Mode.")]
    [SerializeField] private MusicState currentState = MusicState.Begin;
    [Tooltip("Если включено, менеджер попытается сразу запустить музыку после загрузки cfg.")]
    [SerializeField] private bool playOnStart = true;
    [Tooltip("Необязательно. Явный путь к cfg-файлу, если нужно принудительно загрузить модовую или тестовую конфигурацию.")]
    [SerializeField] private string configOverridePath;
    [Tooltip("Если включено, менеджер будет останавливать старый MenuMusic AudioSource, чтобы избежать наложения музыки.")]
    [SerializeField] private bool disableLegacyMenuMusic = true;

    [Header("Audio Sources")]
    [Tooltip("Необязательно. Первый источник для кроссфейда. Если не назначен, будет создан автоматически.")]
    [SerializeField] private AudioSource primaryAudioSource;
    [Tooltip("Необязательно. Второй источник для кроссфейда. Если не назначен, будет создан автоматически.")]
    [SerializeField] private AudioSource secondaryAudioSource;

    [Header("Отладка")]
    [Tooltip("Включает подробные сообщения в консоль о загрузке cfg, треков и переходах между состояниями.")]
    [SerializeField] private bool logDebugInfo = true;
    [SerializeField, ReadOnlyInspector] private bool configLoaded;
    [SerializeField, ReadOnlyInspector] private bool usingRuntimePlaybackSettings;
    [SerializeField, ReadOnlyInspector] private string loadedConfigPath = string.Empty;
    [SerializeField, ReadOnlyInspector] private string currentTrackId = string.Empty;
    [SerializeField, ReadOnlyInspector] private string currentTrackPath = string.Empty;
    [SerializeField, ReadOnlyInspector] private MusicState appliedState;
    [SerializeField, ReadOnlyInspector] private MusicState pendingState;

    private const string PrimaryAudioChildName = "Music Audio A";
    private const string SecondaryAudioChildName = "Music Audio B";

    private readonly Dictionary<MusicState, string> lastTrackIdsByState =
        new Dictionary<MusicState, string>();

    private MusicLibraryData library;
    private MusicRuntimeSettings runtimeSettings;
    private bool isInitialized;
    private bool hasPendingStateChange;
    private float pendingStateRequestedAt;
    private float lastStateAppliedAt = float.NegativeInfinity;
    private MusicState inspectorStateCache;
    private int activeSourceIndex;
    private Coroutine initializationCoroutine;
    private Coroutine transitionCoroutine;

    public MusicState CurrentState => currentState;
    public bool IsConfigLoaded => configLoaded;
    public string LoadedConfigPath => loadedConfigPath;

    public void SetState(MusicState newState)
    {
        currentState = newState;
        pendingState = newState;
        hasPendingStateChange = true;
        pendingStateRequestedAt = Time.unscaledTime;

        if (logDebugInfo)
        {
            Debug.Log($"MusicManager: запрошено состояние {newState}.", this);
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsurePlaybackSettings();
        EnsureAudioSources();
        inspectorStateCache = currentState;
    }

    private void OnEnable()
    {
        if (initializationCoroutine != null)
        {
            StopCoroutine(initializationCoroutine);
        }

        initializationCoroutine = StartCoroutine(InitializeMusicSystem());
    }

    private void OnDisable()
    {
        if (initializationCoroutine != null)
        {
            StopCoroutine(initializationCoroutine);
            initializationCoroutine = null;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
    }

    private void Update()
    {
        if (!isInitialized)
            return;

        if (inspectorStateCache != currentState)
        {
            inspectorStateCache = currentState;
            SetState(currentState);
        }

        if (hasPendingStateChange && CanApplyPendingState())
        {
            hasPendingStateChange = false;
            StartStateTransition(pendingState, false);
        }

        AudioSource activeSource = GetActiveAudioSource();

        if (configLoaded &&
            transitionCoroutine == null &&
            activeSource != null &&
            activeSource.clip != null &&
            !activeSource.isPlaying)
        {
            StartStateTransition(appliedState, true);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeInstance()
    {
        // if (FindFirstObjectByType<MusicManager>() != null)
        //     return;
        //
        // GameObject managerObject = new GameObject("Music Manager");
        // managerObject.AddComponent<MusicManager>();
    }

    private IEnumerator InitializeMusicSystem()
    {
        EnsurePlaybackSettings();

        if (disableLegacyMenuMusic)
        {
            StopLegacySceneMusic();
        }

        yield return null;

        configLoaded = false;
        loadedConfigPath = string.Empty;
        currentTrackId = string.Empty;
        currentTrackPath = string.Empty;
        library = null;
        runtimeSettings = BuildFallbackRuntimeSettings();

        if (TryLoadMusicConfiguration(out MusicCfgParseResult parseResult))
        {
            library = parseResult.Library;
            runtimeSettings = parseResult.RuntimeSettings ?? runtimeSettings;
            loadedConfigPath = parseResult.ConfigPath;
            configLoaded = library != null;

            for (int i = 0; i < parseResult.Warnings.Count; i++)
            {
                Debug.LogWarning($"MusicManager: {parseResult.Warnings[i]}", this);
            }

            if (logDebugInfo)
            {
                Debug.Log($"MusicManager: загружен cfg '{loadedConfigPath}'.", this);
            }
        }
        else
        {
            Debug.LogWarning("MusicManager: cfg-файл не найден. Менеджер останется в безопасном режиме без музыки.", this);
        }

        appliedState = currentState;
        pendingState = currentState;
        inspectorStateCache = currentState;
        isInitialized = true;

        if (configLoaded && playOnStart)
        {
            StartStateTransition(currentState, true);
        }

        initializationCoroutine = null;
    }

    private bool TryLoadMusicConfiguration(out MusicCfgParseResult parseResult)
    {
        foreach (string candidatePath in MusicPathResolver.EnumerateConfigCandidatePaths(
            playbackSettings,
            configOverridePath))
        {
            if (!MusicCfgParser.TryParseFile(candidatePath, playbackSettings, out parseResult))
                continue;

            return true;
        }

        parseResult = null;
        return false;
    }

    private void StartStateTransition(MusicState state, bool ignoreStateTiming)
    {
        if (!configLoaded || library == null)
            return;

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        transitionCoroutine = StartCoroutine(TransitionToState(state, ignoreStateTiming));
    }

    private IEnumerator TransitionToState(MusicState targetState, bool ignoreStateTiming)
    {
        if (!library.TryGetPlaylist(targetState, out MusicStatePlaylistData playlist) ||
            playlist == null ||
            playlist.Tracks.Count == 0)
        {
            Debug.LogWarning($"MusicManager: для состояния {targetState} не найден playlist с треками.", this);
            transitionCoroutine = null;
            yield break;
        }

        if (!TrySelectPlayableTrack(targetState, playlist, out MusicTrackEntry selectedTrack, out string resolvedTrackPath))
        {
            Debug.LogWarning($"MusicManager: для состояния {targetState} не удалось найти доступный аудиофайл.", this);
            transitionCoroutine = null;
            yield break;
        }

        AudioType audioType = GetAudioType(resolvedTrackPath);
        string requestUrl = $"file:///{resolvedTrackPath.Replace("\\", "/")}";
        UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(requestUrl, audioType);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning(
                $"MusicManager: не удалось загрузить трек '{selectedTrack.TrackId}' по пути '{resolvedTrackPath}'. Причина: {request.error}",
                this
            );
            request.Dispose();
            transitionCoroutine = null;
            yield break;
        }

        AudioClip nextClip = DownloadHandlerAudioClip.GetContent(request);
        request.Dispose();

        if (nextClip == null)
        {
            Debug.LogWarning($"MusicManager: Unity не вернула AudioClip для трека '{selectedTrack.TrackId}'.", this);
            transitionCoroutine = null;
            yield break;
        }

        AudioSource fromSource = GetActiveAudioSource();
        AudioSource toSource = GetInactiveAudioSource();
        AudioClip oldClip = toSource.clip;

        if (oldClip != null)
        {
            Destroy(oldClip);
            toSource.clip = null;
        }

        toSource.clip = nextClip;
        toSource.volume = 0f;
        toSource.loop = false;
        toSource.Play();

        float fadeDuration = runtimeSettings != null ? runtimeSettings.FadeTime : 0f;

        if (fromSource == null || fromSource.clip == null || !fromSource.isPlaying || fadeDuration <= 0.001f)
        {
            if (fromSource != null && fromSource != toSource)
            {
                StopAndReleaseSource(fromSource);
            }

            toSource.volume = 1f;
        }
        else
        {
            float elapsed = 0f;
            float fromInitialVolume = fromSource.volume;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                fromSource.volume = Mathf.Lerp(fromInitialVolume, 0f, t);
                toSource.volume = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            fromSource.volume = 0f;
            StopAndReleaseSource(fromSource);
            toSource.volume = 1f;
        }

        activeSourceIndex = activeSourceIndex == 0 ? 1 : 0;
        appliedState = targetState;
        currentTrackId = selectedTrack.TrackId;
        currentTrackPath = resolvedTrackPath;
        lastTrackIdsByState[targetState] = selectedTrack.TrackId;

        if (ignoreStateTiming || targetState != appliedState)
        {
            lastStateAppliedAt = Time.unscaledTime;
        }
        else
        {
            lastStateAppliedAt = Time.unscaledTime;
        }

        if (logDebugInfo)
        {
            Debug.Log(
                $"MusicManager: состояние {targetState}, трек '{selectedTrack.TrackId}', путь '{resolvedTrackPath}'.",
                this
            );
        }

        transitionCoroutine = null;
    }

    private bool TrySelectPlayableTrack(
        MusicState state,
        MusicStatePlaylistData playlist,
        out MusicTrackEntry selectedTrack,
        out string resolvedTrackPath)
    {
        selectedTrack = null;
        resolvedTrackPath = null;

        List<MusicTrackEntry> playableTracks = new List<MusicTrackEntry>();
        List<string> playablePaths = new List<string>();
        string lastTrackId = lastTrackIdsByState.TryGetValue(state, out string cachedTrackId)
            ? cachedTrackId
            : string.Empty;

        for (int i = 0; i < playlist.Tracks.Count; i++)
        {
            MusicTrackEntry track = playlist.Tracks[i];

            if (track == null || string.IsNullOrWhiteSpace(track.TrackId))
                continue;

            if (!MusicPathResolver.TryResolveTrackPath(
                    track.TrackId,
                    loadedConfigPath.Length > 0 ? Path.GetDirectoryName(loadedConfigPath) : string.Empty,
                    playbackSettings,
                    out string trackPath))
            {
                Debug.LogWarning($"MusicManager: трек '{track.TrackId}' не найден в модовых или встроенных каталогах.", this);
                continue;
            }

            playableTracks.Add(track);
            playablePaths.Add(trackPath);
        }

        if (playableTracks.Count == 0)
            return false;

        if (playableTracks.Count > 1 && !string.IsNullOrEmpty(lastTrackId))
        {
            for (int i = playableTracks.Count - 1; i >= 0; i--)
            {
                if (string.Equals(playableTracks[i].TrackId, lastTrackId, System.StringComparison.OrdinalIgnoreCase))
                {
                    playableTracks.RemoveAt(i);
                    playablePaths.RemoveAt(i);
                }
            }
        }

        if (playableTracks.Count == 0)
            return false;

        int selectedIndex = PickWeightedIndex(playableTracks);
        selectedTrack = playableTracks[selectedIndex];
        resolvedTrackPath = playablePaths[selectedIndex];
        return true;
    }

    private int PickWeightedIndex(List<MusicTrackEntry> tracks)
    {
        float totalWeight = 0f;

        for (int i = 0; i < tracks.Count; i++)
        {
            totalWeight += Mathf.Max(0.01f, tracks[i].Weight);
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < tracks.Count; i++)
        {
            cumulative += Mathf.Max(0.01f, tracks[i].Weight);

            if (roll <= cumulative)
                return i;
        }

        return tracks.Count - 1;
    }

    private bool CanApplyPendingState()
    {
        if (!hasPendingStateChange)
            return false;

        if (transitionCoroutine != null)
            return false;

        if (runtimeSettings == null)
            return true;

        if (Time.unscaledTime - pendingStateRequestedAt < runtimeSettings.StateConfirmationTime)
            return false;

        if (pendingState != appliedState &&
            Time.unscaledTime - lastStateAppliedAt < runtimeSettings.MinStateDuration)
        {
            return false;
        }

        return true;
    }

    private void EnsureAudioSources()
    {
        if (primaryAudioSource == null)
        {
            primaryAudioSource = CreateChildAudioSource(PrimaryAudioChildName);
        }

        if (secondaryAudioSource == null)
        {
            secondaryAudioSource = CreateChildAudioSource(SecondaryAudioChildName);
        }

        ConfigureAudioSource(primaryAudioSource);
        ConfigureAudioSource(secondaryAudioSource);
    }

    private void EnsurePlaybackSettings()
    {
        if (playbackSettings != null)
        {
            usingRuntimePlaybackSettings = false;
            return;
        }

        playbackSettings = ScriptableObject.CreateInstance<MusicPlaybackSettingsAsset>();
        playbackSettings.name = "Runtime Music Playback Settings";
        playbackSettings.hideFlags = HideFlags.DontSave;
        usingRuntimePlaybackSettings = true;

        if (logDebugInfo)
        {
            Debug.Log(
                "MusicManager: MusicPlaybackSettingsAsset не назначен. Создан runtime-экземпляр с настройками по умолчанию.",
                this
            );
        }
    }

    private AudioSource CreateChildAudioSource(string childName)
    {
        Transform child = transform.Find(childName);
        GameObject childObject = child != null ? child.gameObject : new GameObject(childName);

        if (child == null)
        {
            childObject.transform.SetParent(transform, false);
        }

        AudioSource audioSource = childObject.GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = childObject.AddComponent<AudioSource>();
        }

        return audioSource;
    }

    private static void ConfigureAudioSource(AudioSource audioSource)
    {
        if (audioSource == null)
            return;

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0f;
    }

    private void StopLegacySceneMusic()
    {
        AudioSource[] sceneAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

        for (int i = 0; i < sceneAudioSources.Length; i++)
        {
            AudioSource sceneAudioSource = sceneAudioSources[i];

            if (sceneAudioSource == null ||
                sceneAudioSource == primaryAudioSource ||
                sceneAudioSource == secondaryAudioSource)
            {
                continue;
            }

            if (sceneAudioSource.gameObject.name == "MenuMusic")
            {
                sceneAudioSource.Stop();
                sceneAudioSource.enabled = false;
            }
        }
    }

    private AudioSource GetActiveAudioSource()
    {
        return activeSourceIndex == 0 ? primaryAudioSource : secondaryAudioSource;
    }

    private AudioSource GetInactiveAudioSource()
    {
        return activeSourceIndex == 0 ? secondaryAudioSource : primaryAudioSource;
    }

    private MusicRuntimeSettings BuildFallbackRuntimeSettings()
    {
        if (playbackSettings == null)
        {
            return new MusicRuntimeSettings(1f, 20f, 2f);
        }

        return new MusicRuntimeSettings(
            playbackSettings.FallbackStateConfirmationTime,
            playbackSettings.FallbackMinStateDuration,
            playbackSettings.FallbackFadeTime
        );
    }

    private static AudioType GetAudioType(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();

        switch (extension)
        {
            case ".ogg":
                return AudioType.OGGVORBIS;
            case ".wav":
                return AudioType.WAV;
            case ".mp3":
                return AudioType.MPEG;
            case ".aiff":
            case ".aif":
                return AudioType.AIFF;
            default:
                return AudioType.UNKNOWN;
        }
    }

    private static void StopAndReleaseSource(AudioSource audioSource)
    {
        if (audioSource == null)
            return;

        AudioClip oldClip = audioSource.clip;
        audioSource.Stop();
        audioSource.clip = null;
        audioSource.volume = 0f;

        if (oldClip != null)
        {
            Destroy(oldClip);
        }
    }
}
