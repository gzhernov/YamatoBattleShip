using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EngineTelegraphAudioSectorEntry
{
    public EngineTelegraphSector sector;
    public AudioClip switchClip;
    public AudioClip confirmClip;

    [Min(0f)]
    public float confirmDelay = 0.3f;

    [Range(0f, 1f)]
    public float switchVolume = 1f;

    [Range(0f, 1f)]
    public float confirmVolume = 1f;

    [Min(0f)]
    public float pitchRandomization = 0f;
}

[CreateAssetMenu(
    fileName = "EngineTelegraphAudioConfig",
    menuName = "Ship/Engine Telegraph Audio Config",
    order = 2
)]
public class EngineTelegraphAudioConfig : ScriptableObject
{
    [Header("Source")]
    [SerializeField] private EngineTelegraphConfig sourceEngineTelegraphConfig;

    [SerializeField] private EngineTelegraphAudioSectorEntry[] sectorAudioEntries = Array.Empty<EngineTelegraphAudioSectorEntry>();

    [Header("Fallbacks")]
    [Min(0f)]
    [SerializeField] private float defaultConfirmDelay = 0.3f;

    [Range(0f, 1f)]
    [SerializeField] private float defaultSwitchVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float defaultConfirmVolume = 1f;

    [SerializeField] private bool logValidationWarnings = true;

    public EngineTelegraphConfig SourceEngineTelegraphConfig => sourceEngineTelegraphConfig;
    public float DefaultConfirmDelay => defaultConfirmDelay;
    public float DefaultSwitchVolume => defaultSwitchVolume;
    public float DefaultConfirmVolume => defaultConfirmVolume;

    public bool TryGetEntry(EngineTelegraphSector sector, out EngineTelegraphAudioSectorEntry entry)
    {
        if (sectorAudioEntries != null)
        {
            for (int i = 0; i < sectorAudioEntries.Length; i++)
            {
                EngineTelegraphAudioSectorEntry candidate = sectorAudioEntries[i];

                if (candidate != null && candidate.sector == sector)
                {
                    entry = candidate;
                    return true;
                }
            }
        }

        entry = null;
        return false;
    }

    public int SyncFromSource()
    {
        return SyncFromTelegraphConfig(sourceEngineTelegraphConfig);
    }

    public int SyncFromTelegraphConfig(EngineTelegraphConfig telegraphConfig)
    {
        if (telegraphConfig == null || !telegraphConfig.HasSectors)
            return 0;

        Dictionary<EngineTelegraphSector, EngineTelegraphAudioSectorEntry> existingEntries = BuildEntryMap();
        EngineTelegraphAudioSectorEntry[] syncedEntries = new EngineTelegraphAudioSectorEntry[telegraphConfig.SectorCount];
        int syncedCount = 0;

        for (int i = 0; i < telegraphConfig.SectorCount; i++)
        {
            EngineTelegraphSectorData sectorData = telegraphConfig.GetSectorDataByIndex(i);

            if (sectorData == null)
                continue;

            if (!existingEntries.TryGetValue(sectorData.sector, out EngineTelegraphAudioSectorEntry entry))
            {
                entry = CreateDefaultEntry(sectorData.sector);
            }

            syncedEntries[syncedCount] = entry;
            syncedCount++;
        }

        if (syncedCount != syncedEntries.Length)
        {
            Array.Resize(ref syncedEntries, syncedCount);
        }

        sectorAudioEntries = syncedEntries;
        return syncedCount;
    }

    public void ValidateAgainst(EngineTelegraphConfig telegraphConfig, UnityEngine.Object context)
    {
        if (!logValidationWarnings || telegraphConfig == null || !telegraphConfig.HasSectors)
            return;

        HashSet<EngineTelegraphSector> expectedSectors = new HashSet<EngineTelegraphSector>();

        for (int i = 0; i < telegraphConfig.SectorCount; i++)
        {
            EngineTelegraphSectorData sectorData = telegraphConfig.GetSectorDataByIndex(i);

            if (sectorData != null)
            {
                expectedSectors.Add(sectorData.sector);
            }
        }

        HashSet<EngineTelegraphSector> configuredSectors = new HashSet<EngineTelegraphSector>();

        if (sectorAudioEntries != null)
        {
            for (int i = 0; i < sectorAudioEntries.Length; i++)
            {
                EngineTelegraphAudioSectorEntry entry = sectorAudioEntries[i];

                if (entry == null)
                {
                    Debug.LogWarning($"EngineTelegraphAudioConfig '{name}': empty entry at index {i}.", context);
                    continue;
                }

                if (!configuredSectors.Add(entry.sector))
                {
                    Debug.LogWarning(
                        $"EngineTelegraphAudioConfig '{name}': duplicate audio entry for sector '{entry.sector}'.",
                        context
                    );
                }

                if (!expectedSectors.Contains(entry.sector))
                {
                    Debug.LogWarning(
                        $"EngineTelegraphAudioConfig '{name}': sector '{entry.sector}' is not present in EngineTelegraphConfig '{telegraphConfig.name}'.",
                        context
                    );
                }
            }
        }

        foreach (EngineTelegraphSector sector in expectedSectors)
        {
            if (!configuredSectors.Contains(sector))
            {
                Debug.LogWarning(
                    $"EngineTelegraphAudioConfig '{name}': missing audio entry for sector '{sector}' required by EngineTelegraphConfig '{telegraphConfig.name}'.",
                    context
                );
            }
        }
    }

    public void ValidateAgainstSource(UnityEngine.Object context)
    {
        ValidateAgainst(sourceEngineTelegraphConfig, context);
    }

    private Dictionary<EngineTelegraphSector, EngineTelegraphAudioSectorEntry> BuildEntryMap()
    {
        Dictionary<EngineTelegraphSector, EngineTelegraphAudioSectorEntry> existingEntries = new Dictionary<EngineTelegraphSector, EngineTelegraphAudioSectorEntry>();

        if (sectorAudioEntries == null)
            return existingEntries;

        for (int i = 0; i < sectorAudioEntries.Length; i++)
        {
            EngineTelegraphAudioSectorEntry entry = sectorAudioEntries[i];

            if (entry == null || existingEntries.ContainsKey(entry.sector))
                continue;

            existingEntries.Add(entry.sector, entry);
        }

        return existingEntries;
    }

    private EngineTelegraphAudioSectorEntry CreateDefaultEntry(EngineTelegraphSector sector)
    {
        return new EngineTelegraphAudioSectorEntry
        {
            sector = sector,
            confirmDelay = defaultConfirmDelay,
            switchVolume = defaultSwitchVolume,
            confirmVolume = defaultConfirmVolume,
            pitchRandomization = 0f
        };
    }

    private void OnValidate()
    {
        defaultConfirmDelay = Mathf.Max(0f, defaultConfirmDelay);
        defaultSwitchVolume = Mathf.Clamp01(defaultSwitchVolume);
        defaultConfirmVolume = Mathf.Clamp01(defaultConfirmVolume);

        if (sectorAudioEntries == null)
            return;

        for (int i = 0; i < sectorAudioEntries.Length; i++)
        {
            EngineTelegraphAudioSectorEntry entry = sectorAudioEntries[i];

            if (entry == null)
                continue;

            entry.confirmDelay = Mathf.Max(0f, entry.confirmDelay);
            entry.switchVolume = Mathf.Clamp01(entry.switchVolume);
            entry.confirmVolume = Mathf.Clamp01(entry.confirmVolume);
            entry.pitchRandomization = Mathf.Max(0f, entry.pitchRandomization);
        }

        ValidateAgainstSource(this);
    }
}
