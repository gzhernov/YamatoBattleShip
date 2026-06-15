using UnityEditor;
using UnityEngine;

namespace World
{
    [CustomEditor(typeof(WorldAgent))]
    public class WorldAgentEditor : Editor
    {
        private SerializedProperty worldManager;
        private SerializedProperty worldTransform;
        private SerializedProperty initAgentTransform;
        private SerializedProperty isPlayer;
        private SerializedProperty visualSource;
        private SerializedProperty visualObject;
        private SerializedProperty showAgentGizmo;
        private SerializedProperty drawAgentGizmoOnlyWhenSelected;
        private SerializedProperty gizmoIcon;

        private void OnEnable()
        {
            worldManager = serializedObject.FindProperty("worldManager");
            worldTransform = serializedObject.FindProperty("worldTransform");
            initAgentTransform = serializedObject.FindProperty("initAgentTransform");
            isPlayer = serializedObject.FindProperty("isPlayer");
            visualSource = serializedObject.FindProperty("visualSource");
            visualObject = serializedObject.FindProperty("visualObject");
            showAgentGizmo = serializedObject.FindProperty("showAgentGizmo");
            drawAgentGizmoOnlyWhenSelected = serializedObject.FindProperty("drawAgentGizmoOnlyWhenSelected");
            gizmoIcon = serializedObject.FindProperty("gizmoIcon");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(worldManager);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(worldTransform, true);
            EditorGUILayout.PropertyField(initAgentTransform);
            EditorGUILayout.PropertyField(isPlayer);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(visualSource);
            DrawVisualSourceHelpBox();
            EditorGUILayout.PropertyField(visualObject);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(showAgentGizmo);
            EditorGUILayout.PropertyField(drawAgentGizmoOnlyWhenSelected);
            EditorGUILayout.PropertyField(gizmoIcon);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawVisualSourceHelpBox()
        {
            if (visualSource == null)
                return;

            if (visualSource.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox(
                    "При мультивыделении режим использования Visual Source определяется отдельно для каждого WorldAgent.",
                    MessageType.Warning
                );
                return;
            }

            GameObject assignedObject = visualSource.objectReferenceValue as GameObject;

            if (assignedObject == null)
            {
                EditorGUILayout.HelpBox(
                    "Visual Source не назначен. WorldAgent не сможет создать или привязать visualObject.",
                    MessageType.Warning
                );
                return;
            }

            if (PrefabUtility.IsPartOfPrefabAsset(assignedObject))
            {
                EditorGUILayout.HelpBox(
                    "Выбран prefab asset. При активации WorldAgent создаст отдельный runtime-экземпляр visualObject.",
                    MessageType.Info
                );
                return;
            }

            WorldAgent worldAgent = (WorldAgent)target;

            if (worldAgent != null && assignedObject.transform.parent != worldAgent.transform)
            {
                EditorGUILayout.HelpBox(
                    "Visual Source должен быть прямым дочерним объектом WorldAgent.",
                    MessageType.Error
                );
                return;
            }

            EditorGUILayout.HelpBox(
                "Выбран объект сцены. WorldAgent будет использовать его как visualObject.",
                MessageType.Info
            );
        }
    }
}
