using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum EngineTelegraphSector
{
    Emer,
    FullReverse,
    BackTwoThirds,
    BackOneThird,
    Stop,
    AheadOneThird,
    AheadTwoThirds,
    StandBy,
    FullAhead,
    FlankAhead
}

[Serializable]
public class EngineTelegraphSectorData
{
    public EngineTelegraphSector sector;
    public string displayName;
    public float angle;

    [Tooltip("Скорость корабля на этом режиме в узлах. Отрицательное значение — ход назад.")]
    [FormerlySerializedAs("speedValue")]
    public float speedKnots;

    [Obsolete("Use speedKnots instead. This property is kept only for compatibility with old code.")]
    public float speedValue
    {
        get => speedKnots;
        set => speedKnots = value;
    }

    public EngineTelegraphSectorData(
        EngineTelegraphSector sector,
        string displayName,
        float angle,
        float speedKnots
    )
    {
        this.sector = sector;
        this.displayName = displayName;
        this.angle = angle;
        this.speedKnots = speedKnots;
    }
}

[CreateAssetMenu(
    fileName = "EngineTelegraphConfig",
    menuName = "Ship/Engine Telegraph Config",
    order = 0
)]
public class EngineTelegraphConfig : ScriptableObject
{
    [SerializeField]
    private EngineTelegraphSectorData[] sectors =
    {
        new EngineTelegraphSectorData(EngineTelegraphSector.Emer, "EMER", -122f, -20f),
        new EngineTelegraphSectorData(EngineTelegraphSector.FullReverse, "ПОЛНЫЙ НАЗАД", -92f, -12f),
        new EngineTelegraphSectorData(EngineTelegraphSector.BackTwoThirds, "НАЗАД 2/3", -60f, -8f),
        new EngineTelegraphSectorData(EngineTelegraphSector.BackOneThird, "НАЗАД 1/3", -30f, -4f),
        new EngineTelegraphSectorData(EngineTelegraphSector.Stop, "СТОП", 0f, 0f),
        new EngineTelegraphSectorData(EngineTelegraphSector.AheadOneThird, "ВПЕРЁД 1/3", 30f, 6f),
        new EngineTelegraphSectorData(EngineTelegraphSector.AheadTwoThirds, "ВПЕРЁД 2/3", 90f, 12f),
        new EngineTelegraphSectorData(EngineTelegraphSector.StandBy, "ВПЕРЁД 2/3", 90f, 16f),
        new EngineTelegraphSectorData(EngineTelegraphSector.FullAhead, "ПОЛНЫЙ ВПЕРЁД", 120f, 25f),
        new EngineTelegraphSectorData(EngineTelegraphSector.FlankAhead, "САМЫЙ ПОЛНЫЙ ВПЕРЁД", 148f, 27f),
    };

    public int SectorCount => sectors == null ? 0 : sectors.Length;
    public bool HasSectors => SectorCount > 0;
    public EngineTelegraphSectorData[] Sectors => sectors;

    public EngineTelegraphSectorData GetSectorData(EngineTelegraphSector sector)
    {
        if (sectors == null)
            return null;

        foreach (EngineTelegraphSectorData sectorData in sectors)
        {
            if (sectorData != null && sectorData.sector == sector)
                return sectorData;
        }

        return null;
    }

    public EngineTelegraphSectorData GetSectorDataByIndex(int index)
    {
        if (!HasSectors)
            return null;

        int clampedIndex = Mathf.Clamp(index, 0, sectors.Length - 1);
        return sectors[clampedIndex];
    }

    public int GetSectorIndex(EngineTelegraphSector sector)
    {
        if (sectors == null)
            return -1;

        for (int i = 0; i < sectors.Length; i++)
        {
            if (sectors[i] != null && sectors[i].sector == sector)
                return i;
        }

        return -1;
    }

    public EngineTelegraphSectorData GetClosestSectorByAngle(float angle)
    {
        if (!HasSectors)
            return null;

        EngineTelegraphSectorData closestSectorData = null;
        float closestDistance = float.MaxValue;

        foreach (EngineTelegraphSectorData sectorData in sectors)
        {
            if (sectorData == null)
                continue;

            float distance = Mathf.Abs(angle - sectorData.angle);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSectorData = sectorData;
            }
        }

        return closestSectorData;
    }

    public EngineTelegraphSectorData GetClosestSectorBySpeedKnots(float speedKnots)
    {
        if (!HasSectors)
            return null;

        EngineTelegraphSectorData closestSectorData = null;
        float closestDistance = float.MaxValue;

        foreach (EngineTelegraphSectorData sectorData in sectors)
        {
            if (sectorData == null)
                continue;

            float distance = Mathf.Abs(speedKnots - sectorData.speedKnots);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSectorData = sectorData;
            }
        }

        return closestSectorData;
    }

    [Obsolete("Use GetClosestSectorBySpeedKnots instead.")]
    public EngineTelegraphSectorData GetClosestSectorBySpeed(float speedValue)
    {
        return GetClosestSectorBySpeedKnots(speedValue);
    }

    public EngineTelegraphSector GetValidOrFallbackSector(EngineTelegraphSector preferredSector)
    {
        if (GetSectorData(preferredSector) != null)
            return preferredSector;

        if (GetSectorData(EngineTelegraphSector.Stop) != null)
            return EngineTelegraphSector.Stop;

        if (HasSectors && sectors[0] != null)
            return sectors[0].sector;

        return EngineTelegraphSector.Stop;
    }

    private void OnValidate()
    {
        if (sectors == null)
            return;

        for (int i = 0; i < sectors.Length; i++)
        {
            if (sectors[i] == null)
            {
                Debug.LogWarning($"EngineTelegraphConfig '{name}': пустой сектор на индексе {i}.", this);
            }
        }
    }
}
