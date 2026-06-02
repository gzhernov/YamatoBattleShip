using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ShipSmokeVfxController))]
public class ShipSmokeVfxControllerEditor : Editor
{
    private SerializedProperty shipStatuses;
    private SerializedProperty smokeVfxConfigOverride;
    private SerializedProperty smokeBindings;
    private SerializedProperty logDebugInfo;

    private string syncSummary;

    private void OnEnable()
    {
        shipStatuses = serializedObject.FindProperty("shipStatuses");
        smokeVfxConfigOverride = serializedObject.FindProperty("smokeVfxConfigOverride");
        smokeBindings = serializedObject.FindProperty("smokeBindings");
        logDebugInfo = serializedObject.FindProperty("logDebugInfo");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(shipStatuses);
        EditorGUILayout.PropertyField(smokeVfxConfigOverride);

        EditorGUILayout.Space();

        bool hasConfigSource = smokeVfxConfigOverride.objectReferenceValue != null || shipStatuses.objectReferenceValue != null;

        if (!hasConfigSource)
        {
            EditorGUILayout.HelpBox(
                "Назначьте Smoke VFX Config в override или у ShipConfig через ShipStatuses, чтобы синхронизация режимов работала.",
                MessageType.Info
            );
        }

        using (new EditorGUI.DisabledScope(!hasConfigSource))
        {
            if (GUILayout.Button("Sync From Smoke Config"))
            {
                SyncFromSmokeConfig();
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

    private void SyncFromSmokeConfig()
    {
        serializedObject.ApplyModifiedProperties();

        ShipSmokeVfxController controller = (ShipSmokeVfxController)target;
        int syncedCount = controller.SyncFromSmokeConfig();

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        serializedObject.Update();

        syncSummary = syncedCount > 0
            ? $"Синхронизировано {syncedCount} привязок дыма."
            : "Синхронизация пропущена: конфиг дыма не назначен или пустой.";
    }
}
