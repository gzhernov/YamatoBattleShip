using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimpleEngineSoundConfig))]
public class SimpleEngineSoundConfigEditor : Editor
{
    private SerializedProperty sourceEngineTelegraphConfig;
    private SerializedProperty audioClip;
    private SerializedProperty transitionTimeSeconds;
    private SerializedProperty sectorEntries;
    private SerializedProperty logValidationWarnings;

    private string syncSummary;

    private void OnEnable()
    {
        sourceEngineTelegraphConfig = serializedObject.FindProperty("sourceEngineTelegraphConfig");
        audioClip = serializedObject.FindProperty("audioClip");
        transitionTimeSeconds = serializedObject.FindProperty("transitionTimeSeconds");
        sectorEntries = serializedObject.FindProperty("sectorEntries");
        logValidationWarnings = serializedObject.FindProperty("logValidationWarnings");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(sourceEngineTelegraphConfig);

        if (sourceEngineTelegraphConfig.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Назначьте Source Engine Telegraph Config, чтобы синхронизировать список секторов.", MessageType.Info);
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
        EditorGUILayout.PropertyField(audioClip);
        EditorGUILayout.PropertyField(transitionTimeSeconds);
        EditorGUILayout.PropertyField(sectorEntries, true);
        EditorGUILayout.PropertyField(logValidationWarnings);

        serializedObject.ApplyModifiedProperties();
    }

    private void SyncFromSource()
    {
        serializedObject.ApplyModifiedProperties();

        SimpleEngineSoundConfig audioConfig = (SimpleEngineSoundConfig)target;
        int syncedCount = audioConfig.SyncFromSource();

        EditorUtility.SetDirty(audioConfig);
        AssetDatabase.SaveAssets();
        serializedObject.Update();

        syncSummary = syncedCount > 0
            ? $"Синхронизировано {syncedCount} записей секторов телеграфа."
            : "Синхронизация пропущена: источник пустой или не назначен.";
    }
}
