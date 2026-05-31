using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DispersionEllipseProfile))]
public class DispersionEllipseProfileEditor : Editor
{
    private const string PreviewDistanceKey = "DispersionEllipseProfileEditor.PreviewDistance";
    private const float DefaultPreviewDistanceKm = 0.75f;
    private const float MinPreviewDistanceKm = 0f;
    private const float MaxPreviewDistanceKm = 50f;
    private const float MetersPerKilometer = 1000f;

    private SerializedProperty pointsProperty;
    private float previewDistanceKm;

    private void OnEnable()
    {
        pointsProperty = serializedObject.FindProperty("points");
        previewDistanceKm = Mathf.Clamp(
            SessionState.GetFloat(PreviewDistanceKey, DefaultPreviewDistanceKm),
            MinPreviewDistanceKm,
            MaxPreviewDistanceKm
        );
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DispersionEllipseProfile profile = (DispersionEllipseProfile)target;

        DrawHeader(profile);
        DrawWarnings();
        DrawPointsSection();
        DrawPreviewSection(profile);
        DrawHint();

        serializedObject.ApplyModifiedProperties();
        SessionState.SetFloat(PreviewDistanceKey, previewDistanceKm);
    }

    private void DrawHeader(DispersionEllipseProfile profile)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(profile.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Dispersion Ellipse Profile", EditorStyles.miniLabel);
        }
    }

    private void DrawWarnings()
    {
        if (pointsProperty.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Add at least one distance point.", MessageType.Warning);
            return;
        }

        for (int i = 0; i < pointsProperty.arraySize; i++)
        {
            SerializedProperty point = pointsProperty.GetArrayElementAtIndex(i);
            float lateral = point.FindPropertyRelative("lateralSpread").floatValue;
            float longitudinal = point.FindPropertyRelative("longitudinalSpread").floatValue;

            if (Mathf.Approximately(lateral, 0f) || Mathf.Approximately(longitudinal, 0f))
            {
                EditorGUILayout.HelpBox($"Point {i + 1} has zero spread. Ellipse will not be visible.", MessageType.Warning);
            }

            if (lateral > longitudinal)
            {
                EditorGUILayout.HelpBox(
                    $"Point {i + 1}: lateral spread is greater than longitudinal spread. Ellipse will be stretched sideways.",
                    MessageType.Info
                );
            }
        }
    }

    private void DrawPointsSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            Rect labelRect = headerRect;
            labelRect.width = Mathf.Min(180f, headerRect.width * 0.5f);
            EditorGUI.LabelField(labelRect, "Points", EditorStyles.boldLabel);

            Rect countRect = headerRect;
            countRect.xMin = headerRect.xMax - 64f;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.IntField(countRect, pointsProperty.arraySize);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(4f);
            DrawTableHeader();
            EditorGUILayout.Space(2f);

            for (int i = 0; i < pointsProperty.arraySize; i++)
            {
                DrawPointRow(i);
            }

            EditorGUILayout.Space(4f);
            DrawPointsButtons();
        }
    }

    private void DrawTableHeader()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 22f);

        float indexWidth = 24f;
        float miniPreviewWidth = 94f;
        float columnGap = 6f;
        float remainingWidth = rect.width - indexWidth - miniPreviewWidth - (columnGap * 4f);
        float distanceWidth = Mathf.Max(90f, remainingWidth * 0.23f);
        float lateralWidth = Mathf.Max(120f, remainingWidth * 0.38f);
        float longitudinalWidth = Mathf.Max(140f, remainingWidth * 0.39f);

        Rect indexRect = new Rect(rect.x, rect.y, indexWidth, rect.height);
        Rect distanceRect = new Rect(indexRect.xMax + columnGap, rect.y, distanceWidth, rect.height);
        Rect longitudinalRect = new Rect(distanceRect.xMax + columnGap, rect.y, longitudinalWidth, rect.height);
        Rect lateralRect = new Rect(longitudinalRect.xMax + columnGap, rect.y, lateralWidth, rect.height);
        Rect previewRect = new Rect(lateralRect.xMax + columnGap, rect.y, miniPreviewWidth, rect.height);

        EditorGUI.LabelField(indexRect, "#", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(distanceRect, "Distance (km)", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(longitudinalRect, "Longitudinal Spread (m)", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(lateralRect, "Lateral Spread (m)", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(previewRect, "Mini Preview", EditorStyles.miniBoldLabel);
    }

    private void DrawPointRow(int index)
    {
        SerializedProperty point = pointsProperty.GetArrayElementAtIndex(index);
        SerializedProperty distanceProp = point.FindPropertyRelative("distance");
        SerializedProperty lateralProp = point.FindPropertyRelative("lateralSpread");
        SerializedProperty longitudinalProp = point.FindPropertyRelative("longitudinalSpread");

        Rect rowRect = EditorGUILayout.GetControlRect(false, 72f);
        float indexWidth = 24f;
        float miniPreviewWidth = 94f;
        float columnGap = 6f;
        float removeWidth = 22f;

        float remainingWidth = rowRect.width - indexWidth - miniPreviewWidth - removeWidth - (columnGap * 5f);
        float distanceWidth = Mathf.Max(90f, remainingWidth * 0.23f);
        float lateralWidth = Mathf.Max(120f, remainingWidth * 0.38f);
        float longitudinalWidth = Mathf.Max(140f, remainingWidth * 0.39f);

        Rect indexRect = new Rect(rowRect.x, rowRect.y + 24f, indexWidth, EditorGUIUtility.singleLineHeight);
        Rect distanceRect = new Rect(indexRect.xMax + columnGap, rowRect.y + 24f, distanceWidth, EditorGUIUtility.singleLineHeight);
        Rect longitudinalRect = new Rect(distanceRect.xMax + columnGap, rowRect.y + 24f, longitudinalWidth, EditorGUIUtility.singleLineHeight);
        Rect lateralRect = new Rect(longitudinalRect.xMax + columnGap, rowRect.y + 24f, lateralWidth, EditorGUIUtility.singleLineHeight);
        Rect previewRect = new Rect(lateralRect.xMax + columnGap, rowRect.y + 12f, miniPreviewWidth, 48f);
        Rect removeRect = new Rect(previewRect.xMax + 4f, rowRect.y + 24f, removeWidth, EditorGUIUtility.singleLineHeight);

        EditorGUI.LabelField(indexRect, index.ToString(), EditorStyles.miniLabel);
        DrawDelayedScaledFloatField(distanceRect, distanceProp, MetersPerKilometer);
        DrawDelayedFloatField(longitudinalRect, longitudinalProp);
        DrawDelayedFloatField(lateralRect, lateralProp);

        DrawMiniPreview(previewRect, lateralProp.floatValue, longitudinalProp.floatValue);

        if (GUI.Button(removeRect, "-"))
        {
            Undo.RecordObject(target, "Remove Dispersion Point");
            pointsProperty.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }

    private void DrawPointsButtons()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("+", GUILayout.Width(28f)))
            {
                Undo.RecordObject(target, "Add Dispersion Point");
                pointsProperty.InsertArrayElementAtIndex(pointsProperty.arraySize);

                SerializedProperty point = pointsProperty.GetArrayElementAtIndex(pointsProperty.arraySize - 1);
                point.FindPropertyRelative("distance").floatValue = pointsProperty.arraySize > 1
                    ? pointsProperty.GetArrayElementAtIndex(pointsProperty.arraySize - 2).FindPropertyRelative("distance").floatValue + 100f
                    : 100f;
                point.FindPropertyRelative("lateralSpread").floatValue = 0f;
                point.FindPropertyRelative("longitudinalSpread").floatValue = 0f;

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("Sort Points by Distance"))
            {
                SortPointsByDistance();
            }
        }
    }

    private void SortPointsByDistance()
    {
        if (pointsProperty.arraySize <= 1)
            return;

        Undo.RecordObject(target, "Sort Dispersion Points");

        List<DispersionEllipsePoint> points = new List<DispersionEllipsePoint>(pointsProperty.arraySize);
        for (int i = 0; i < pointsProperty.arraySize; i++)
        {
            SerializedProperty point = pointsProperty.GetArrayElementAtIndex(i);
            points.Add(new DispersionEllipsePoint
            {
                distance = point.FindPropertyRelative("distance").floatValue,
                lateralSpread = point.FindPropertyRelative("lateralSpread").floatValue,
                longitudinalSpread = point.FindPropertyRelative("longitudinalSpread").floatValue
            });
        }

        points.Sort((left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < points.Count; i++)
        {
            SerializedProperty point = pointsProperty.GetArrayElementAtIndex(i);
            point.FindPropertyRelative("distance").floatValue = points[i].distance;
            point.FindPropertyRelative("lateralSpread").floatValue = points[i].lateralSpread;
            point.FindPropertyRelative("longitudinalSpread").floatValue = points[i].longitudinalSpread;
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private void DrawPreviewSection(DispersionEllipseProfile profile)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            float maxPreviewDistanceKm = MaxPreviewDistanceKm;
            previewDistanceKm = Mathf.Clamp(previewDistanceKm, MinPreviewDistanceKm, maxPreviewDistanceKm);
            Rect previewLineRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            Rect sliderRect = EditorGUI.PrefixLabel(previewLineRect, new GUIContent("Preview Distance"));
            sliderRect.xMax -= 88f;
            Rect valueRect = previewLineRect;
            valueRect.xMin = sliderRect.xMax + 4f;
            Rect unitRect = valueRect;
            unitRect.xMin = valueRect.xMax - 14f;
            valueRect.xMax = unitRect.xMin - 2f;

            previewDistanceKm = GUI.HorizontalSlider(sliderRect, previewDistanceKm, MinPreviewDistanceKm, maxPreviewDistanceKm);
            previewDistanceKm = EditorGUI.DelayedFloatField(valueRect, previewDistanceKm);
            EditorGUI.LabelField(unitRect, "km");
            previewDistanceKm = Mathf.Clamp(previewDistanceKm, MinPreviewDistanceKm, maxPreviewDistanceKm);

            DispersionEllipse ellipse = profile.Evaluate(previewDistanceKm * MetersPerKilometer);

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Interpolated Ellipse", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Longitudinal: {ellipse.longitudinalSpread:0.##} m");
                EditorGUILayout.LabelField($"Lateral: {ellipse.lateralSpread:0.##} m");
            }

            Rect previewRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(340f), GUILayout.ExpandWidth(true));
            DrawMainPreview(previewRect, ellipse);
        }
    }

    private void DrawHint()
    {
        EditorGUILayout.HelpBox(
            "Longitudinal spread is the full length of the ellipse along the fire direction. Lateral spread is the full width of the ellipse from left to right.",
            MessageType.Info
        );
    }

    private static void DrawMiniPreview(Rect rect, float lateralSpread, float longitudinalSpread)
    {
        EditorGUI.DrawRect(rect, new Color(0.13f, 0.13f, 0.13f, 1f));
        DrawEllipsePreview(rect, lateralSpread, longitudinalSpread, showLabels: false, showGrid: false, tiny: true);
    }

    private static void DrawMainPreview(Rect rect, DispersionEllipse ellipse)
    {
        EditorGUI.DrawRect(rect, new Color(0.14f, 0.14f, 0.14f, 1f));
        DrawEllipsePreview(rect, ellipse.lateralSpread, ellipse.longitudinalSpread, showLabels: true, showGrid: true, tiny: false);
    }

    private static void DrawEllipsePreview(Rect rect, float lateralSpread, float longitudinalSpread, bool showLabels, bool showGrid, bool tiny)
    {
        if (Event.current.type != EventType.Repaint)
            return;

        Handles.BeginGUI();

        Vector2 center = rect.center;
        float maxDimension = Mathf.Max(Mathf.Max(lateralSpread, longitudinalSpread), 1f);
        float targetSize = tiny ? Mathf.Min(rect.width, rect.height) * 0.6f : Mathf.Min(rect.width, rect.height) * 0.72f;
        float scale = targetSize / maxDimension;
        float radiusX = lateralSpread * 0.5f * scale;
        float radiusY = longitudinalSpread * 0.5f * scale;

        if (showGrid)
        {
            DrawGrid(rect);
        }

        if (lateralSpread > 0f && longitudinalSpread > 0f)
        {
            Vector3[] ellipsePoints = BuildEllipsePoints(center, radiusX, radiusY, 72);

            Handles.color = new Color(0.28f, 0.57f, 1f, tiny ? 0.08f : 0.12f);
            Handles.DrawAAConvexPolygon(ellipsePoints);

            Handles.color = new Color(0.28f, 0.57f, 1f, 1f);
            Handles.DrawAAPolyLine(2.5f, ellipsePoints);
        }

        Color axisColor = new Color(1f, 1f, 1f, tiny ? 0.35f : 0.45f);
        Handles.color = axisColor;
        Handles.DrawDottedLine(new Vector3(rect.xMin + 18f, center.y, 0f), new Vector3(rect.xMax - 18f, center.y, 0f), 4f);
        Handles.DrawDottedLine(new Vector3(center.x, rect.yMin + 18f, 0f), new Vector3(center.x, rect.yMax - 18f, 0f), 4f);
        Handles.DrawSolidDisc(center, Vector3.forward, 2.5f);

        if (showLabels && !tiny)
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };

            GUIStyle sizeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft
            };

            Rect fireRect = new Rect(center.x - 72f, rect.yMin + 8f, 144f, 18f);
            GUI.Label(fireRect, "Fire Direction \u2191", titleStyle);

            Rect valueRect = new Rect(rect.xMax - 182f, rect.yMin + 4f, 172f, 30f);
            GUI.Label(valueRect, $"Longitudinal: {longitudinalSpread:0.##} m\nLateral: {lateralSpread:0.##} m", sizeStyle);
        }

        Handles.EndGUI();
    }

    private static void DrawGrid(Rect rect)
    {
        Handles.color = new Color(1f, 1f, 1f, 0.045f);

        const float spacing = 40f;
        for (float x = rect.xMin; x <= rect.xMax; x += spacing)
        {
            Handles.DrawLine(new Vector3(x, rect.yMin, 0f), new Vector3(x, rect.yMax, 0f));
        }

        for (float y = rect.yMin; y <= rect.yMax; y += spacing)
        {
            Handles.DrawLine(new Vector3(rect.xMin, y, 0f), new Vector3(rect.xMax, y, 0f));
        }
    }

    private static Vector3[] BuildEllipsePoints(Vector2 center, float radiusX, float radiusY, int segments)
    {
        Vector3[] points = new Vector3[segments + 1];
        for (int i = 0; i < segments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / segments;
            float x = center.x + Mathf.Cos(angle) * radiusX;
            float y = center.y + Mathf.Sin(angle) * radiusY;
            points[i] = new Vector3(x, y, 0f);
        }

        points[segments] = points[0];

        return points;
    }

    private static void DrawDelayedFloatField(Rect rect, SerializedProperty property)
    {
        EditorGUI.BeginChangeCheck();
        float value = EditorGUI.DelayedFloatField(rect, GUIContent.none, property.floatValue);
        if (EditorGUI.EndChangeCheck())
        {
            property.floatValue = Mathf.Max(0f, value);
        }
    }

    private static void DrawDelayedScaledFloatField(Rect rect, SerializedProperty property, float scaleFromDisplayToRaw)
    {
        EditorGUI.BeginChangeCheck();
        float displayValue = property.floatValue / scaleFromDisplayToRaw;
        float updatedDisplayValue = EditorGUI.DelayedFloatField(rect, GUIContent.none, displayValue);
        if (EditorGUI.EndChangeCheck())
        {
            property.floatValue = Mathf.Max(0f, updatedDisplayValue) * scaleFromDisplayToRaw;
        }
    }
}
