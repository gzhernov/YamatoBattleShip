using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "BattleShip/Combat/Shell Ballistics Profile",
    fileName = "New Shell Ballistics Profile")]
public class ShellBallisticsProfile : ScriptableObject
{
    private const float MetersPerKilometer = 1000f;

    [TextArea(4, 12)]
    [SerializeField] private string description;

    [SerializeField] private string shellType;

    [Min(0f)]
    [SerializeField] private float caliberMm;

    [Min(0f)]
    [SerializeField] private float weightKg;

    [Min(0f)]
    [SerializeField] private float projectileLengthCm;

    [Min(0f)]
    [SerializeField] private float explosiveMassKg;

    [SerializeField] private string explosiveType;
    [SerializeField] private string fuseDescription;

    [Min(0f)]
    [SerializeField] private float fuseDelaySeconds;

    [Min(0f)]
    [SerializeField] private float muzzleVelocityMetersPerSecond;

    [Min(0f)]
    [SerializeField] private float propellantChargeKg;

    [SerializeField] private string propellantDescription;

    [Min(0f)]
    [SerializeField] private float maxRangeKmAt45Degrees;

    [SerializeField] private DispersionEllipseProfile dispersionProfile;

    [SerializeField] private List<ShellBallisticPoint> ballisticTable = new List<ShellBallisticPoint>();

    public string Description => description;
    public string ShellType => shellType;
    public float CaliberMm => caliberMm;
    public float WeightKg => weightKg;
    public float ProjectileLengthCm => projectileLengthCm;
    public float ExplosiveMassKg => explosiveMassKg;
    public string ExplosiveType => explosiveType;
    public string FuseDescription => fuseDescription;
    public float FuseDelaySeconds => fuseDelaySeconds;
    public float MuzzleVelocityMetersPerSecond => muzzleVelocityMetersPerSecond;
    public float PropellantChargeKg => propellantChargeKg;
    public string PropellantDescription => propellantDescription;
    public float MaxRangeKmAt45Degrees => maxRangeKmAt45Degrees;
    public DispersionEllipseProfile DispersionProfile => dispersionProfile;
    public IReadOnlyList<ShellBallisticPoint> BallisticTable => ballisticTable;

    public ShellBallistics EvaluateBallistics(float rangeKm)
    {
        if (ballisticTable == null || ballisticTable.Count == 0)
            return ShellBallistics.Empty(rangeKm);

        if (ballisticTable.Count == 1)
            return ShellBallistics.FromPoint(rangeKm, ballisticTable[0]);

        List<ShellBallisticPoint> sortedPoints = GetSortedBallisticPoints();

        if (rangeKm <= sortedPoints[0].rangeKm)
            return ShellBallistics.FromPoint(rangeKm, sortedPoints[0]);

        int lastIndex = sortedPoints.Count - 1;
        if (rangeKm >= sortedPoints[lastIndex].rangeKm)
            return ShellBallistics.FromPoint(rangeKm, sortedPoints[lastIndex]);

        int upperIndex = FindUpperBound(sortedPoints, rangeKm);
        ShellBallisticPoint lower = sortedPoints[Mathf.Max(0, upperIndex - 1)];
        ShellBallisticPoint upper = sortedPoints[upperIndex];

        float span = upper.rangeKm - lower.rangeKm;
        if (Mathf.Approximately(span, 0f))
            return ShellBallistics.FromPoint(rangeKm, upper);

        float t = Mathf.Clamp01((rangeKm - lower.rangeKm) / span);

        return new ShellBallistics(
            rangeKm,
            lower.hasElevationAngle && upper.hasElevationAngle,
            Mathf.Lerp(lower.elevationAngleDegrees, upper.elevationAngleDegrees, t),
            lower.hasFlightTime && upper.hasFlightTime,
            Mathf.Lerp(lower.flightTimeSeconds, upper.flightTimeSeconds, t),
            lower.hasFallAngle && upper.hasFallAngle,
            Mathf.Lerp(lower.fallAngleDegrees, upper.fallAngleDegrees, t),
            lower.hasImpactVelocity && upper.hasImpactVelocity,
            Mathf.Lerp(lower.impactVelocityMetersPerSecond, upper.impactVelocityMetersPerSecond, t),
            lower.hasArmorPenetration && upper.hasArmorPenetration,
            Mathf.Lerp(lower.armorPenetrationMm, upper.armorPenetrationMm, t)
        );
    }

    public DispersionEllipse GetDispersion(float rangeKm)
    {
        if (dispersionProfile == null)
            return new DispersionEllipse(rangeKm * MetersPerKilometer, 0f, 0f);

        return dispersionProfile.Evaluate(rangeKm * MetersPerKilometer);
    }

    public float GetMinRangeKm()
    {
        if (ballisticTable == null || ballisticTable.Count == 0)
            return 0f;

        float minRangeKm = ballisticTable[0].rangeKm;
        for (int i = 1; i < ballisticTable.Count; i++)
        {
            minRangeKm = Mathf.Min(minRangeKm, ballisticTable[i].rangeKm);
        }

        return minRangeKm;
    }

    public float GetMaxRangeKm()
    {
        if (ballisticTable == null || ballisticTable.Count == 0)
            return 0f;

        float maxRangeKm = ballisticTable[0].rangeKm;
        for (int i = 1; i < ballisticTable.Count; i++)
        {
            maxRangeKm = Mathf.Max(maxRangeKm, ballisticTable[i].rangeKm);
        }

        return maxRangeKm;
    }

    private List<ShellBallisticPoint> GetSortedBallisticPoints()
    {
        List<ShellBallisticPoint> sortedPoints = new List<ShellBallisticPoint>(ballisticTable);
        sortedPoints.Sort((left, right) => left.rangeKm.CompareTo(right.rangeKm));
        return sortedPoints;
    }

    private static int FindUpperBound(List<ShellBallisticPoint> sortedPoints, float rangeKm)
    {
        int low = 0;
        int high = sortedPoints.Count - 1;

        while (low <= high)
        {
            int mid = low + ((high - low) >> 1);
            if (sortedPoints[mid].rangeKm < rangeKm)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return Mathf.Clamp(low, 1, sortedPoints.Count - 1);
    }
}

[Serializable]
public struct ShellBallisticPoint
{
    [Min(0f)]
    public float rangeKm;

    public bool hasElevationAngle;
    public float elevationAngleDegrees;

    public bool hasFlightTime;

    [Min(0f)]
    public float flightTimeSeconds;

    public bool hasFallAngle;
    public float fallAngleDegrees;

    public bool hasImpactVelocity;

    [Min(0f)]
    public float impactVelocityMetersPerSecond;

    public bool hasArmorPenetration;

    [Min(0f)]
    public float armorPenetrationMm;
}

public readonly struct ShellBallistics
{
    public readonly float rangeKm;
    public readonly bool hasElevationAngle;
    public readonly float elevationAngleDegrees;
    public readonly bool hasFlightTime;
    public readonly float flightTimeSeconds;
    public readonly bool hasFallAngle;
    public readonly float fallAngleDegrees;
    public readonly bool hasImpactVelocity;
    public readonly float impactVelocityMetersPerSecond;
    public readonly bool hasArmorPenetration;
    public readonly float armorPenetrationMm;

    public ShellBallistics(
        float rangeKm,
        bool hasElevationAngle,
        float elevationAngleDegrees,
        bool hasFlightTime,
        float flightTimeSeconds,
        bool hasFallAngle,
        float fallAngleDegrees,
        bool hasImpactVelocity,
        float impactVelocityMetersPerSecond,
        bool hasArmorPenetration,
        float armorPenetrationMm)
    {
        this.rangeKm = rangeKm;
        this.hasElevationAngle = hasElevationAngle;
        this.elevationAngleDegrees = hasElevationAngle ? elevationAngleDegrees : 0f;
        this.hasFlightTime = hasFlightTime;
        this.flightTimeSeconds = hasFlightTime ? flightTimeSeconds : 0f;
        this.hasFallAngle = hasFallAngle;
        this.fallAngleDegrees = hasFallAngle ? fallAngleDegrees : 0f;
        this.hasImpactVelocity = hasImpactVelocity;
        this.impactVelocityMetersPerSecond = hasImpactVelocity ? impactVelocityMetersPerSecond : 0f;
        this.hasArmorPenetration = hasArmorPenetration;
        this.armorPenetrationMm = hasArmorPenetration ? armorPenetrationMm : 0f;
    }

    public static ShellBallistics Empty(float rangeKm)
    {
        return new ShellBallistics(rangeKm, false, 0f, false, 0f, false, 0f, false, 0f, false, 0f);
    }

    public static ShellBallistics FromPoint(float rangeKm, ShellBallisticPoint point)
    {
        return new ShellBallistics(
            rangeKm,
            point.hasElevationAngle,
            point.elevationAngleDegrees,
            point.hasFlightTime,
            point.flightTimeSeconds,
            point.hasFallAngle,
            point.fallAngleDegrees,
            point.hasImpactVelocity,
            point.impactVelocityMetersPerSecond,
            point.hasArmorPenetration,
            point.armorPenetrationMm
        );
    }
}
