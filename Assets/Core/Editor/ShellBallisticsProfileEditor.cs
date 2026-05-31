using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ShellBallisticsProfile))]
public class ShellBallisticsProfileEditor : Editor
{
    private const float MetersPerKilometer = 1000f;
    private const float BallisticsTableWidth = 760f;

    private SerializedProperty descriptionProperty;
    private SerializedProperty shellTypeProperty;
    private SerializedProperty caliberMmProperty;
    private SerializedProperty weightKgProperty;
    private SerializedProperty projectileLengthCmProperty;
    private SerializedProperty explosiveMassKgProperty;
    private SerializedProperty explosiveTypeProperty;
    private SerializedProperty fuseDescriptionProperty;
    private SerializedProperty fuseDelaySecondsProperty;
    private SerializedProperty muzzleVelocityMetersPerSecondProperty;
    private SerializedProperty propellantChargeKgProperty;
    private SerializedProperty propellantDescriptionProperty;
    private SerializedProperty maxRangeKmAt45DegreesProperty;
    private SerializedProperty dispersionProfileProperty;
    private SerializedProperty ballisticTableProperty;
    private Vector2 ballisticsTableScroll;

    private void OnEnable()
    {
        descriptionProperty = serializedObject.FindProperty("description");
        shellTypeProperty = serializedObject.FindProperty("shellType");
        caliberMmProperty = serializedObject.FindProperty("caliberMm");
        weightKgProperty = serializedObject.FindProperty("weightKg");
        projectileLengthCmProperty = serializedObject.FindProperty("projectileLengthCm");
        explosiveMassKgProperty = serializedObject.FindProperty("explosiveMassKg");
        explosiveTypeProperty = serializedObject.FindProperty("explosiveType");
        fuseDescriptionProperty = serializedObject.FindProperty("fuseDescription");
        fuseDelaySecondsProperty = serializedObject.FindProperty("fuseDelaySeconds");
        muzzleVelocityMetersPerSecondProperty = serializedObject.FindProperty("muzzleVelocityMetersPerSecond");
        propellantChargeKgProperty = serializedObject.FindProperty("propellantChargeKg");
        propellantDescriptionProperty = serializedObject.FindProperty("propellantDescription");
        maxRangeKmAt45DegreesProperty = serializedObject.FindProperty("maxRangeKmAt45Degrees");
        dispersionProfileProperty = serializedObject.FindProperty("dispersionProfile");
        ballisticTableProperty = serializedObject.FindProperty("ballisticTable");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeader();
        DrawProfileFields();
        DrawRangeInfoAndWarnings();
        DrawBallisticsTable();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeader()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(target.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Shell Ballistics Profile", EditorStyles.miniLabel);
        }
    }

    private void DrawProfileFields()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Shell Characteristics", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(descriptionProperty, new GUIContent("Description / History"));
            EditorGUILayout.PropertyField(shellTypeProperty, new GUIContent("Shell Type"));

            DrawDelayedFloatField(EditorGUILayout.GetControlRect(), caliberMmProperty, new GUIContent("Caliber (mm)"));
            DrawDelayedFloatField(EditorGUILayout.GetControlRect(), weightKgProperty, new GUIContent("Weight (kg)"));
            DrawDelayedFloatField(EditorGUILayout.GetControlRect(), projectileLengthCmProperty, new GUIContent("Projectile Length (cm)"));
            DrawDelayedFloatField(EditorGUILayout.GetControlRect(), explosiveMassKgProperty, new GUIContent("Explosive Mass (kg)"));

            EditorGUILayout.PropertyField(explosiveTypeProperty, new GUIContent("Explosive Type"));
            EditorGUILayout.PropertyField(fuseDescriptionProperty, new GUIContent("Fuse"));

            DrawDelayedFloatField(EditorGUILayout.GetControlRect(), fuseDelaySecondsProperty, new GUIContent("Fuse Delay (s)"));
            DrawDelayedFloatField(EditorGUILayout.GetControlRect(), muzzleVelocityMetersPerSecondProperty, new GUIContent("Muzzle Velocity (m/s)"));
            DrawDelayedFloatField(EditorGUILayout.GetControlRect(), propellantChargeKgProperty, new GUIContent("Propellant Charge (kg)"));

            EditorGUILayout.PropertyField(propellantDescriptionProperty, new GUIContent("Propellant Description"));

            DrawDelayedFloatField(EditorGUILayout.GetControlRect(), maxRangeKmAt45DegreesProperty, new GUIContent("Max Range at 45 deg (km)"));

            EditorGUILayout.PropertyField(dispersionProfileProperty, new GUIContent("Dispersion Profile"));
        }
    }

    private void DrawRangeInfoAndWarnings()
    {
        DispersionEllipseProfile dispersionProfile = dispersionProfileProperty.objectReferenceValue as DispersionEllipseProfile;
        bool hasBallisticPoints = ballisticTableProperty.arraySize > 0;
        bool hasDispersionProfile = dispersionProfile != null;

        if (!hasBallisticPoints)
        {
            EditorGUILayout.HelpBox("Add at least one ballistics point.", MessageType.Warning);
        }

        if (!hasDispersionProfile)
        {
            EditorGUILayout.HelpBox("Assign a dispersion profile asset.", MessageType.Warning);
        }

        if (!hasBallisticPoints || !hasDispersionProfile)
            return;

        bool hasDispersionPoints = dispersionProfile.Points != null && dispersionProfile.Points.Count > 0;
        if (!hasDispersionPoints)
        {
            EditorGUILayout.HelpBox("Assigned dispersion profile has no points.", MessageType.Warning);
            return;
        }

        float ballisticMinKm = GetMinRangeKm();
        float ballisticMaxKm = GetMaxRangeKm();
        float dispersionMinKm = dispersionProfile.GetMinDistance() / MetersPerKilometer;
        float dispersionMaxKm = dispersionProfile.GetMaxDistance() / MetersPerKilometer;

        EditorGUILayout.HelpBox(
            $"Ballistics range: {ballisticMinKm:0.###}-{ballisticMaxKm:0.###} km, dispersion range: {dispersionMinKm:0.###}-{dispersionMaxKm:0.###} km.",
            MessageType.Info
        );

        if (ballisticMaxKm < dispersionMinKm || ballisticMinKm > dispersionMaxKm)
        {
            EditorGUILayout.HelpBox("Ballistics and dispersion ranges do not overlap.", MessageType.Warning);
            return;
        }

        if (ballisticMinKm < dispersionMinKm || ballisticMaxKm > dispersionMaxKm)
        {
            EditorGUILayout.HelpBox("Ballistics range extends outside the dispersion range. Dispersion values outside that range will clamp to the nearest dispersion point.", MessageType.Warning);
        }
    }

    private void DrawBallisticsTable()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            Rect titleRect = headerRect;
            titleRect.width = Mathf.Min(220f, headerRect.width * 0.5f);
            EditorGUI.LabelField(titleRect, "Ballistics Table", EditorStyles.boldLabel);

            Rect countRect = headerRect;
            countRect.xMin = headerRect.xMax - 64f;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.IntField(countRect, ballisticTableProperty.arraySize);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(4f);
            float tableHeight = 22f + 2f + Mathf.Max(1, ballisticTableProperty.arraySize) * 46f;
            tableHeight = Mathf.Min(300f, tableHeight);

            using (EditorGUILayout.ScrollViewScope scrollView = new EditorGUILayout.ScrollViewScope(
                ballisticsTableScroll,
                true,
                false,
                GUILayout.Height(tableHeight)))
            {
                ballisticsTableScroll = scrollView.scrollPosition;
                DrawTableHeader();
                EditorGUILayout.Space(2f);

                for (int i = 0; i < ballisticTableProperty.arraySize; i++)
                {
                    DrawBallisticRow(i);
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Checked cells are known values. Unchecked cells are stored as '-'.", EditorStyles.miniLabel);
            DrawTableButtons();
        }
    }

    private void DrawTableHeader()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 22f, GUILayout.Width(BallisticsTableWidth));
        BallisticsTableLayout layout = CalculateTableLayout(rect);

        EditorGUI.LabelField(layout.indexRect, "#", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(layout.rangeRect, "Range (km)", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(layout.elevationRect, "Elevation", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(layout.flightTimeRect, "Flight Time", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(layout.fallAngleRect, "Fall Angle", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(layout.impactVelocityRect, "Impact Velocity", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(layout.armorPenetrationRect, "Armor Pen. (mm)", EditorStyles.miniBoldLabel);
    }

    private void DrawBallisticRow(int index)
    {
        SerializedProperty point = ballisticTableProperty.GetArrayElementAtIndex(index);
        SerializedProperty rangeKm = point.FindPropertyRelative("rangeKm");
        SerializedProperty hasElevationAngle = point.FindPropertyRelative("hasElevationAngle");
        SerializedProperty elevationAngleDegrees = point.FindPropertyRelative("elevationAngleDegrees");
        SerializedProperty hasFlightTime = point.FindPropertyRelative("hasFlightTime");
        SerializedProperty flightTimeSeconds = point.FindPropertyRelative("flightTimeSeconds");
        SerializedProperty hasFallAngle = point.FindPropertyRelative("hasFallAngle");
        SerializedProperty fallAngleDegrees = point.FindPropertyRelative("fallAngleDegrees");
        SerializedProperty hasImpactVelocity = point.FindPropertyRelative("hasImpactVelocity");
        SerializedProperty impactVelocityMetersPerSecond = point.FindPropertyRelative("impactVelocityMetersPerSecond");
        SerializedProperty hasArmorPenetration = point.FindPropertyRelative("hasArmorPenetration");
        SerializedProperty armorPenetrationMm = point.FindPropertyRelative("armorPenetrationMm");

        Rect rowRect = EditorGUILayout.GetControlRect(false, 46f, GUILayout.Width(BallisticsTableWidth));
        BallisticsTableLayout layout = CalculateTableLayout(rowRect);
        OffsetRowLayout(ref layout, 12f);

        EditorGUI.LabelField(layout.indexRect, index.ToString(), EditorStyles.miniLabel);
        DrawDelayedFloatField(layout.rangeRect, rangeKm);
        DrawOptionalFloatField(layout.elevationRect, hasElevationAngle, elevationAngleDegrees);
        DrawOptionalFloatField(layout.flightTimeRect, hasFlightTime, flightTimeSeconds);
        DrawOptionalFloatField(layout.fallAngleRect, hasFallAngle, fallAngleDegrees);
        DrawOptionalFloatField(layout.impactVelocityRect, hasImpactVelocity, impactVelocityMetersPerSecond);
        DrawOptionalFloatField(layout.armorPenetrationRect, hasArmorPenetration, armorPenetrationMm);

        if (GUI.Button(layout.removeRect, "-"))
        {
            Undo.RecordObject(target, "Remove Ballistics Point");
            ballisticTableProperty.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }

    private void DrawTableButtons()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("+", GUILayout.Width(28f)))
            {
                Undo.RecordObject(target, "Add Ballistics Point");
                ballisticTableProperty.InsertArrayElementAtIndex(ballisticTableProperty.arraySize);

                SerializedProperty point = ballisticTableProperty.GetArrayElementAtIndex(ballisticTableProperty.arraySize - 1);
                point.FindPropertyRelative("rangeKm").floatValue = ballisticTableProperty.arraySize > 1
                    ? ballisticTableProperty.GetArrayElementAtIndex(ballisticTableProperty.arraySize - 2).FindPropertyRelative("rangeKm").floatValue + 1f
                    : 5f;
                point.FindPropertyRelative("hasElevationAngle").boolValue = false;
                point.FindPropertyRelative("elevationAngleDegrees").floatValue = 0f;
                point.FindPropertyRelative("hasFlightTime").boolValue = false;
                point.FindPropertyRelative("flightTimeSeconds").floatValue = 0f;
                point.FindPropertyRelative("hasFallAngle").boolValue = false;
                point.FindPropertyRelative("fallAngleDegrees").floatValue = 0f;
                point.FindPropertyRelative("hasImpactVelocity").boolValue = false;
                point.FindPropertyRelative("impactVelocityMetersPerSecond").floatValue = 0f;
                point.FindPropertyRelative("hasArmorPenetration").boolValue = false;
                point.FindPropertyRelative("armorPenetrationMm").floatValue = 0f;

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("Sort by Range"))
            {
                SortByRange();
            }
        }
    }

    private void DrawOptionalFloatField(Rect rect, SerializedProperty hasValue, SerializedProperty valueProperty)
    {
        Rect toggleRect = rect;
        toggleRect.width = 18f;

        Rect valueRect = rect;
        valueRect.xMin = toggleRect.xMax + 4f;

        hasValue.boolValue = EditorGUI.Toggle(toggleRect, hasValue.boolValue);

        if (hasValue.boolValue)
        {
            DrawDelayedFloatField(valueRect, valueProperty);
        }
        else
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.TextField(valueRect, "-");
            EditorGUI.EndDisabledGroup();
        }
    }

    private void SortByRange()
    {
        if (ballisticTableProperty.arraySize <= 1)
            return;

        Undo.RecordObject(target, "Sort Ballistics Points");

        List<ShellBallisticPoint> points = new List<ShellBallisticPoint>(ballisticTableProperty.arraySize);
        for (int i = 0; i < ballisticTableProperty.arraySize; i++)
        {
            SerializedProperty point = ballisticTableProperty.GetArrayElementAtIndex(i);
            points.Add(new ShellBallisticPoint
            {
                rangeKm = point.FindPropertyRelative("rangeKm").floatValue,
                hasElevationAngle = point.FindPropertyRelative("hasElevationAngle").boolValue,
                elevationAngleDegrees = point.FindPropertyRelative("elevationAngleDegrees").floatValue,
                hasFlightTime = point.FindPropertyRelative("hasFlightTime").boolValue,
                flightTimeSeconds = point.FindPropertyRelative("flightTimeSeconds").floatValue,
                hasFallAngle = point.FindPropertyRelative("hasFallAngle").boolValue,
                fallAngleDegrees = point.FindPropertyRelative("fallAngleDegrees").floatValue,
                hasImpactVelocity = point.FindPropertyRelative("hasImpactVelocity").boolValue,
                impactVelocityMetersPerSecond = point.FindPropertyRelative("impactVelocityMetersPerSecond").floatValue,
                hasArmorPenetration = point.FindPropertyRelative("hasArmorPenetration").boolValue,
                armorPenetrationMm = point.FindPropertyRelative("armorPenetrationMm").floatValue
            });
        }

        points.Sort((left, right) => left.rangeKm.CompareTo(right.rangeKm));

        for (int i = 0; i < points.Count; i++)
        {
            SerializedProperty point = ballisticTableProperty.GetArrayElementAtIndex(i);
            point.FindPropertyRelative("rangeKm").floatValue = points[i].rangeKm;
            point.FindPropertyRelative("hasElevationAngle").boolValue = points[i].hasElevationAngle;
            point.FindPropertyRelative("elevationAngleDegrees").floatValue = points[i].elevationAngleDegrees;
            point.FindPropertyRelative("hasFlightTime").boolValue = points[i].hasFlightTime;
            point.FindPropertyRelative("flightTimeSeconds").floatValue = points[i].flightTimeSeconds;
            point.FindPropertyRelative("hasFallAngle").boolValue = points[i].hasFallAngle;
            point.FindPropertyRelative("fallAngleDegrees").floatValue = points[i].fallAngleDegrees;
            point.FindPropertyRelative("hasImpactVelocity").boolValue = points[i].hasImpactVelocity;
            point.FindPropertyRelative("impactVelocityMetersPerSecond").floatValue = points[i].impactVelocityMetersPerSecond;
            point.FindPropertyRelative("hasArmorPenetration").boolValue = points[i].hasArmorPenetration;
            point.FindPropertyRelative("armorPenetrationMm").floatValue = points[i].armorPenetrationMm;
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private float GetMinRangeKm()
    {
        if (ballisticTableProperty.arraySize == 0)
            return 0f;

        float minRangeKm = ballisticTableProperty.GetArrayElementAtIndex(0).FindPropertyRelative("rangeKm").floatValue;
        for (int i = 1; i < ballisticTableProperty.arraySize; i++)
        {
            minRangeKm = Mathf.Min(minRangeKm, ballisticTableProperty.GetArrayElementAtIndex(i).FindPropertyRelative("rangeKm").floatValue);
        }

        return minRangeKm;
    }

    private float GetMaxRangeKm()
    {
        if (ballisticTableProperty.arraySize == 0)
            return 0f;

        float maxRangeKm = ballisticTableProperty.GetArrayElementAtIndex(0).FindPropertyRelative("rangeKm").floatValue;
        for (int i = 1; i < ballisticTableProperty.arraySize; i++)
        {
            maxRangeKm = Mathf.Max(maxRangeKm, ballisticTableProperty.GetArrayElementAtIndex(i).FindPropertyRelative("rangeKm").floatValue);
        }

        return maxRangeKm;
    }

    private static void DrawDelayedFloatField(Rect rect, SerializedProperty property)
    {
        DrawDelayedFloatField(rect, property, GUIContent.none);
    }

    private static void DrawDelayedFloatField(Rect rect, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginChangeCheck();
        float value = EditorGUI.DelayedFloatField(rect, label, property.floatValue);
        if (EditorGUI.EndChangeCheck())
        {
            property.floatValue = Mathf.Max(0f, value);
        }
    }

    private static BallisticsTableLayout CalculateTableLayout(Rect rect)
    {
        float indexWidth = 24f;
        float removeWidth = 22f;
        float columnGap = 6f;
        float availableWidth = rect.width - indexWidth - removeWidth - (columnGap * 7f);

        float rangeWidth = Mathf.Max(72f, availableWidth * 0.12f);
        float elevationWidth = Mathf.Max(92f, availableWidth * 0.15f);
        float flightTimeWidth = Mathf.Max(92f, availableWidth * 0.15f);
        float fallAngleWidth = Mathf.Max(92f, availableWidth * 0.15f);
        float velocityWidth = Mathf.Max(112f, availableWidth * 0.2f);
        float armorWidth = Mathf.Max(118f, availableWidth * 0.23f);

        BallisticsTableLayout layout = new BallisticsTableLayout();
        layout.indexRect = new Rect(rect.x, rect.y, indexWidth, rect.height);
        layout.rangeRect = new Rect(layout.indexRect.xMax + columnGap, rect.y, rangeWidth, rect.height);
        layout.elevationRect = new Rect(layout.rangeRect.xMax + columnGap, rect.y, elevationWidth, rect.height);
        layout.flightTimeRect = new Rect(layout.elevationRect.xMax + columnGap, rect.y, flightTimeWidth, rect.height);
        layout.fallAngleRect = new Rect(layout.flightTimeRect.xMax + columnGap, rect.y, fallAngleWidth, rect.height);
        layout.impactVelocityRect = new Rect(layout.fallAngleRect.xMax + columnGap, rect.y, velocityWidth, rect.height);
        layout.armorPenetrationRect = new Rect(layout.impactVelocityRect.xMax + columnGap, rect.y, armorWidth, rect.height);
        layout.removeRect = new Rect(layout.armorPenetrationRect.xMax + columnGap, rect.y, removeWidth, rect.height);
        return layout;
    }

    private static void OffsetRowLayout(ref BallisticsTableLayout layout, float yOffset)
    {
        OffsetRowRect(ref layout.indexRect, yOffset);
        OffsetRowRect(ref layout.rangeRect, yOffset);
        OffsetRowRect(ref layout.elevationRect, yOffset);
        OffsetRowRect(ref layout.flightTimeRect, yOffset);
        OffsetRowRect(ref layout.fallAngleRect, yOffset);
        OffsetRowRect(ref layout.impactVelocityRect, yOffset);
        OffsetRowRect(ref layout.armorPenetrationRect, yOffset);
        OffsetRowRect(ref layout.removeRect, yOffset);
    }

    private static void OffsetRowRect(ref Rect rect, float yOffset)
    {
        rect.y += yOffset;
        rect.height = EditorGUIUtility.singleLineHeight;
    }

    private struct BallisticsTableLayout
    {
        public Rect indexRect;
        public Rect rangeRect;
        public Rect elevationRect;
        public Rect flightTimeRect;
        public Rect fallAngleRect;
        public Rect impactVelocityRect;
        public Rect armorPenetrationRect;
        public Rect removeRect;
    }
}
