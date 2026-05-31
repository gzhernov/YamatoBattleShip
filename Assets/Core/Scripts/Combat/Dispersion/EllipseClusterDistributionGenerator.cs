using System;
using System.Collections.Generic;
using UnityEngine;

public static class EllipseClusterDistributionGenerator
{
    public const float MinEllipseSize = 0.01f;
    public const float MinGaussianSpreadFactor = 0.05f;
    public const float MaxGaussianSpreadFactor = 1f;
    public const float MinClusterSpreadFactor = 0.001f;
    public const float MaxClusterSpreadFactor = 0.5f;

    private const int MaxSamplingAttempts = 100;
    private const float MinRandomValue = 0.000001f;

    public static void GeneratePoints(
        List<Vector2> results,
        float ellipseSizeX,
        float ellipseSizeY,
        int pointsCount,
        int pointsPerCluster,
        float gaussianSpreadFactor,
        float clusterSpreadFactor,
        int seed)
    {
        System.Random random = new System.Random(seed);
        GeneratePoints(
            results,
            ellipseSizeX,
            ellipseSizeY,
            pointsCount,
            pointsPerCluster,
            gaussianSpreadFactor,
            clusterSpreadFactor,
            () => (float)random.NextDouble()
        );
    }

    public static void GeneratePoints(
        List<Vector2> results,
        float ellipseSizeX,
        float ellipseSizeY,
        int pointsCount,
        int pointsPerCluster,
        float gaussianSpreadFactor,
        float clusterSpreadFactor,
        Func<float> nextValue)
    {
        if (results == null)
            throw new ArgumentNullException(nameof(results));

        if (nextValue == null)
            throw new ArgumentNullException(nameof(nextValue));

        results.Clear();

        float clampedSizeX = Mathf.Max(MinEllipseSize, ellipseSizeX);
        float clampedSizeY = Mathf.Max(MinEllipseSize, ellipseSizeY);
        int clampedPointsCount = Mathf.Max(1, pointsCount);
        int clampedPointsPerCluster = Mathf.Max(1, pointsPerCluster);
        float clampedGaussianSpread = Mathf.Clamp(
            gaussianSpreadFactor,
            MinGaussianSpreadFactor,
            MaxGaussianSpreadFactor
        );
        float clampedClusterSpread = Mathf.Clamp(
            clusterSpreadFactor,
            MinClusterSpreadFactor,
            MaxClusterSpreadFactor
        );

        float semiX = clampedSizeX * 0.5f;
        float semiY = clampedSizeY * 0.5f;
        float clusterRadius = Mathf.Min(semiX, semiY) * clampedClusterSpread;

        int created = 0;
        while (created < clampedPointsCount)
        {
            Vector2 clusterCenter = GenerateGaussianPointInsideEllipse(
                semiX,
                semiY,
                clampedGaussianSpread,
                nextValue
            );

            int pointsInThisCluster = Mathf.Min(clampedPointsPerCluster, clampedPointsCount - created);
            for (int i = 0; i < pointsInThisCluster; i++)
            {
                Vector2 point = GenerateUniformPointNearCluster(
                    clusterCenter,
                    clusterRadius,
                    semiX,
                    semiY,
                    nextValue
                );

                results.Add(point);
                created++;
            }
        }
    }

    public static Vector2 GenerateGaussianPointInsideEllipse(
        float semiX,
        float semiY,
        float spreadFactor,
        Func<float> nextValue)
    {
        for (int attempt = 0; attempt < MaxSamplingAttempts; attempt++)
        {
            float x = NextGaussian(nextValue) * semiX * spreadFactor;
            float y = NextGaussian(nextValue) * semiY * spreadFactor;
            Vector2 point = new Vector2(x, y);

            if (IsInsideEllipse(point, semiX, semiY))
                return point;
        }

        return Vector2.zero;
    }

    public static Vector2 GenerateUniformPointNearCluster(
        Vector2 clusterCenter,
        float radius,
        float semiX,
        float semiY,
        Func<float> nextValue)
    {
        for (int attempt = 0; attempt < MaxSamplingAttempts; attempt++)
        {
            Vector2 offset = NextInsideUnitCircle(nextValue) * radius;
            Vector2 point = clusterCenter + offset;

            if (IsInsideEllipse(point, semiX, semiY))
                return point;
        }

        return ClampPointToEllipse(clusterCenter, semiX, semiY);
    }

    public static bool IsInsideEllipse(Vector2 point, float semiX, float semiY)
    {
        float nx = point.x / semiX;
        float ny = point.y / semiY;
        return (nx * nx) + (ny * ny) <= 1f;
    }

    public static Vector2 ClampPointToEllipse(Vector2 point, float semiX, float semiY)
    {
        float nx = point.x / semiX;
        float ny = point.y / semiY;
        float value = (nx * nx) + (ny * ny);

        if (value <= 1f)
            return point;

        float scale = 1f / Mathf.Sqrt(value);
        return new Vector2(point.x * scale, point.y * scale);
    }

    private static Vector2 NextInsideUnitCircle(Func<float> nextValue)
    {
        float angle = Mathf.Clamp01(nextValue()) * Mathf.PI * 2f;
        float radius = Mathf.Sqrt(Mathf.Clamp01(nextValue()));

        return new Vector2(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(angle) * radius
        );
    }

    private static float NextGaussian(Func<float> nextValue)
    {
        float u1 = Mathf.Max(MinRandomValue, 1f - Mathf.Clamp01(nextValue()));
        float u2 = 1f - Mathf.Clamp01(nextValue());

        return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
    }
}
