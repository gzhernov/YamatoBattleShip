using System;
using System.Collections.Generic;
using UnityEngine;

public static class NavalSalvoDistributionGenerator
{
    public const float MinSpreadFactor = 0f;
    public const float MaxSpreadFactor = 1f;
    public const float MaxGunClusterSpreadFactor = 0.5f;

    public static void GeneratePoints(
        List<Vector2> results,
        float ellipseSizeX,
        float ellipseSizeY,
        int salvoCount,
        int turretsCount,
        int gunsPerTurret,
        float gaussianSpreadFactor,
        float salvoSpreadFactor,
        float turretSpreadFactor,
        float gunClusterSpreadFactor,
        int seed)
    {
        System.Random random = new System.Random(seed);
        GeneratePoints(
            results,
            ellipseSizeX,
            ellipseSizeY,
            salvoCount,
            turretsCount,
            gunsPerTurret,
            gaussianSpreadFactor,
            salvoSpreadFactor,
            turretSpreadFactor,
            gunClusterSpreadFactor,
            () => (float)random.NextDouble()
        );
    }

    public static void GeneratePoints(
        List<Vector2> results,
        float ellipseSizeX,
        float ellipseSizeY,
        int salvoCount,
        int turretsCount,
        int gunsPerTurret,
        float gaussianSpreadFactor,
        float salvoSpreadFactor,
        float turretSpreadFactor,
        float gunClusterSpreadFactor,
        Func<float> nextValue)
    {
        if (results == null)
            throw new ArgumentNullException(nameof(results));

        if (nextValue == null)
            throw new ArgumentNullException(nameof(nextValue));

        results.Clear();

        float clampedEllipseSizeX = Mathf.Max(EllipseClusterDistributionGenerator.MinEllipseSize, ellipseSizeX);
        float clampedEllipseSizeY = Mathf.Max(EllipseClusterDistributionGenerator.MinEllipseSize, ellipseSizeY);
        int clampedSalvoCount = Mathf.Max(1, salvoCount);
        int clampedTurretsCount = Mathf.Max(1, turretsCount);
        int clampedGunsPerTurret = Mathf.Max(1, gunsPerTurret);
        float clampedGaussianSpread = Mathf.Clamp(
            gaussianSpreadFactor,
            EllipseClusterDistributionGenerator.MinGaussianSpreadFactor,
            EllipseClusterDistributionGenerator.MaxGaussianSpreadFactor
        );
        float clampedSalvoSpread = Mathf.Clamp(salvoSpreadFactor, MinSpreadFactor, MaxSpreadFactor);
        float clampedTurretSpread = Mathf.Clamp(turretSpreadFactor, MinSpreadFactor, MaxSpreadFactor);
        float clampedGunClusterSpread = Mathf.Clamp(
            gunClusterSpreadFactor,
            MinSpreadFactor,
            MaxGunClusterSpreadFactor
        );

        float semiX = clampedEllipseSizeX * 0.5f;
        float semiY = clampedEllipseSizeY * 0.5f;
        float gunClusterRadius = Mathf.Min(semiX, semiY) * clampedGunClusterSpread;

        for (int salvoIndex = 0; salvoIndex < clampedSalvoCount; salvoIndex++)
        {
            Vector2 salvoCenter = GenerateSalvoCenter(
                semiX,
                semiY,
                clampedGaussianSpread,
                clampedSalvoSpread,
                nextValue
            );

            for (int turretIndex = 0; turretIndex < clampedTurretsCount; turretIndex++)
            {
                Vector2 turretCenter = GenerateTurretCenter(
                    salvoCenter,
                    semiX,
                    semiY,
                    clampedGaussianSpread,
                    clampedTurretSpread,
                    nextValue
                );

                for (int gunIndex = 0; gunIndex < clampedGunsPerTurret; gunIndex++)
                {
                    Vector2 point = gunClusterRadius > 0f
                        ? EllipseClusterDistributionGenerator.GenerateUniformPointNearCluster(
                            turretCenter,
                            gunClusterRadius,
                            semiX,
                            semiY,
                            nextValue
                        )
                        : turretCenter;

                    results.Add(point);
                }
            }
        }
    }

    private static Vector2 GenerateSalvoCenter(
        float semiX,
        float semiY,
        float gaussianSpreadFactor,
        float salvoSpreadFactor,
        Func<float> nextValue)
    {
        if (salvoSpreadFactor <= 0f)
            return Vector2.zero;

        return EllipseClusterDistributionGenerator.GenerateGaussianPointInsideEllipse(
            semiX * salvoSpreadFactor,
            semiY * salvoSpreadFactor,
            gaussianSpreadFactor,
            nextValue
        );
    }

    private static Vector2 GenerateTurretCenter(
        Vector2 salvoCenter,
        float semiX,
        float semiY,
        float gaussianSpreadFactor,
        float turretSpreadFactor,
        Func<float> nextValue)
    {
        if (turretSpreadFactor <= 0f)
            return salvoCenter;

        Vector2 turretOffset = EllipseClusterDistributionGenerator.GenerateGaussianPointInsideEllipse(
            semiX * turretSpreadFactor,
            semiY * turretSpreadFactor,
            gaussianSpreadFactor,
            nextValue
        );

        return EllipseClusterDistributionGenerator.ClampPointToEllipse(
            salvoCenter + turretOffset,
            semiX,
            semiY
        );
    }
}
