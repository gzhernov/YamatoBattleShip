using System.Collections.Generic;
using System.Globalization;
using System.IO;

public static class MusicCfgParser
{
    public static bool TryParseFile(
        string configPath,
        MusicPlaybackSettingsAsset playbackSettings,
        out MusicCfgParseResult result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
            return false;

        string text = File.ReadAllText(configPath);
        result = ParseText(text, configPath, playbackSettings);
        return true;
    }

    public static MusicCfgParseResult ParseText(
        string text,
        string configPath,
        MusicPlaybackSettingsAsset playbackSettings)
    {
        Dictionary<string, List<string>> sections = ParseSections(text);
        List<string> warnings = new List<string>();

        MusicRuntimeSettings runtimeSettings = BuildRuntimeSettings(
            sections,
            playbackSettings,
            warnings
        );

        List<MusicStatePlaylistData> playlists = BuildPlaylists(sections, warnings);
        MusicLibraryData library = new MusicLibraryData(playlists);

        return new MusicCfgParseResult(
            library,
            runtimeSettings,
            configPath,
            string.IsNullOrWhiteSpace(configPath) ? string.Empty : Path.GetDirectoryName(configPath),
            warnings
        );
    }

    private static Dictionary<string, List<string>> ParseSections(string text)
    {
        Dictionary<string, List<string>> sections =
            new Dictionary<string, List<string>>(System.StringComparer.OrdinalIgnoreCase);

        List<string> currentSection = null;
        string[] lines = (text ?? string.Empty).Replace("\r\n", "\n").Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                string sectionName = line.Substring(1, line.Length - 2).Trim();

                if (string.IsNullOrEmpty(sectionName))
                    continue;

                currentSection = new List<string>();
                sections[sectionName] = currentSection;
                continue;
            }

            if (currentSection == null)
                continue;

            currentSection.Add(line);
        }

        return sections;
    }

    private static MusicRuntimeSettings BuildRuntimeSettings(
        Dictionary<string, List<string>> sections,
        MusicPlaybackSettingsAsset playbackSettings,
        List<string> warnings)
    {
        float stateConfirmationTime = playbackSettings != null
            ? playbackSettings.FallbackStateConfirmationTime
            : 1f;
        float minStateDuration = playbackSettings != null
            ? playbackSettings.FallbackMinStateDuration
            : 20f;
        float fadeTime = playbackSettings != null
            ? playbackSettings.FallbackFadeTime
            : 2f;

        if (!sections.TryGetValue("GENERAL", out List<string> generalSectionLines))
        {
            warnings.Add("Секция [GENERAL] не найдена. Используются резервные значения.");
            return new MusicRuntimeSettings(stateConfirmationTime, minStateDuration, fadeTime);
        }

        Dictionary<string, string> generalSection = ReadKeyValueSection(
            generalSectionLines,
            "[GENERAL]",
            warnings);

        stateConfirmationTime = GetFloatValue(
            generalSection,
            "CertainityTime",
            stateConfirmationTime,
            warnings,
            "[GENERAL]/CertainityTime"
        );
        minStateDuration = GetFloatValue(
            generalSection,
            "MinDuration",
            minStateDuration,
            warnings,
            "[GENERAL]/MinDuration"
        );
        fadeTime = GetFloatValue(
            generalSection,
            "FadeTime",
            fadeTime,
            warnings,
            "[GENERAL]/FadeTime"
        );

        return new MusicRuntimeSettings(stateConfirmationTime, minStateDuration, fadeTime);
    }

    private static List<MusicStatePlaylistData> BuildPlaylists(
        Dictionary<string, List<string>> sections,
        List<string> warnings)
    {
        List<MusicStatePlaylistData> playlists = new List<MusicStatePlaylistData>();

        foreach (KeyValuePair<string, List<string>> pair in sections)
        {
            if (pair.Key.Equals("GENERAL", System.StringComparison.OrdinalIgnoreCase))
                continue;

            if (!MusicStateUtility.TryParseCfgSectionName(pair.Key, out MusicState state))
                continue;

            List<MusicTrackEntry> tracks = ReadTracks(pair.Value, pair.Key, warnings);
            playlists.Add(new MusicStatePlaylistData(state, tracks));
        }

        return playlists;
    }

    private static List<MusicTrackEntry> ReadTracks(
        List<string> stateSectionLines,
        string sectionName,
        List<string> warnings)
    {
        List<MusicTrackEntry> tracks = new List<MusicTrackEntry>();

        for (int i = 0; i < stateSectionLines.Count; i++)
        {
            string line = stateSectionLines[i];

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.IndexOf('=') >= 0)
            {
                warnings.Add($"В секции [{sectionName}] поддерживаются только строки с треками. Строка '{line}' будет проигнорирована.");
                continue;
            }

            tracks.Add(new MusicTrackEntry(line));
        }

        return tracks;
    }

    private static Dictionary<string, string> ReadKeyValueSection(
        List<string> sectionLines,
        string sectionName,
        List<string> warnings)
    {
        Dictionary<string, string> values =
            new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < sectionLines.Count; i++)
        {
            string line = sectionLines[i];

            if (string.IsNullOrWhiteSpace(line))
                continue;

            int separatorIndex = line.IndexOf('=');

            if (separatorIndex <= 0)
            {
                warnings.Add($"В секции {sectionName} ожидаются пары ключ=значение. Строка '{line}' будет проигнорирована.");
                continue;
            }

            string key = line.Substring(0, separatorIndex).Trim();
            string value = line.Substring(separatorIndex + 1).Trim();

            if (string.IsNullOrEmpty(key))
            {
                warnings.Add($"В секции {sectionName} найден пустой ключ в строке '{line}'.");
                continue;
            }

            values[key] = value;
        }

        return values;
    }

    private static int GetIntValue(
        Dictionary<string, string> section,
        string key,
        int fallbackValue,
        List<string> warnings,
        string location)
    {
        if (!section.TryGetValue(key, out string rawValue))
            return fallbackValue;

        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedValue))
            return parsedValue;

        warnings.Add($"Не удалось разобрать целое значение в {location}: '{rawValue}'.");
        return fallbackValue;
    }

    private static float GetFloatValue(
        Dictionary<string, string> section,
        string key,
        float fallbackValue,
        List<string> warnings,
        string location)
    {
        if (!section.TryGetValue(key, out string rawValue))
            return fallbackValue;

        if (float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue))
            return parsedValue;

        warnings.Add($"Не удалось разобрать число в {location}: '{rawValue}'.");
        return fallbackValue;
    }
}
