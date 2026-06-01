using UnityEngine;

[CreateAssetMenu(
    fileName = "MusicPlaybackSettings",
    menuName = "BattleShip/Audio/Music Playback Settings",
    order = 20
)]
public class MusicPlaybackSettingsAsset : ScriptableObject
{
    [Header("Пути")]
    [Tooltip("Имя cfg-файла музыкальной системы.")]
    [SerializeField] private string configFileName = "music.cfg";
    [Tooltip("Путь к встроенной папке музыки относительно папки Assets.")]
    [SerializeField] private string builtInMusicRelativePath = "Sound/Music";
    [Tooltip("Путь к папке музыки в StreamingAssets.")]
    [SerializeField] private string streamingAssetsMusicRelativePath = "Music";
    [Tooltip("Путь к папке модов относительно корня проекта.")]
    [SerializeField] private string projectModsMusicRelativePath = "Mods/Music";
    [Tooltip("Путь к папке модов относительно persistentDataPath.")]
    [SerializeField] private string persistentModsMusicRelativePath = "Mods/Music";

    [Header("Значения по умолчанию")]
    [Tooltip("Резервное время подтверждения нового состояния, если параметр отсутствует в cfg.")]
    [Min(0f)]
    [SerializeField] private float fallbackStateConfirmationTime = 1f;
    [Tooltip("Резервное минимальное время удержания состояния, если параметр отсутствует в cfg.")]
    [Min(0f)]
    [SerializeField] private float fallbackMinStateDuration = 20f;
    [Tooltip("Резервное время плавного перехода, если параметр отсутствует в cfg.")]
    [Min(0f)]
    [SerializeField] private float fallbackFadeTime = 2f;

    public string ConfigFileName => string.IsNullOrWhiteSpace(configFileName) ? "music.cfg" : configFileName.Trim();
    public string BuiltInMusicRelativePath => NormalizeRelativePath(builtInMusicRelativePath, "Sound/Music");
    public string StreamingAssetsMusicRelativePath => NormalizeRelativePath(streamingAssetsMusicRelativePath, "Music");
    public string ProjectModsMusicRelativePath => NormalizeRelativePath(projectModsMusicRelativePath, "Mods/Music");
    public string PersistentModsMusicRelativePath => NormalizeRelativePath(persistentModsMusicRelativePath, "Mods/Music");
    public float FallbackStateConfirmationTime => Mathf.Max(0f, fallbackStateConfirmationTime);
    public float FallbackMinStateDuration => Mathf.Max(0f, fallbackMinStateDuration);
    public float FallbackFadeTime => Mathf.Max(0f, fallbackFadeTime);

    private void OnValidate()
    {
        fallbackStateConfirmationTime = Mathf.Max(0f, fallbackStateConfirmationTime);
        fallbackMinStateDuration = Mathf.Max(0f, fallbackMinStateDuration);
        fallbackFadeTime = Mathf.Max(0f, fallbackFadeTime);
    }

    private static string NormalizeRelativePath(string value, string fallback)
    {
        string normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return normalized.Replace("\\", "/").Trim('/');
    }
}
