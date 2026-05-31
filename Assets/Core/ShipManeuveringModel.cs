using UnityEngine;

public struct ShipManeuveringResult
{
    public float Speed01;
    public float Rudder01;
    public float SpeedEffectiveness;
    public float RudderEffectiveness;
    public float TurnEffectiveness;
    public float TargetTurnRadius;
    public float TargetYawRateDegreesPerSecond;
}

public static class ShipManeuveringModel
{
    private const float Epsilon = 0.0001f;

    public static ShipManeuveringResult Calculate(
        ShipConfig config,
        float currentSpeedKnots,
        float currentSpeedUnityUnitsPerSecond,
        float actualRudderSignedValue
    )
    {
        ShipManeuveringResult result = new ShipManeuveringResult();

        if (config == null)
            return result;

        float absSpeedKnots = Mathf.Abs(currentSpeedKnots);
        float absSpeedUnitsPerSecond = Mathf.Abs(currentSpeedUnityUnitsPerSecond);
        float absRudder = Mathf.Abs(actualRudderSignedValue);

        result.Speed01 = Mathf.Clamp01(absSpeedKnots / Mathf.Max(0.01f, config.MaxEngineTelegraphSpeedKnots));
        result.Rudder01 = Mathf.Clamp01(absRudder);

        result.SpeedEffectiveness = EvaluateNormalizedCurve(
            config.SpeedToRudderEffectiveness,
            result.Speed01,
            result.Speed01
        );

        result.RudderEffectiveness = EvaluateNormalizedCurve(
            config.RudderToTurnEffectiveness,
            result.Rudder01,
            result.Rudder01
        );

        result.TurnEffectiveness = Mathf.Clamp01(
            result.SpeedEffectiveness * result.RudderEffectiveness
        );

        if (absSpeedUnitsPerSecond <= Epsilon || result.Rudder01 <= Epsilon || result.TurnEffectiveness <= Epsilon)
        {
            result.TargetTurnRadius = 0f;
            result.TargetYawRateDegreesPerSecond = 0f;
            return result;
        }

        result.TargetTurnRadius = Mathf.Lerp(
            config.MaximumTurningRadius,
            config.MinimumTurningRadius,
            result.TurnEffectiveness
        );

        if (result.TargetTurnRadius <= Epsilon)
        {
            result.TargetTurnRadius = 0f;
            result.TargetYawRateDegreesPerSecond = 0f;
            return result;
        }

        float yawRateRadiansPerSecond = absSpeedUnitsPerSecond / result.TargetTurnRadius;
        float yawRateDegreesPerSecond = yawRateRadiansPerSecond * Mathf.Rad2Deg * config.TurnRateMultiplier;

        float rudderSign = Mathf.Sign(actualRudderSignedValue);
        float speedSign = Mathf.Sign(currentSpeedKnots);
        float modelDirectionSign = config.MovementDirectionMultiplier;

        result.TargetYawRateDegreesPerSecond = yawRateDegreesPerSecond * rudderSign * speedSign * modelDirectionSign;

        return result;
    }

    private static float EvaluateNormalizedCurve(AnimationCurve curve, float input01, float fallbackValue)
    {
        if (curve == null || curve.length == 0)
            return Mathf.Clamp01(fallbackValue);

        return Mathf.Clamp01(curve.Evaluate(Mathf.Clamp01(input01)));
    }
}
