using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "BattleShip/Combat/Dispersion Ellipse Profile",
    fileName = "New Dispersion Ellipse Profile")]
public class DispersionEllipseProfile : ScriptableObject
{
    [SerializeField]
    private List<DispersionEllipsePoint> points = new List<DispersionEllipsePoint>();

    public IReadOnlyList<DispersionEllipsePoint> Points => points;

    public DispersionEllipse Evaluate(float distance)
    {
        if (points == null || points.Count == 0)
            return new DispersionEllipse(distance, 0f, 0f);

        if (points.Count == 1)
        {
            DispersionEllipsePoint singlePoint = points[0];
            return new DispersionEllipse(distance, singlePoint.lateralSpread, singlePoint.longitudinalSpread);
        }

        List<DispersionEllipsePoint> sortedPoints = GetSortedPoints();

        if (distance <= sortedPoints[0].distance)
        {
            DispersionEllipsePoint firstPoint = sortedPoints[0];
            return new DispersionEllipse(distance, firstPoint.lateralSpread, firstPoint.longitudinalSpread);
        }

        int lastIndex = sortedPoints.Count - 1;
        if (distance >= sortedPoints[lastIndex].distance)
        {
            DispersionEllipsePoint lastPoint = sortedPoints[lastIndex];
            return new DispersionEllipse(distance, lastPoint.lateralSpread, lastPoint.longitudinalSpread);
        }

        int upperIndex = FindUpperBound(sortedPoints, distance);
        int lowerIndex = Mathf.Max(0, upperIndex - 1);

        DispersionEllipsePoint lower = sortedPoints[lowerIndex];
        DispersionEllipsePoint upper = sortedPoints[upperIndex];

        float span = upper.distance - lower.distance;
        if (Mathf.Approximately(span, 0f))
        {
            return new DispersionEllipse(distance, upper.lateralSpread, upper.longitudinalSpread);
        }

        float t = Mathf.Clamp01((distance - lower.distance) / span);
        float lateralSpread = Mathf.Lerp(lower.lateralSpread, upper.lateralSpread, t);
        float longitudinalSpread = Mathf.Lerp(lower.longitudinalSpread, upper.longitudinalSpread, t);

        return new DispersionEllipse(distance, lateralSpread, longitudinalSpread);
    }

    public float GetMinDistance()
    {
        if (points == null || points.Count == 0)
            return 0f;

        float minDistance = points[0].distance;
        for (int i = 1; i < points.Count; i++)
        {
            minDistance = Mathf.Min(minDistance, points[i].distance);
        }

        return minDistance;
    }

    public float GetMaxDistance()
    {
        if (points == null || points.Count == 0)
            return 0f;

        float maxDistance = points[0].distance;
        for (int i = 1; i < points.Count; i++)
        {
            maxDistance = Mathf.Max(maxDistance, points[i].distance);
        }

        return maxDistance;
    }

    private List<DispersionEllipsePoint> GetSortedPoints()
    {
        List<DispersionEllipsePoint> sortedPoints = new List<DispersionEllipsePoint>(points);
        sortedPoints.Sort((left, right) => left.distance.CompareTo(right.distance));
        return sortedPoints;
    }

    private static int FindUpperBound(List<DispersionEllipsePoint> sortedPoints, float distance)
    {
        int low = 0;
        int high = sortedPoints.Count - 1;

        while (low <= high)
        {
            int mid = low + ((high - low) >> 1);
            if (sortedPoints[mid].distance < distance)
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
public struct DispersionEllipsePoint
{
    [Min(0f)]
    public float distance;

    [Min(0f)]
    public float lateralSpread;

    [Min(0f)]
    public float longitudinalSpread;
}

public readonly struct DispersionEllipse
{
    public readonly float distance;
    public readonly float lateralSpread;
    public readonly float longitudinalSpread;

    public DispersionEllipse(float distance, float lateralSpread, float longitudinalSpread)
    {
        this.distance = distance;
        this.lateralSpread = lateralSpread;
        this.longitudinalSpread = longitudinalSpread;
    }
}
