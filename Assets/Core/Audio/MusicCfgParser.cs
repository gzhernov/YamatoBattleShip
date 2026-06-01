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
        Dictionary<string, Dictionary<string, string>> sections = ParseSections(text);
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

    private static Dictionary<string, Dictionary<string, string>> ParseSections(string text)
    {
        Dictionary<string, Dictionary<string, string>> sections =
            new Dictionary<string, Dictionary<string, string>>(System.StringComparer.OrdinalIgnoreCase);

        Dictionary<string, string> currentSection = null;
        string[] lines = (text ?? string.Empty).Replace("\r\n", "\n").Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                string sectionName = line.Substring(1, line.Length - 2).Trim();

                if (string.IsNullOrEmpty(sectionName))
                    continue;

                currentSection = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
                sections[sectionName] = currentSection;
                continue;
            }

            if (currentSection == null)
                continue;

            int separatorIndex = line.IndexOf('=');

            if (separatorIndex <= 0)
                continue;

            string key = line.Substring(0, separatorIndex).Trim();
            string value = line.Substring(separatorIndex + 1).Trim();

            if (!string.IsNullOrEmpty(key))
            {
                currentSection[key] = value;
            }
        }

        return sections;
    }

    private static MusicRuntimeSettings BuildRuntimeSettings(
        Dictionary<string, Dictionary<string, string>> sections,
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

        if (!sections.TryGetValue("GENERAL", out Dictionary<string, string> generalSection))
        {
            warnings.Add("Секция [GENERAL] не найдена. Используются резервные значения воспроизведения.");
            return new MusicRuntimeSettings(stateConfirmationTime, minStateDuration, fadeTime);
        }

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
        Dictionary<string, Dictionary<string, string>> sections,
        List<string> warnings)
    {
        List<MusicStatePlaylistData> playlists = new List<MusicStatePlaylistData>();
        List<MusicState> declaredStates = ReadDeclaredStates(sections, warnings);

        if (declaredStates.Count == 0)
        {
            foreach (KeyValuePair<string, Dictionary<string, string>> pair in sections)
            {
                if (pair.Key.Equals("GENERAL", System.StringComparison.OrdinalIgnoreCase) ||
                    pair.Key.Equals("STATES", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (MusicStateUtility.TryParseCfgSectionName(pair.Key, out MusicState parsedState) &&
                    !declaredStates.Contains(parsedState))
                {
                    declaredStates.Add(parsedState);
                }
            }
        }

        for (int i = 0; i < declaredStates.Count; i++)
        {
            MusicState state = declaredStates[i];
            string sectionName = MusicStateUtility.GetCfgSectionName(state);

            if (!sections.TryGetValue(sectionName, out Dictionary<string, string> stateSection))
            {
                warnings.Add($"Секция состояния [{sectionName}] не найдена и будет пропущена.");
                continue;
            }

            List<MusicTrackEntry> tracks = ReadTracks(stateSection, sectionName, warnings);
            playlists.Add(new MusicStatePlaylistData(state, tracks));
        }

        return playlists;
    }

    private static List<MusicState> ReadDeclaredStates(
        Dictionary<string, Dictionary<string, string>> sections,
        List<string> warnings)
    {
        List<MusicState> states = new List<MusicState>();

        if (!sections.TryGetValue("STATES", out Dictionary<string, string> statesSection))
        {
            warnings.Add("Секция [STATES] не найдена. Состояния будут собраны по именам секций.");
            return states;
        }

        int declaredCount = GetIntValue(statesSection, "States", -1, warnings, "[STATES]/States");

        if (declaredCount > 0)
        {
            for (int i = 1; i <= declaredCount; i++)
            {
                string stateKey = $"State{i}";

                if (!statesSection.TryGetValue(stateKey, out string rawStateName))
                {
                    warnings.Add($"В [STATES] отсутствует ключ {stateKey}.");
                    continue;
                }

                if (!MusicStateUtility.TryParseCfgSectionName(rawStateName, out MusicState state))
                {
                    warnings.Add($"Неизвестное состояние '{rawStateName}' в ключе {stateKey}.");
                    continue;
                }

                if (!states.Contains(state))
                {
                    states.Add(state);
                }
            }

            return states;
        }

        List<string> fallbackKeys = new List<string>();

        foreach (string key in statesSection.Keys)
        {
            if (key.StartsWith("State", System.StringComparison.OrdinalIgnoreCase))
            {
                fallbackKeys.Add(key);
            }
        }

        fallbackKeys.Sort(System.StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < fallbackKeys.Count; i++)
        {
            string key = fallbackKeys[i];
            string rawStateName = statesSection[key];

            if (!MusicStateUtility.TryParseCfgSectionName(rawStateName, out MusicState state))
            {
                warnings.Add($"Неизвестное состояние '{rawStateName}' в ключе {key}.");
                continue;
            }

            if (!states.Contains(state))
            {
                states.Add(state);
            }
        }

        return states;
    }

    private static List<MusicTrackEntry> ReadTracks(
        Dictionary<string, string> stateSection,
        string sectionName,
        List<string> warnings)
    {
        List<MusicTrackEntry> tracks = new List<MusicTrackEntry>();
        int declaredCount = GetIntValue(stateSection, "Tracks", -1, warnings, $"[{sectionName}]/Tracks");

        if (declaredCount > 0)
        {
            for (int i = 1; i <= declaredCount; i++)
            {
                string trackKey = $"Track{i}";

                if (!stateSection.TryGetValue(trackKey, out string trackId) || string.IsNullOrWhiteSpace(trackId))
                {
                    warnings.Add($"В секции [{sectionName}] отсутствует или пуст ключ {trackKey}.");
                    continue;
                }

                tracks.Add(new MusicTrackEntry(trackId));
            }

            return tracks;
        }

        List<string> fallbackKeys = new List<string>();

        foreach (string key in stateSection.Keys)
        {
            if (key.StartsWith("Track", System.StringComparison.OrdinalIgnoreCase))
            {
                fallbackKeys.Add(key);
            }
        }

        fallbackKeys.Sort(System.StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < fallbackKeys.Count; i++)
        {
            string trackId = stateSection[fallbackKeys[i]];

            if (string.IsNullOrWhiteSpace(trackId))
                continue;

            tracks.Add(new MusicTrackEntry(trackId));
        }

        return tracks;
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
