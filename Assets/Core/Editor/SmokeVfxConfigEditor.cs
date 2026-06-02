using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SmokeVfxConfig))]
public class SmokeVfxConfigEditor : Editor
{
    private SerializedProperty sourceEngineTelegraphConfig;
    private SerializedProperty modeEntries;
    private SerializedProperty logValidationWarnings;

    private string syncSummary;

    private void OnEnable()
    {
        sourceEngineTelegraphConfig = serializedObject.FindProperty("sourceEngineTelegraphConfig");
        modeEntries = serializedObject.FindProperty("modeEntries");
        logValidationWarnings = serializedObject.FindProperty("logValidationWarnings");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(sourceEngineTelegraphConfig);

        if (sourceEngineTelegraphConfig.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Назначьте Source Engine Telegraph Config, чтобы синхронизировать список режимов дыма.", MessageType.Info);
        }

        using (new EditorGUI.DisabledScope(sourceEngineTelegraphConfig.objectReferenceValue == null))
        {
            if (GUILayout.Button("Sync From Telegraph Config"))
            {
                SyncFromSource();
            }
        }

        if (!string.IsNullOrEmpty(syncSummary))
        {
            EditorGUILayout.HelpBox(syncSummary, MessageType.None);
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(modeEntries, true);
        EditorGUILayout.PropertyField(logValidationWarnings);

        serializedObject.ApplyModifiedProperties();
    }

    private void SyncFromSource()
    {
        serializedObject.ApplyModifiedProperties();

        SmokeVfxConfig smokeConfig = (SmokeVfxConfig)target;
        int syncedCount = smokeConfig.SyncFromSource();

        EditorUtility.SetDirty(smokeConfig);
        AssetDatabase.SaveAssets();
        serializedObject.Update();

        syncSummary = syncedCount > 0
            ? $"Синхронизировано {syncedCount} записей режимов дыма."
            : "Синхронизация пропущена: источник пустой или не назначен.";
    }
}
