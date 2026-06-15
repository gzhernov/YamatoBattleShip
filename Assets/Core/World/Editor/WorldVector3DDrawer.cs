using UnityEditor;
using UnityEngine;

namespace World
{
[CustomPropertyDrawer(typeof(WorldVector3D))]
public sealed class WorldVector3DDrawer : PropertyDrawer
{
    private static readonly GUIContent[] AxisLabels =
    {
        new GUIContent("X"),
        new GUIContent("Y"),
        new GUIContent("Z")
    };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty xProperty = property.FindPropertyRelative("x");
        SerializedProperty yProperty = property.FindPropertyRelative("y");
        SerializedProperty zProperty = property.FindPropertyRelative("z");

        if (xProperty == null || yProperty == null || zProperty == null)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.MultiPropertyField(position, AxisLabels, xProperty, label);
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
}
