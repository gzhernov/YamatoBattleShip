using UnityEditor;
using UnityEngine;

namespace World
{
[CustomPropertyDrawer(typeof(WorldTransform))]
public sealed class WorldTransformDrawer : PropertyDrawer
{
    private static readonly GUIContent[] AxisLabels =
    {
        new GUIContent("X"),
        new GUIContent("Y"),
        new GUIContent("Z")
    };

    private const float LineSpacing = 2f;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty positionProperty = property.FindPropertyRelative("position");
        SerializedProperty rotationProperty = property.FindPropertyRelative("rotation");
        SerializedProperty scaleProperty = property.FindPropertyRelative("scale");

        if (positionProperty == null || rotationProperty == null || scaleProperty == null)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        EditorGUI.BeginProperty(position, label, property);

        Rect lineRect = new Rect(
            position.x,
            position.y,
            position.width,
            EditorGUIUtility.singleLineHeight
        );

        DrawPositionRow(lineRect, positionProperty);

        lineRect.y += EditorGUIUtility.singleLineHeight + LineSpacing;
        DrawRotationRow(lineRect, rotationProperty);

        lineRect.y += EditorGUIUtility.singleLineHeight + LineSpacing;
        EditorGUI.PropertyField(lineRect, scaleProperty, new GUIContent("Scale"));

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return (EditorGUIUtility.singleLineHeight * 3f) + (LineSpacing * 2f);
    }

    private static void DrawPositionRow(Rect position, SerializedProperty positionProperty)
    {
        SerializedProperty xProperty = positionProperty.FindPropertyRelative("x");
        SerializedProperty yProperty = positionProperty.FindPropertyRelative("y");
        SerializedProperty zProperty = positionProperty.FindPropertyRelative("z");

        if (xProperty == null || yProperty == null || zProperty == null)
        {
            EditorGUI.PropertyField(position, positionProperty, new GUIContent("Position"), true);
            return;
        }

        EditorGUI.MultiPropertyField(position, AxisLabels, xProperty, new GUIContent("Position"));
    }

    private static void DrawRotationRow(Rect position, SerializedProperty rotationProperty)
    {
        EditorGUI.BeginChangeCheck();

        bool previousMixedValue = EditorGUI.showMixedValue;
        EditorGUI.showMixedValue = rotationProperty.hasMultipleDifferentValues;

        Vector3 eulerAngles = rotationProperty.quaternionValue.eulerAngles;
        Vector3 updatedEulerAngles = EditorGUI.Vector3Field(position, "Rotation", eulerAngles);

        EditorGUI.showMixedValue = previousMixedValue;

        if (EditorGUI.EndChangeCheck())
        {
            rotationProperty.quaternionValue = Quaternion.Euler(updatedEulerAngles);
        }
    }

}
}
