using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SimpleEngineSoundSectorEntry
{
    public EngineTelegraphSector sector;

    [Tooltip("Целевая громкость дорожки двигателя для этого сектора телеграфа.")]
    [Range(0f, 1f)]
    public float targetVolume = 1f;

    [Tooltip("Целевой pitch дорожки двигателя для этого сектора телеграфа.")]
    [Min(0f)]
    public float targetPitch = 1f;

    public void Validate()
    {
        targetVolume = Mathf.Clamp01(targetVolume);
        targetPitch = Mathf.Max(0f, targetPitch);
    }
}

[CreateAssetMenu(
    fileName = "SimpleEngineSoundConfig",
    menuName = "Ship/Simple Engine Sound Config",
    order = 5
)]
public class SimpleEngineSoundConfig : ScriptableObject
{
    [SerializeField] private EngineTelegraphConfig sourceEngineTelegraphConfig;

    [Tooltip("Одна зацикленная дорожка двигателя, которая будет играть во всех режимах телеграфа.")]
    [SerializeField] private AudioClip audioClip;

    [Tooltip("Сколько секунд занимает плавный переход громкости и pitch к значениям нового режима.")]
    [Min(0.01f)]
    [SerializeField] private float transitionTimeSeconds = 1f;

    [SerializeField] private SimpleEngineSoundSectorEntry[] sectorEntries = Array.Empty<SimpleEngineSoundSectorEntry>();

    [Tooltip("Включает предупреждения в консоли, если конфиг заполнен не полностью.")]
    [SerializeField] private bool logValidationWarnings = true;

    public EngineTelegraphConfig SourceEngineTelegraphConfig => sourceEngineTelegraphConfig;
    public AudioClip AudioClip => audioClip;
    public float TransitionTimeSeconds => transitionTimeSeconds;
    public SimpleEngineSoundSectorEntry[] SectorEntries => sectorEntries;

    public bool TryGetEntry(EngineTelegraphSector sector, out SimpleEngineSoundSectorEntry entry)
    {
        if (sectorEntries != null)
        {
            for (int i = 0; i < sectorEntries.Length; i++)
            {
                SimpleEngineSoundSectorEntry candidate = sectorEntries[i];

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

        Dictionary<EngineTelegraphSector, SimpleEngineSoundSectorEntry> existingEntries = BuildEntryMap();
        SimpleEngineSoundSectorEntry[] syncedEntries = new SimpleEngineSoundSectorEntry[telegraphConfig.SectorCount];

        for (int i = 0; i < telegraphConfig.SectorCount; i++)
        {
            EngineTelegraphSectorData sectorData = telegraphConfig.GetSectorDataByIndex(i);

            if (sectorData == null)
                continue;

            if (!existingEntries.TryGetValue(sectorData.sector, out SimpleEngineSoundSectorEntry entry))
            {
                entry = CreateDefaultEntry(sectorData.sector);
            }

            entry.Validate();
            syncedEntries[i] = entry;
        }

        sourceEngineTelegraphConfig = telegraphConfig;
        sectorEntries = syncedEntries;
        return syncedEntries.Length;
    }

    public void ValidateAgainst(EngineTelegraphConfig telegraphConfig, UnityEngine.Object context)
    {
        transitionTimeSeconds = Mathf.Max(0.01f, transitionTimeSeconds);

        if (sectorEntries != null)
        {
            for (int i = 0; i < sectorEntries.Length; i++)
            {
                sectorEntries[i]?.Validate();
            }
        }

        if (!logValidationWarnings || telegraphConfig == null || !telegraphConfig.HasSectors)
            return;

        if (audioClip == null)
        {
            Debug.LogWarning($"SimpleEngineSoundConfig '{name}': не назначен AudioClip.", context);
        }

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

        if (sectorEntries != null)
        {
            for (int i = 0; i < sectorEntries.Length; i++)
            {
                SimpleEngineSoundSectorEntry entry = sectorEntries[i];

                if (entry == null)
                {
                    Debug.LogWarning($"SimpleEngineSoundConfig '{name}': пустая запись на индексе {i}.", context);
                    continue;
                }

                entry.Validate();

                if (!configuredSectors.Add(entry.sector))
                {
                    Debug.LogWarning(
                        $"SimpleEngineSoundConfig '{name}': дублируется запись для сектора '{entry.sector}'.",
                        context
                    );
                }

                if (!expectedSectors.Contains(entry.sector))
                {
                    Debug.LogWarning(
                        $"SimpleEngineSoundConfig '{name}': сектор '{entry.sector}' отсутствует в EngineTelegraphConfig '{telegraphConfig.name}'.",
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
                    $"SimpleEngineSoundConfig '{name}': отсутствует запись для сектора '{sector}', который требуется в EngineTelegraphConfig '{telegraphConfig.name}'.",
                    context
                );
            }
        }
    }

    private void OnValidate()
    {
        transitionTimeSeconds = Mathf.Max(0.01f, transitionTimeSeconds);
        ValidateAgainst(sourceEngineTelegraphConfig, this);
    }

    private Dictionary<EngineTelegraphSector, SimpleEngineSoundSectorEntry> BuildEntryMap()
    {
        Dictionary<EngineTelegraphSector, SimpleEngineSoundSectorEntry> existingEntries = new Dictionary<EngineTelegraphSector, SimpleEngineSoundSectorEntry>();

        if (sectorEntries == null)
            return existingEntries;

        for (int i = 0; i < sectorEntries.Length; i++)
        {
            SimpleEngineSoundSectorEntry entry = sectorEntries[i];

            if (entry == null || existingEntries.ContainsKey(entry.sector))
                continue;

            existingEntries.Add(entry.sector, entry);
        }

        return existingEntries;
    }

    private static SimpleEngineSoundSectorEntry CreateDefaultEntry(EngineTelegraphSector sector)
    {
        return new SimpleEngineSoundSectorEntry
        {
            sector = sector,
            targetVolume = sector == EngineTelegraphSector.Stop ? 0f : 1f,
            targetPitch = 1f
        };
    }
}
