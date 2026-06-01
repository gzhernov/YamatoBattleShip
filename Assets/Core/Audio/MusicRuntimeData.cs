using System.Collections.Generic;

public sealed class MusicTrackEntry
{
    public MusicTrackEntry(string trackId, float weight = 1f)
    {
        TrackId = string.IsNullOrWhiteSpace(trackId) ? string.Empty : trackId.Trim();
        Weight = weight <= 0f ? 1f : weight;
    }

    public string TrackId { get; }
    public float Weight { get; }
}

public sealed class MusicStatePlaylistData
{
    public MusicStatePlaylistData(MusicState state, IReadOnlyList<MusicTrackEntry> tracks)
    {
        State = state;
        Tracks = tracks ?? System.Array.Empty<MusicTrackEntry>();
    }

    public MusicState State { get; }
    public IReadOnlyList<MusicTrackEntry> Tracks { get; }
}

public sealed class MusicLibraryData
{
    private readonly Dictionary<MusicState, MusicStatePlaylistData> playlistsByState;

    public MusicLibraryData(IReadOnlyList<MusicStatePlaylistData> playlists)
    {
        Playlists = playlists ?? System.Array.Empty<MusicStatePlaylistData>();
        playlistsByState = new Dictionary<MusicState, MusicStatePlaylistData>();

        for (int i = 0; i < Playlists.Count; i++)
        {
            MusicStatePlaylistData playlist = Playlists[i];

            if (playlist == null)
                continue;

            playlistsByState[playlist.State] = playlist;
        }
    }

    public IReadOnlyList<MusicStatePlaylistData> Playlists { get; }

    public bool TryGetPlaylist(MusicState state, out MusicStatePlaylistData playlist)
    {
        return playlistsByState.TryGetValue(state, out playlist);
    }
}

public sealed class MusicRuntimeSettings
{
    public MusicRuntimeSettings(float stateConfirmationTime, float minStateDuration, float fadeTime)
    {
        StateConfirmationTime = stateConfirmationTime < 0f ? 0f : stateConfirmationTime;
        MinStateDuration = minStateDuration < 0f ? 0f : minStateDuration;
        FadeTime = fadeTime < 0f ? 0f : fadeTime;
    }

    public float StateConfirmationTime { get; }
    public float MinStateDuration { get; }
    public float FadeTime { get; }
}

public sealed class MusicCfgParseResult
{
    public MusicCfgParseResult(
        MusicLibraryData library,
        MusicRuntimeSettings runtimeSettings,
        string configPath,
        string configDirectory,
        IReadOnlyList<string> warnings)
    {
        Library = library;
        RuntimeSettings = runtimeSettings;
        ConfigPath = configPath ?? string.Empty;
        ConfigDirectory = configDirectory ?? string.Empty;
        Warnings = warnings ?? System.Array.Empty<string>();
    }

    public MusicLibraryData Library { get; }
    public MusicRuntimeSettings RuntimeSettings { get; }
    public string ConfigPath { get; }
    public string ConfigDirectory { get; }
    public IReadOnlyList<string> Warnings { get; }
}
