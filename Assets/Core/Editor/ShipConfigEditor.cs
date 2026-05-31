using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ShipConfig))]
public class ShipConfigEditor : Editor
{
    private const string FoldoutPrefix = "ShipConfigEditor.";

    private SerializedProperty engineTelegraphConfig;
    private SerializedProperty engineTelegraphAudioConfig;
    private SerializedProperty engineSoundConfig;
    private SerializedProperty initialEngineTelegraphSector;

    private SerializedProperty rudderAudioConfig;
    private SerializedProperty rudderDefaultPosition;
    private SerializedProperty rudderTotalPositions;
    private SerializedProperty rudderShiftTimeFromCenterToFull;
    private SerializedProperty maxRudderAngleDegrees;

    private SerializedProperty knotsToUnityUnitsPerSecond;
    private SerializedProperty simulationSpeedMultiplier;
    private SerializedProperty movementDirection;

    private SerializedProperty forwardAccelerationKnotsPerSecond;
    private SerializedProperty reverseAccelerationKnotsPerSecond;
    private SerializedProperty naturalCoastingDecelerationKnotsPerSecond;
    private SerializedProperty oppositeThrustDecelerationKnotsPerSecond;

    private SerializedProperty maxRudderDragDecelerationKnotsPerSecond;
    private SerializedProperty maxRudderSpeedLossFraction;
    private SerializedProperty rudderToDragEffectiveness;

    private SerializedProperty shipLength;
    private SerializedProperty minimumTurningRadiusInShipLengths;
    private SerializedProperty maximumTurningRadiusInShipLengths;
    private SerializedProperty rudderToTurnEffectiveness;
    private SerializedProperty speedToRudderEffectiveness;
    private SerializedProperty turnAcceleration;
    private SerializedProperty turnRateMultiplier;

    private SerializedProperty maxManeuverHeelAngle;
    private SerializedProperty yawRateForFullHeel;
    private SerializedProperty speedToHeelEffectiveness;
    private SerializedProperty heelResponseSpeed;
    private SerializedProperty heelRecoverySpeed;

    private SerializedProperty heaveAmplitude;
    private SerializedProperty rollAmplitude;
    private SerializedProperty pitchAmplitude;
    private SerializedProperty waveFrequency;
    private SerializedProperty speedForFullWaveEffectKnots;
    private SerializedProperty waveInfluenceSmoothSpeed;

    private void OnEnable()
    {
        engineTelegraphConfig = serializedObject.FindProperty("engineTelegraphConfig");
        engineTelegraphAudioConfig = serializedObject.FindProperty("engineTelegraphAudioConfig");
        engineSoundConfig = serializedObject.FindProperty("engineSoundConfig");
        initialEngineTelegraphSector = serializedObject.FindProperty("initialEngineTelegraphSector");

        rudderAudioConfig = serializedObject.FindProperty("rudderAudioConfig");
        rudderDefaultPosition = serializedObject.FindProperty("rudderDefaultPosition");
        rudderTotalPositions = serializedObject.FindProperty("rudderTotalPositions");
        rudderShiftTimeFromCenterToFull = serializedObject.FindProperty("rudderShiftTimeFromCenterToFull");
        maxRudderAngleDegrees = serializedObject.FindProperty("maxRudderAngleDegrees");

        knotsToUnityUnitsPerSecond = serializedObject.FindProperty("knotsToUnityUnitsPerSecond");
        simulationSpeedMultiplier = serializedObject.FindProperty("simulationSpeedMultiplier");
        movementDirection = serializedObject.FindProperty("movementDirection");

        forwardAccelerationKnotsPerSecond = serializedObject.FindProperty("forwardAccelerationKnotsPerSecond");
        reverseAccelerationKnotsPerSecond = serializedObject.FindProperty("reverseAccelerationKnotsPerSecond");
        naturalCoastingDecelerationKnotsPerSecond = serializedObject.FindProperty("naturalCoastingDecelerationKnotsPerSecond");
        oppositeThrustDecelerationKnotsPerSecond = serializedObject.FindProperty("oppositeThrustDecelerationKnotsPerSecond");

        maxRudderDragDecelerationKnotsPerSecond = serializedObject.FindProperty("maxRudderDragDecelerationKnotsPerSecond");
        maxRudderSpeedLossFraction = serializedObject.FindProperty("maxRudderSpeedLossFraction");
        rudderToDragEffectiveness = serializedObject.FindProperty("rudderToDragEffectiveness");

        shipLength = serializedObject.FindProperty("shipLength");
        minimumTurningRadiusInShipLengths = serializedObject.FindProperty("minimumTurningRadiusInShipLengths");
        maximumTurningRadiusInShipLengths = serializedObject.FindProperty("maximumTurningRadiusInShipLengths");
        rudderToTurnEffectiveness = serializedObject.FindProperty("rudderToTurnEffectiveness");
        speedToRudderEffectiveness = serializedObject.FindProperty("speedToRudderEffectiveness");
        turnAcceleration = serializedObject.FindProperty("turnAcceleration");
        turnRateMultiplier = serializedObject.FindProperty("turnRateMultiplier");

        maxManeuverHeelAngle = serializedObject.FindProperty("maxManeuverHeelAngle");
        yawRateForFullHeel = serializedObject.FindProperty("yawRateForFullHeel");
        speedToHeelEffectiveness = serializedObject.FindProperty("speedToHeelEffectiveness");
        heelResponseSpeed = serializedObject.FindProperty("heelResponseSpeed");
        heelRecoverySpeed = serializedObject.FindProperty("heelRecoverySpeed");

        heaveAmplitude = serializedObject.FindProperty("heaveAmplitude");
        rollAmplitude = serializedObject.FindProperty("rollAmplitude");
        pitchAmplitude = serializedObject.FindProperty("pitchAmplitude");
        waveFrequency = serializedObject.FindProperty("waveFrequency");
        speedForFullWaveEffectKnots = serializedObject.FindProperty("speedForFullWaveEffectKnots");
        waveInfluenceSmoothSpeed = serializedObject.FindProperty("waveInfluenceSmoothSpeed");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSection(
            "Engine Telegraph",
            BuildEngineTelegraphSummary(),
            () =>
            {
                EditorGUILayout.PropertyField(engineTelegraphConfig);
                EditorGUILayout.PropertyField(engineTelegraphAudioConfig);
                EditorGUILayout.PropertyField(engineSoundConfig);
                EditorGUILayout.PropertyField(initialEngineTelegraphSector);
            },
            true
        );

        DrawSection(
            "Rudder",
            $"default {rudderDefaultPosition.intValue + 1}/{rudderTotalPositions.intValue}, max {maxRudderAngleDegrees.floatValue:0.#} deg",
            () =>
            {
                EditorGUILayout.PropertyField(rudderAudioConfig);
                EditorGUILayout.PropertyField(rudderDefaultPosition);
                EditorGUILayout.PropertyField(rudderTotalPositions);
                EditorGUILayout.PropertyField(rudderShiftTimeFromCenterToFull);
                EditorGUILayout.PropertyField(maxRudderAngleDegrees);
            }
        );

        DrawSection(
            "Movement Scale",
            $"{knotsToUnityUnitsPerSecond.floatValue:0.###} units/sec per knot, x{simulationSpeedMultiplier.floatValue:0.##}",
            () =>
            {
                EditorGUILayout.PropertyField(knotsToUnityUnitsPerSecond);
                EditorGUILayout.PropertyField(simulationSpeedMultiplier);
                EditorGUILayout.PropertyField(movementDirection);
            }
        );

        DrawSection(
            "Propulsion / Inertia",
            $"ahead {forwardAccelerationKnotsPerSecond.floatValue:0.##}, astern {reverseAccelerationKnotsPerSecond.floatValue:0.##} knots/sec",
            () =>
            {
                EditorGUILayout.PropertyField(forwardAccelerationKnotsPerSecond);
                EditorGUILayout.PropertyField(reverseAccelerationKnotsPerSecond);
                EditorGUILayout.PropertyField(naturalCoastingDecelerationKnotsPerSecond);
                EditorGUILayout.PropertyField(oppositeThrustDecelerationKnotsPerSecond);
            }
        );

        DrawSection(
            "Rudder Drag",
            $"drag {maxRudderDragDecelerationKnotsPerSecond.floatValue:0.##}, speed loss {maxRudderSpeedLossFraction.floatValue:P0}",
            () =>
            {
                EditorGUILayout.PropertyField(maxRudderDragDecelerationKnotsPerSecond);
                EditorGUILayout.PropertyField(maxRudderSpeedLossFraction);
                EditorGUILayout.PropertyField(rudderToDragEffectiveness);
            }
        );

        DrawSection(
            "Maneuvering",
            $"length {shipLength.floatValue:0.#}, radius {minimumTurningRadiusInShipLengths.floatValue:0.#}-{maximumTurningRadiusInShipLengths.floatValue:0.#}L",
            () =>
            {
                EditorGUILayout.PropertyField(shipLength);
                EditorGUILayout.PropertyField(minimumTurningRadiusInShipLengths);
                EditorGUILayout.PropertyField(maximumTurningRadiusInShipLengths);
                EditorGUILayout.PropertyField(rudderToTurnEffectiveness);
                EditorGUILayout.PropertyField(speedToRudderEffectiveness);
                EditorGUILayout.PropertyField(turnAcceleration);
                EditorGUILayout.PropertyField(turnRateMultiplier);
            }
        );

        DrawSection(
            "Maneuver Heel",
            $"max {maxManeuverHeelAngle.floatValue:0.#} deg, full at {yawRateForFullHeel.floatValue:0.##} deg/sec",
            () =>
            {
                EditorGUILayout.PropertyField(maxManeuverHeelAngle);
                EditorGUILayout.PropertyField(yawRateForFullHeel);
                EditorGUILayout.PropertyField(speedToHeelEffectiveness);
                EditorGUILayout.PropertyField(heelResponseSpeed);
                EditorGUILayout.PropertyField(heelRecoverySpeed);
            }
        );

        DrawSection(
            "Wave Motion",
            $"heave {heaveAmplitude.floatValue:0.###}, roll {rollAmplitude.floatValue:0.#}, pitch {pitchAmplitude.floatValue:0.#}",
            () =>
            {
                EditorGUILayout.PropertyField(heaveAmplitude);
                EditorGUILayout.PropertyField(rollAmplitude);
                EditorGUILayout.PropertyField(pitchAmplitude);
                EditorGUILayout.PropertyField(waveFrequency);
                EditorGUILayout.PropertyField(speedForFullWaveEffectKnots);
                EditorGUILayout.PropertyField(waveInfluenceSmoothSpeed);
            }
        );

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawSection(string title, string summary, System.Action drawContent, bool defaultOpen = false)
    {
        string key = FoldoutPrefix + title;
        bool isOpen = SessionState.GetBool(key, defaultOpen);

        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            Rect headerRect = EditorGUILayout.GetControlRect(false, 22f);
            headerRect.xMin += 2f;
            headerRect.xMax -= 2f;

            isOpen = EditorGUI.Foldout(headerRect, isOpen, title, true, EditorStyles.foldoutHeader);

            Rect summaryRect = headerRect;
            summaryRect.xMin += Mathf.Min(190f, headerRect.width * 0.6f) + 8f;

            if (summaryRect.width > 32f)
            {
                EditorGUI.LabelField(summaryRect, summary, EditorStyles.miniLabel);
            }

            SessionState.SetBool(key, isOpen);

            if (!isOpen)
                return;

            EditorGUILayout.Space(2f);
            EditorGUI.indentLevel++;
            drawContent();
            EditorGUI.indentLevel--;
        }
    }

    private string BuildEngineTelegraphSummary()
    {
        string configName = engineTelegraphConfig.objectReferenceValue != null
            ? engineTelegraphConfig.objectReferenceValue.name
            : "not assigned";

        string audioConfigName = engineTelegraphAudioConfig.objectReferenceValue != null
            ? engineTelegraphAudioConfig.objectReferenceValue.name
            : "audio not assigned";

        string soundConfigName = engineSoundConfig.objectReferenceValue != null
            ? engineSoundConfig.objectReferenceValue.name
            : "engine sound not assigned";

        return $"{configName}, {audioConfigName}, {soundConfigName}, initial {GetEnumDisplayName(initialEngineTelegraphSector)}";
    }

    private static string GetEnumDisplayName(SerializedProperty property)
    {
        if (property.hasMultipleDifferentValues)
            return "-";

        if (property.enumValueIndex < 0 || property.enumValueIndex >= property.enumDisplayNames.Length)
            return property.enumValueIndex.ToString();

        return property.enumDisplayNames[property.enumValueIndex];
    }
}
