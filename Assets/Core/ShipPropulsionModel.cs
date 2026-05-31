using UnityEngine;

public enum ShipPropulsionState
{
    Holding,
    CoastingToStop,
    AcceleratingWithThrust,
    CoastingDownToOrder,
    OppositeThrustBraking
}

public struct ShipPropulsionResult
{
    public float CurrentSpeedKnots;
    public float EngineOrderSpeedKnots;

    // Скорость режима двигателя после учёта потерь от переложенного руля.
    // Например Full Ahead 27 узлов при полном руле и MaxRudderSpeedLossFraction = 0.12
    // становится примерно 23.8 узла поддерживаемой скорости.
    public float EffectiveEngineOrderSpeedKnots;

    public float NewSpeedKnots;
    public float AppliedAccelerationKnotsPerSecond;
    public float SpeedDeltaKnots;

    public float RudderDragEffectiveness;
    public float RudderDragDecelerationKnotsPerSecond;

    public ShipPropulsionState State;
}

/// <summary>
/// Рассчитывает продольное изменение скорости корабля по текущей команде машинного телеграфа.
/// Отвечает за разгон, выбег, торможение встречной тягой и потерю скорости от руля,
/// но сам не перемещает и не поворачивает корабль.
/// </summary>
public static class ShipPropulsionModel
{
    private const float SpeedEpsilonKnots = 0.001f;

    public static ShipPropulsionResult Calculate(
        ShipConfig config,
        float currentSpeedKnots,
        float engineOrderSpeedKnots,
        float actualRudderSignedValue,
        float deltaTime
    )
    {
        ShipPropulsionResult result = new ShipPropulsionResult
        {
            CurrentSpeedKnots = currentSpeedKnots,
            EngineOrderSpeedKnots = engineOrderSpeedKnots,
            EffectiveEngineOrderSpeedKnots = engineOrderSpeedKnots,
            NewSpeedKnots = currentSpeedKnots,
            AppliedAccelerationKnotsPerSecond = 0f,
            SpeedDeltaKnots = 0f,
            RudderDragEffectiveness = 0f,
            RudderDragDecelerationKnotsPerSecond = 0f,
            State = ShipPropulsionState.Holding
        };

        if (config == null || deltaTime <= 0f)
            return result;

        float rudderDragEffectiveness = GetRudderDragEffectiveness(
            config,
            actualRudderSignedValue
        );

        float rudderDragDeceleration = GetRudderDragDecelerationKnotsPerSecond(
            config,
            currentSpeedKnots,
            rudderDragEffectiveness
        );

        float effectiveEngineOrderSpeedKnots = GetEffectiveEngineOrderSpeedKnots(
            config,
            engineOrderSpeedKnots,
            rudderDragEffectiveness
        );

        float newSpeedKnots = CalculateNewSpeed(
            config,
            currentSpeedKnots,
            effectiveEngineOrderSpeedKnots,
            rudderDragDeceleration,
            deltaTime,
            out ShipPropulsionState state
        );

        result.EffectiveEngineOrderSpeedKnots = effectiveEngineOrderSpeedKnots;
        result.NewSpeedKnots = newSpeedKnots;
        result.SpeedDeltaKnots = newSpeedKnots - currentSpeedKnots;
        result.AppliedAccelerationKnotsPerSecond = result.SpeedDeltaKnots / deltaTime;
        result.RudderDragEffectiveness = rudderDragEffectiveness;
        result.RudderDragDecelerationKnotsPerSecond = rudderDragDeceleration;
        result.State = state;

        return result;
    }

    private static float CalculateNewSpeed(
        ShipConfig config,
        float currentSpeedKnots,
        float effectiveEngineOrderSpeedKnots,
        float rudderDragDecelerationKnotsPerSecond,
        float deltaTime,
        out ShipPropulsionState state
    )
    {
        state = ShipPropulsionState.Holding;

        if (IsNearlyZero(effectiveEngineOrderSpeedKnots))
        {
            state = ShipPropulsionState.CoastingToStop;
            return MoveTowards(
                currentSpeedKnots,
                0f,
                config.NaturalCoastingDecelerationKnotsPerSecond + rudderDragDecelerationKnotsPerSecond,
                deltaTime
            );
        }

        if (IsNearlyZero(currentSpeedKnots))
        {
            state = ShipPropulsionState.AcceleratingWithThrust;
            return MoveTowards(
                currentSpeedKnots,
                effectiveEngineOrderSpeedKnots,
                GetAccelerationForOrder(config, effectiveEngineOrderSpeedKnots),
                deltaTime
            );
        }

        int currentSpeedSign = Sign(currentSpeedKnots);
        int engineOrderSign = Sign(effectiveEngineOrderSpeedKnots);

        if (currentSpeedSign != engineOrderSign)
        {
            state = ShipPropulsionState.OppositeThrustBraking;
            return ApplyOppositeThrust(
                config,
                currentSpeedKnots,
                effectiveEngineOrderSpeedKnots,
                rudderDragDecelerationKnotsPerSecond,
                deltaTime
            );
        }

        float absCurrentSpeed = Mathf.Abs(currentSpeedKnots);
        float absEngineOrderSpeed = Mathf.Abs(effectiveEngineOrderSpeedKnots);

        if (absCurrentSpeed < absEngineOrderSpeed - SpeedEpsilonKnots)
        {
            state = ShipPropulsionState.AcceleratingWithThrust;
            return MoveTowards(
                currentSpeedKnots,
                effectiveEngineOrderSpeedKnots,
                GetAccelerationForOrder(config, effectiveEngineOrderSpeedKnots),
                deltaTime
            );
        }

        if (absCurrentSpeed > absEngineOrderSpeed + SpeedEpsilonKnots)
        {
            state = ShipPropulsionState.CoastingDownToOrder;
            return MoveTowards(
                currentSpeedKnots,
                effectiveEngineOrderSpeedKnots,
                config.NaturalCoastingDecelerationKnotsPerSecond + rudderDragDecelerationKnotsPerSecond,
                deltaTime
            );
        }

        state = ShipPropulsionState.Holding;
        return effectiveEngineOrderSpeedKnots;
    }

    private static float ApplyOppositeThrust(
        ShipConfig config,
        float currentSpeedKnots,
        float effectiveEngineOrderSpeedKnots,
        float rudderDragDecelerationKnotsPerSecond,
        float deltaTime
    )
    {
        float brakingAcceleration = Mathf.Max(
            0f,
            config.OppositeThrustDecelerationKnotsPerSecond + rudderDragDecelerationKnotsPerSecond
        );

        if (brakingAcceleration <= 0f)
            return currentSpeedKnots;

        float speedToZero = Mathf.Abs(currentSpeedKnots);
        float brakingStep = brakingAcceleration * deltaTime;

        if (brakingStep < speedToZero)
        {
            return Mathf.MoveTowards(currentSpeedKnots, 0f, brakingStep);
        }

        float timeToZero = speedToZero / brakingAcceleration;
        float remainingTime = Mathf.Max(0f, deltaTime - timeToZero);
        float accelerationAfterStop = GetAccelerationForOrder(config, effectiveEngineOrderSpeedKnots);

        return MoveTowards(
            0f,
            effectiveEngineOrderSpeedKnots,
            accelerationAfterStop,
            remainingTime
        );
    }

    private static float GetEffectiveEngineOrderSpeedKnots(
        ShipConfig config,
        float engineOrderSpeedKnots,
        float rudderDragEffectiveness
    )
    {
        if (IsNearlyZero(engineOrderSpeedKnots))
            return 0f;

        float speedLossFactor = Mathf.Clamp01(
            config.MaxRudderSpeedLossFraction * Mathf.Clamp01(rudderDragEffectiveness)
        );

        return engineOrderSpeedKnots * (1f - speedLossFactor);
    }

    private static float GetRudderDragEffectiveness(
        ShipConfig config,
        float actualRudderSignedValue
    )
    {
        float rudderAbs = Mathf.Clamp01(Mathf.Abs(actualRudderSignedValue));

        AnimationCurve curve = config.RudderToDragEffectiveness;
        if (curve == null || curve.length == 0)
            return rudderAbs;

        return Mathf.Clamp01(curve.Evaluate(rudderAbs));
    }

    private static float GetRudderDragDecelerationKnotsPerSecond(
        ShipConfig config,
        float currentSpeedKnots,
        float rudderDragEffectiveness
    )
    {
        if (IsNearlyZero(currentSpeedKnots) || rudderDragEffectiveness <= 0f)
            return 0f;

        float speed01 = Mathf.InverseLerp(
            0f,
            config.MaxEngineTelegraphSpeedKnots,
            Mathf.Abs(currentSpeedKnots)
        );

        return Mathf.Max(0f, config.MaxRudderDragDecelerationKnotsPerSecond)
            * Mathf.Clamp01(rudderDragEffectiveness)
            * speed01;
    }

    private static float MoveTowards(
        float current,
        float target,
        float speedKnotsPerSecond,
        float deltaTime
    )
    {
        if (speedKnotsPerSecond <= 0f)
            return current;

        return Mathf.MoveTowards(
            current,
            target,
            speedKnotsPerSecond * deltaTime
        );
    }

    private static float GetAccelerationForOrder(ShipConfig config, float engineOrderSpeedKnots)
    {
        return engineOrderSpeedKnots >= 0f
            ? config.ForwardAccelerationKnotsPerSecond
            : config.ReverseAccelerationKnotsPerSecond;
    }

    private static bool IsNearlyZero(float value)
    {
        return Mathf.Abs(value) <= SpeedEpsilonKnots;
    }

    private static int Sign(float value)
    {
        if (value > SpeedEpsilonKnots)
            return 1;

        if (value < -SpeedEpsilonKnots)
            return -1;

        return 0;
    }
}
