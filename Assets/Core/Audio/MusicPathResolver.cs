using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class MusicPathResolver
{
    public static IEnumerable<string> EnumerateConfigCandidatePaths(
        MusicPlaybackSettingsAsset playbackSettings,
        string overrideConfigPath)
    {
        HashSet<string> seenPaths = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(overrideConfigPath))
        {
            string absoluteOverridePath = NormalizeFullPath(overrideConfigPath);

            if (seenPaths.Add(absoluteOverridePath))
                yield return absoluteOverridePath;
        }

        foreach (string root in EnumerateMusicRoots(playbackSettings))
        {
            string candidatePath = NormalizeFullPath(
                Path.Combine(root, GetConfigFileName(playbackSettings))
            );

            if (seenPaths.Add(candidatePath))
                yield return candidatePath;
        }
    }

    public static bool TryResolveTrackPath(
        string trackId,
        string configDirectory,
        MusicPlaybackSettingsAsset playbackSettings,
        out string resolvedPath)
    {
        resolvedPath = null;

        if (string.IsNullOrWhiteSpace(trackId))
            return false;

        foreach (string candidatePath in EnumerateTrackCandidatePaths(trackId, configDirectory, playbackSettings))
        {
            if (File.Exists(candidatePath))
            {
                resolvedPath = candidatePath;
                return true;
            }
        }

        return false;
    }

    public static IEnumerable<string> EnumerateTrackCandidatePaths(
        string trackId,
        string configDirectory,
        MusicPlaybackSettingsAsset playbackSettings)
    {
        HashSet<string> seenPaths = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        string trimmedTrackId = trackId.Trim();

        if (Path.IsPathRooted(trimmedTrackId))
        {
            string rootedPath = NormalizeFullPath(trimmedTrackId);

            if (seenPaths.Add(rootedPath))
                yield return rootedPath;
        }

        if (!string.IsNullOrWhiteSpace(configDirectory))
        {
            string relativeToConfig = NormalizeFullPath(Path.Combine(configDirectory, trimmedTrackId));

            if (seenPaths.Add(relativeToConfig))
                yield return relativeToConfig;
        }

        foreach (string root in EnumerateMusicRoots(playbackSettings))
        {
            string candidatePath = NormalizeFullPath(Path.Combine(root, trimmedTrackId));

            if (seenPaths.Add(candidatePath))
                yield return candidatePath;
        }
    }

    public static IEnumerable<string> EnumerateMusicRoots(MusicPlaybackSettingsAsset playbackSettings)
    {
        HashSet<string> seenPaths = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        string projectRoot = NormalizeFullPath(Path.Combine(Application.dataPath, ".."));
        string builtInRoot = NormalizeFullPath(
            Path.Combine(Application.dataPath, GetBuiltInMusicRelativePath(playbackSettings))
        );
        string streamingAssetsRoot = NormalizeFullPath(
            Path.Combine(Application.streamingAssetsPath, GetStreamingAssetsMusicRelativePath(playbackSettings))
        );
        string projectModsRoot = NormalizeFullPath(
            Path.Combine(projectRoot, GetProjectModsMusicRelativePath(playbackSettings))
        );
        string persistentModsRoot = NormalizeFullPath(
            Path.Combine(Application.persistentDataPath, GetPersistentModsMusicRelativePath(playbackSettings))
        );

        string[] roots =
        {
            projectModsRoot,
            persistentModsRoot,
            streamingAssetsRoot,
            builtInRoot
        };

        for (int i = 0; i < roots.Length; i++)
        {
            string root = roots[i];

            if (seenPaths.Add(root))
                yield return root;
        }
    }

    private static string GetConfigFileName(MusicPlaybackSettingsAsset playbackSettings)
    {
        return playbackSettings != null ? playbackSettings.ConfigFileName : "music.cfg";
    }

    private static string GetBuiltInMusicRelativePath(MusicPlaybackSettingsAsset playbackSettings)
    {
        return playbackSettings != null ? playbackSettings.BuiltInMusicRelativePath : "Sound/Music";
    }

    private static string GetStreamingAssetsMusicRelativePath(MusicPlaybackSettingsAsset playbackSettings)
    {
        return playbackSettings != null ? playbackSettings.StreamingAssetsMusicRelativePath : "Music";
    }

    private static string GetProjectModsMusicRelativePath(MusicPlaybackSettingsAsset playbackSettings)
    {
        return playbackSettings != null ? playbackSettings.ProjectModsMusicRelativePath : "Mods/Music";
    }

    private static string GetPersistentModsMusicRelativePath(MusicPlaybackSettingsAsset playbackSettings)
    {
        return playbackSettings != null ? playbackSettings.PersistentModsMusicRelativePath : "Mods/Music";
    }

    private static string NormalizeFullPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return Path.GetFullPath(path);
    }
}
