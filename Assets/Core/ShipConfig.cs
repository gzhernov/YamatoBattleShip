using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "ShipConfig",
    menuName = "Ship/Ship Config",
    order = 1
)]
public class ShipConfig : ScriptableObject
{
    [Header("Engine Telegraph")]
    [SerializeField] private EngineTelegraphConfig engineTelegraphConfig;
    [SerializeField] private EngineTelegraphAudioConfig engineTelegraphAudioConfig;
    [SerializeField] private EngineSoundConfig engineSoundConfig;
    [SerializeField] private EngineTelegraphSector initialEngineTelegraphSector = EngineTelegraphSector.Stop;

    [Header("Rudder")]
    [SerializeField] private RudderAudioConfig rudderAudioConfig;
    [SerializeField] private int rudderDefaultPosition = 2;
    [SerializeField] private int rudderTotalPositions = 5;

    [Tooltip("Сколько секунд занимает перекладка фактического руля от центра до полного борта. От левого борта до правого будет примерно в 2 раза дольше.")]
    [SerializeField] private float rudderShiftTimeFromCenterToFull = 6f;

    [Tooltip("Максимальный угол перекладки физического пера руля в градусах при signed value = +/-1. Для кораблей часто используют примерно 30-35 градусов.")]
    [SerializeField] private float maxRudderAngleDegrees = 35f;

    [Header("Movement Scale")]
    [Tooltip("Сколько Unity units/sec соответствует 1 узлу. Если 1 Unity unit = 1 метр, используй 0.514444.")]
    [FormerlySerializedAs("speedMultiplier")]
    [SerializeField] private float knotsToUnityUnitsPerSecond = 0.514444f;

    [Tooltip("Игровой множитель скорости. 1 = физическая конвертация узлов в м/с. 2 = в два раза быстрее.")]
    [SerializeField] private float simulationSpeedMultiplier = 1f;

    [Tooltip("Направление движения корабля. В Unity transform.forward — это локальная ось +Z. Если нос модели смотрит в -Z, выбери Local Backward.")]
    [SerializeField] private ShipMovementDirection movementDirection = ShipMovementDirection.LocalForward;

    [Header("Propulsion / Inertia")]
    [Tooltip("Разгон на переднем ходу, узлов/сек. Используется, когда выбранный Ahead-режим быстрее текущей скорости, или после остановки при переходе с заднего хода на передний.")]
    [FormerlySerializedAs("accelerationKnotsPerSecond")]
    [FormerlySerializedAs("acceleration")]
    [SerializeField] private float forwardAccelerationKnotsPerSecond = 2f;

    [Tooltip("Разгон на заднем ходу, узлов/сек. Обычно меньше переднего разгона.")]
    [SerializeField] private float reverseAccelerationKnotsPerSecond = 1f;

    [Tooltip("Естественное замедление корпуса от сопротивления воды, узлов/сек. Используется при Stop и при переходе Full Ahead -> Slow Ahead. Должно быть заметно меньше активного торможения.")]
    [SerializeField] private float naturalCoastingDecelerationKnotsPerSecond = 0.08f;

    [Tooltip("Активное торможение обратной тягой, узлов/сек. Используется, когда двигатель тянет против текущего движения: Ahead при движении назад или Astern при движении вперёд.")]
    [SerializeField] private float oppositeThrustDecelerationKnotsPerSecond = 0.8f;

    [Header("Rudder Drag")]
    [Tooltip("Максимальное дополнительное замедление от полностью переложенного руля, узлов/сек. Добавляется к естественному сопротивлению и активному торможению.")]
    [SerializeField] private float maxRudderDragDecelerationKnotsPerSecond = 0.12f;

    [Tooltip("Максимальная потеря поддерживаемой скорости режима двигателя при полном руле. 0.12 = примерно минус 12% от скорости выбранного режима.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float maxRudderSpeedLossFraction = 0.12f;

    [Tooltip("Кривая сопротивления от фактического отклонения руля. X: 0..1 отклонение руля, Y: 0..1 сила дополнительного drag.")]
    [SerializeField] private AnimationCurve rudderToDragEffectiveness = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.25f, 0.06f),
        new Keyframe(0.5f, 0.22f),
        new Keyframe(0.75f, 0.55f),
        new Keyframe(1f, 1f)
    );

    [Header("Maneuvering")]
    [Tooltip("Длина корабля в Unity units. Если 1 unit = 1 метр, для Yamato можно использовать 263.")]
    [SerializeField] private float shipLength = 263f;

    [Tooltip("Минимальный радиус циркуляции в длинах корпуса при полном руле и эффективной скорости.")]
    [SerializeField] private float minimumTurningRadiusInShipLengths = 4.5f;

    [Tooltip("Максимальный рабочий радиус циркуляции в длинах корпуса при слабом руле/низкой эффективности.")]
    [SerializeField] private float maximumTurningRadiusInShipLengths = 20f;

    [Tooltip("Кривая влияния угла руля на поворот. X: 0..1 отклонение руля, Y: 0..1 эффективность.")]
    [SerializeField] private AnimationCurve rudderToTurnEffectiveness = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.25f, 0.08f),
        new Keyframe(0.5f, 0.3f),
        new Keyframe(0.75f, 0.65f),
        new Keyframe(1f, 1f)
    );

    [Tooltip("Кривая влияния скорости на эффективность руля. X: 0..1 доля от максимальной скорости, Y: 0..1 эффективность.")]
    [SerializeField] private AnimationCurve speedToRudderEffectiveness = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.15f, 0.1f),
        new Keyframe(0.35f, 0.55f),
        new Keyframe(0.7f, 1f),
        new Keyframe(1f, 0.9f)
    );

    [Tooltip("Скорость набора/сброса угловой скорости разворота, градусов/сек^2. Это инерция входа в циркуляцию.")]
    [SerializeField] private float turnAcceleration = 0.75f;

    [Tooltip("Игровой множитель угловой скорости. Увеличь, если реальный радиус выглядит слишком медленно для геймплея.")]
    [SerializeField] private float turnRateMultiplier = 1.5f;

    [Header("Maneuver Heel")]
    [Tooltip("Максимальный визуальный крен от манёвра в градусах. Для крупного корабля обычно достаточно 2-5 градусов.")]
    [SerializeField] private float maxManeuverHeelAngle = 4f;

    [Tooltip("При какой текущей угловой скорости разворота крен достигает максимума. Единица: градусов/сек.")]
    [SerializeField] private float yawRateForFullHeel = 0.8f;

    [Tooltip("Кривая влияния скорости на крен. X: 0..1 доля от максимальной скорости, Y: 0..1 сила крена.")]
    [SerializeField] private AnimationCurve speedToHeelEffectiveness = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.2f, 0.25f),
        new Keyframe(0.5f, 0.8f),
        new Keyframe(1f, 1f)
    );

    [Tooltip("Скорость входа корпуса в крен. Больше значение = быстрее реакция.")]
    [SerializeField] private float heelResponseSpeed = 2f;

    [Tooltip("Скорость возврата корпуса из крена к нулю. Больше значение = быстрее выравнивание.")]
    [SerializeField] private float heelRecoverySpeed = 1.5f;

    [Header("Legacy Steering")]
    [Tooltip("Legacy-поле из старой модели поворота. В новой модели не используется.")]
    [SerializeField, HideInInspector] private float turnSpeed = 30f;

    [Header("Wave Motion")]
    [Tooltip("Вертикальное покачивание модели в Unity units.")]
    [SerializeField] private float heaveAmplitude = 0.06f;

    [Tooltip("Крен в градусах. Для крупного корабля держи маленьким.")]
    [SerializeField] private float rollAmplitude = 0.7f;

    [Tooltip("Дифферент нос-корма в градусах.")]
    [SerializeField] private float pitchAmplitude = 0.3f;

    [Tooltip("Частота качки. Меньше значение = медленнее и тяжелее.")]
    [SerializeField] private float waveFrequency = 0.32f;

    [Tooltip("При какой скорости в узлах качка достигает полной силы.")]
    [SerializeField] private float speedForFullWaveEffectKnots = 12f;

    [Tooltip("Скорость плавного появления/исчезновения качки при изменении скорости корабля.")]
    [SerializeField] private float waveInfluenceSmoothSpeed = 2f;

    public EngineTelegraphConfig EngineTelegraphConfig => engineTelegraphConfig;
    public EngineTelegraphAudioConfig EngineTelegraphAudioConfig => engineTelegraphAudioConfig;
    public EngineSoundConfig EngineSoundConfig => engineSoundConfig;
    public EngineTelegraphSector InitialEngineTelegraphSector => initialEngineTelegraphSector;

    public RudderAudioConfig RudderAudioConfig => rudderAudioConfig;
    public int RudderDefaultPosition => rudderDefaultPosition;
    public int RudderTotalPositions => rudderTotalPositions;
    public float RudderShiftTimeFromCenterToFull => rudderShiftTimeFromCenterToFull;
    public float MaxRudderAngleDegrees => maxRudderAngleDegrees;
    public float RudderShiftSpeedSignedUnitsPerSecond => rudderShiftTimeFromCenterToFull > 0f
        ? 1f / rudderShiftTimeFromCenterToFull
        : float.PositiveInfinity;

    public float KnotsToUnityUnitsPerSecond => knotsToUnityUnitsPerSecond * simulationSpeedMultiplier;
    public float RawKnotsToUnityUnitsPerSecond => knotsToUnityUnitsPerSecond;
    public float SimulationSpeedMultiplier => simulationSpeedMultiplier;

    public float ForwardAccelerationKnotsPerSecond => forwardAccelerationKnotsPerSecond;
    public float ReverseAccelerationKnotsPerSecond => reverseAccelerationKnotsPerSecond;
    public float NaturalCoastingDecelerationKnotsPerSecond => naturalCoastingDecelerationKnotsPerSecond;
    public float OppositeThrustDecelerationKnotsPerSecond => oppositeThrustDecelerationKnotsPerSecond;
    public float MaxRudderDragDecelerationKnotsPerSecond => maxRudderDragDecelerationKnotsPerSecond;
    public float MaxRudderSpeedLossFraction => maxRudderSpeedLossFraction;
    public AnimationCurve RudderToDragEffectiveness => rudderToDragEffectiveness;

    public ShipMovementDirection MovementDirection => movementDirection;
    public float MovementDirectionMultiplier => movementDirection == ShipMovementDirection.LocalForward ? 1f : -1f;

    public float ShipLength => shipLength;
    public float MinimumTurningRadiusInShipLengths => minimumTurningRadiusInShipLengths;
    public float MaximumTurningRadiusInShipLengths => maximumTurningRadiusInShipLengths;
    public float MinimumTurningRadius => shipLength * minimumTurningRadiusInShipLengths;
    public float MaximumTurningRadius => shipLength * maximumTurningRadiusInShipLengths;
    public AnimationCurve RudderToTurnEffectiveness => rudderToTurnEffectiveness;
    public AnimationCurve SpeedToRudderEffectiveness => speedToRudderEffectiveness;
    public float TurnAcceleration => turnAcceleration;
    public float TurnRateMultiplier => turnRateMultiplier;

    public float MaxManeuverHeelAngle => maxManeuverHeelAngle;
    public float YawRateForFullHeel => yawRateForFullHeel;
    public AnimationCurve SpeedToHeelEffectiveness => speedToHeelEffectiveness;
    public float HeelResponseSpeed => heelResponseSpeed;
    public float HeelRecoverySpeed => heelRecoverySpeed;

    public float MaxEngineTelegraphSpeedKnots => GetMaxEngineTelegraphSpeedKnots();

    [Obsolete("Legacy property from the old direct-speed model. Use ForwardAccelerationKnotsPerSecond instead.")]
    public float AccelerationKnotsPerSecond => forwardAccelerationKnotsPerSecond;

    [Obsolete("Legacy property from the old direct-turn model. Use turning radius/yaw rate model instead.")]
    public float TurnSpeed => turnSpeed;

    public float HeaveAmplitude => heaveAmplitude;
    public float RollAmplitude => rollAmplitude;
    public float PitchAmplitude => pitchAmplitude;
    public float WaveFrequency => waveFrequency;
    public float SpeedForFullWaveEffectKnots => speedForFullWaveEffectKnots;
    public float WaveInfluenceSmoothSpeed => waveInfluenceSmoothSpeed;

    [Obsolete("Use KnotsToUnityUnitsPerSecond instead.")]
    public float SpeedMultiplier => KnotsToUnityUnitsPerSecond;

    [Obsolete("Use ForwardAccelerationKnotsPerSecond instead.")]
    public float Acceleration => forwardAccelerationKnotsPerSecond;

    private float GetMaxEngineTelegraphSpeedKnots()
    {
        if (engineTelegraphConfig == null || !engineTelegraphConfig.HasSectors)
            return 1f;

        float maxAbsSpeed = 0f;

        for (int i = 0; i < engineTelegraphConfig.SectorCount; i++)
        {
            EngineTelegraphSectorData sectorData = engineTelegraphConfig.GetSectorDataByIndex(i);

            if (sectorData == null)
                continue;

            maxAbsSpeed = Mathf.Max(maxAbsSpeed, Mathf.Abs(sectorData.speedKnots));
        }

        return Mathf.Max(0.01f, maxAbsSpeed);
    }

    private void OnValidate()
    {
        rudderTotalPositions = Mathf.Max(2, rudderTotalPositions);
        rudderDefaultPosition = Mathf.Clamp(rudderDefaultPosition, 0, rudderTotalPositions - 1);
        rudderShiftTimeFromCenterToFull = Mathf.Max(0.01f, rudderShiftTimeFromCenterToFull);
        maxRudderAngleDegrees = Mathf.Max(0f, maxRudderAngleDegrees);

        knotsToUnityUnitsPerSecond = Mathf.Max(0f, knotsToUnityUnitsPerSecond);
        simulationSpeedMultiplier = Mathf.Max(0f, simulationSpeedMultiplier);
        forwardAccelerationKnotsPerSecond = Mathf.Max(0f, forwardAccelerationKnotsPerSecond);
        reverseAccelerationKnotsPerSecond = Mathf.Max(0f, reverseAccelerationKnotsPerSecond);
        naturalCoastingDecelerationKnotsPerSecond = Mathf.Max(0f, naturalCoastingDecelerationKnotsPerSecond);
        oppositeThrustDecelerationKnotsPerSecond = Mathf.Max(0f, oppositeThrustDecelerationKnotsPerSecond);
        maxRudderDragDecelerationKnotsPerSecond = Mathf.Max(0f, maxRudderDragDecelerationKnotsPerSecond);
        maxRudderSpeedLossFraction = Mathf.Clamp(maxRudderSpeedLossFraction, 0f, 0.5f);

        shipLength = Mathf.Max(0.01f, shipLength);
        minimumTurningRadiusInShipLengths = Mathf.Max(0.01f, minimumTurningRadiusInShipLengths);
        maximumTurningRadiusInShipLengths = Mathf.Max(minimumTurningRadiusInShipLengths, maximumTurningRadiusInShipLengths);
        turnAcceleration = Mathf.Max(0f, turnAcceleration);
        turnRateMultiplier = Mathf.Max(0f, turnRateMultiplier);

        maxManeuverHeelAngle = Mathf.Max(0f, maxManeuverHeelAngle);
        yawRateForFullHeel = Mathf.Max(0.01f, yawRateForFullHeel);
        heelResponseSpeed = Mathf.Max(0f, heelResponseSpeed);
        heelRecoverySpeed = Mathf.Max(0f, heelRecoverySpeed);
        turnSpeed = Mathf.Max(0f, turnSpeed);

        EnsureCurve(ref rudderToTurnEffectiveness, CreateDefaultRudderEffectivenessCurve());
        EnsureCurve(ref speedToRudderEffectiveness, CreateDefaultSpeedEffectivenessCurve());
        EnsureCurve(ref speedToHeelEffectiveness, CreateDefaultSpeedToHeelEffectivenessCurve());
        EnsureCurve(ref rudderToDragEffectiveness, CreateDefaultRudderDragEffectivenessCurve());

        heaveAmplitude = Mathf.Max(0f, heaveAmplitude);
        rollAmplitude = Mathf.Max(0f, rollAmplitude);
        pitchAmplitude = Mathf.Max(0f, pitchAmplitude);
        waveFrequency = Mathf.Max(0f, waveFrequency);
        speedForFullWaveEffectKnots = Mathf.Max(0.01f, speedForFullWaveEffectKnots);
        waveInfluenceSmoothSpeed = Mathf.Max(0f, waveInfluenceSmoothSpeed);

        if (engineTelegraphConfig != null && engineTelegraphConfig.HasSectors)
        {
            initialEngineTelegraphSector = engineTelegraphConfig.GetValidOrFallbackSector(initialEngineTelegraphSector);
        }

        if (engineTelegraphAudioConfig != null)
        {
            engineTelegraphAudioConfig.ValidateAgainst(engineTelegraphConfig, this);
        }

        if (engineSoundConfig != null)
        {
            engineSoundConfig.Validate(this);
        }

        if (rudderAudioConfig != null)
        {
            rudderAudioConfig.Validate(this);
        }
    }

    private static void EnsureCurve(ref AnimationCurve curve, AnimationCurve fallback)
    {
        if (curve == null || curve.length == 0)
        {
            curve = fallback;
        }
    }

    private static AnimationCurve CreateDefaultRudderEffectivenessCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.25f, 0.08f),
            new Keyframe(0.5f, 0.3f),
            new Keyframe(0.75f, 0.65f),
            new Keyframe(1f, 1f)
        );
    }

    private static AnimationCurve CreateDefaultSpeedEffectivenessCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.15f, 0.1f),
            new Keyframe(0.35f, 0.55f),
            new Keyframe(0.7f, 1f),
            new Keyframe(1f, 0.9f)
        );
    }

    private static AnimationCurve CreateDefaultSpeedToHeelEffectivenessCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.2f, 0.25f),
            new Keyframe(0.5f, 0.8f),
            new Keyframe(1f, 1f)
        );
    }

    private static AnimationCurve CreateDefaultRudderDragEffectivenessCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.25f, 0.06f),
            new Keyframe(0.5f, 0.22f),
            new Keyframe(0.75f, 0.55f),
            new Keyframe(1f, 1f)
        );
    }
}
