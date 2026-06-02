using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SmokeVfxModeEntry
{
    public EngineTelegraphSector sector;
}

[CreateAssetMenu(
    fileName = "SmokeVfxConfig",
    menuName = "Ship/Smoke VFX Config",
    order = 6
)]
public class SmokeVfxConfig : ScriptableObject
{
    [Header("Source")]
    [SerializeField] private EngineTelegraphConfig sourceEngineTelegraphConfig;

    [SerializeField] private SmokeVfxModeEntry[] modeEntries = Array.Empty<SmokeVfxModeEntry>();

    [Tooltip("Включает предупреждения в консоли, если конфиг заполнен не полностью или содержит дубликаты.")]
    [SerializeField] private bool logValidationWarnings = true;

    public EngineTelegraphConfig SourceEngineTelegraphConfig => sourceEngineTelegraphConfig;
    public SmokeVfxModeEntry[] ModeEntries => modeEntries;

    public bool TryGetEntry(EngineTelegraphSector sector, out SmokeVfxModeEntry entry)
    {
        if (modeEntries != null)
        {
            for (int i = 0; i < modeEntries.Length; i++)
            {
                SmokeVfxModeEntry candidate = modeEntries[i];

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

        Dictionary<EngineTelegraphSector, SmokeVfxModeEntry> existingEntries = BuildEntryMap();
        SmokeVfxModeEntry[] syncedEntries = new SmokeVfxModeEntry[telegraphConfig.SectorCount];

        for (int i = 0; i < telegraphConfig.SectorCount; i++)
        {
            EngineTelegraphSectorData sectorData = telegraphConfig.GetSectorDataByIndex(i);

            if (sectorData == null)
                continue;

            if (!existingEntries.TryGetValue(sectorData.sector, out SmokeVfxModeEntry entry))
            {
                entry = CreateDefaultEntry(sectorData.sector);
            }

            syncedEntries[i] = entry;
        }

        sourceEngineTelegraphConfig = telegraphConfig;
        modeEntries = syncedEntries;
        return syncedEntries.Length;
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

        if (modeEntries != null)
        {
            for (int i = 0; i < modeEntries.Length; i++)
            {
                SmokeVfxModeEntry entry = modeEntries[i];

                if (entry == null)
                {
                    Debug.LogWarning($"SmokeVfxConfig '{name}': пустая запись на индексе {i}.", context);
                    continue;
                }

                if (!configuredSectors.Add(entry.sector))
                {
                    Debug.LogWarning(
                        $"SmokeVfxConfig '{name}': дублируется запись для сектора '{entry.sector}'.",
                        context
                    );
                }

                if (!expectedSectors.Contains(entry.sector))
                {
                    Debug.LogWarning(
                        $"SmokeVfxConfig '{name}': сектор '{entry.sector}' отсутствует в EngineTelegraphConfig '{telegraphConfig.name}'.",
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
                    $"SmokeVfxConfig '{name}': отсутствует запись для сектора '{sector}', который требуется в EngineTelegraphConfig '{telegraphConfig.name}'.",
                    context
                );
            }
        }
    }

    public void ValidateAgainstSource(UnityEngine.Object context)
    {
        ValidateAgainst(sourceEngineTelegraphConfig, context);
    }

    private void OnValidate()
    {
        ValidateAgainstSource(this);
    }

    private Dictionary<EngineTelegraphSector, SmokeVfxModeEntry> BuildEntryMap()
    {
        Dictionary<EngineTelegraphSector, SmokeVfxModeEntry> existingEntries = new Dictionary<EngineTelegraphSector, SmokeVfxModeEntry>();

        if (modeEntries == null)
            return existingEntries;

        for (int i = 0; i < modeEntries.Length; i++)
        {
            SmokeVfxModeEntry entry = modeEntries[i];

            if (entry == null || existingEntries.ContainsKey(entry.sector))
                continue;

            existingEntries.Add(entry.sector, entry);
        }

        return existingEntries;
    }

    private static SmokeVfxModeEntry CreateDefaultEntry(EngineTelegraphSector sector)
    {
        return new SmokeVfxModeEntry
        {
            sector = sector
        };
    }
}
