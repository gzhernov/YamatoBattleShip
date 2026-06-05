using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EngineVfxController))]
public class ShipSmokeVfxControllerEditor : Editor
{
    private SerializedProperty shipStatuses;
    private SerializedProperty smokeBindings;
    private SerializedProperty logDebugInfo;

    private string syncSummary;

    private void OnEnable()
    {
        shipStatuses = serializedObject.FindProperty("shipStatuses");
        smokeBindings = serializedObject.FindProperty("smokeBindings");
        logDebugInfo = serializedObject.FindProperty("logDebugInfo");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(shipStatuses);
        EditorGUILayout.Space();

        bool hasConfigSource = HasEngineTelegraphConfig();

        if (!hasConfigSource)
        {
            EditorGUILayout.HelpBox(
                "Назначьте ShipStatuses с доступным EngineTelegraphConfig, чтобы синхронизировать режимы дыма.",
                MessageType.Info
            );
        }

        using (new EditorGUI.DisabledScope(!hasConfigSource))
        {
            if (GUILayout.Button("Sync From Engine Telegraph Config"))
            {
                SyncFromEngineTelegraphConfig();
            }
        }

        if (!string.IsNullOrEmpty(syncSummary))
        {
            EditorGUILayout.HelpBox(syncSummary, MessageType.None);
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(smokeBindings, true);
        EditorGUILayout.PropertyField(logDebugInfo);

        serializedObject.ApplyModifiedProperties();
    }

    private void SyncFromEngineTelegraphConfig()
    {
        serializedObject.ApplyModifiedProperties();

        EngineVfxController controller = (EngineVfxController)target;
        int syncedCount = controller.SyncFromEngineTelegraphConfig();

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        serializedObject.Update();

        syncSummary = syncedCount > 0
            ? $"Синхронизировано {syncedCount} привязок дыма."
            : "Синхронизация пропущена: EngineTelegraphConfig не назначен или пустой.";
    }

    private bool HasEngineTelegraphConfig()
    {
        EngineVfxController controller = (EngineVfxController)target;

        if (controller == null)
            return false;

        ShipStatuses statuses = controller.GetComponentInParent<ShipStatuses>();

        if (statuses == null)
        {
            statuses = controller.GetComponent<ShipStatuses>();
        }

        if (statuses == null || statuses.ShipConfig == null)
            return false;

        return statuses.ShipConfig.EngineTelegraphConfig != null;
    }
}
