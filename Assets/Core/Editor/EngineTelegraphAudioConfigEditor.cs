using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EngineTelegraphAudioConfig))]
public class EngineTelegraphAudioConfigEditor : Editor
{
    private SerializedProperty sourceEngineTelegraphConfig;
    private SerializedProperty sectorAudioEntries;
    private SerializedProperty defaultConfirmDelay;
    private SerializedProperty defaultSwitchVolume;
    private SerializedProperty defaultConfirmVolume;
    private SerializedProperty logValidationWarnings;

    private string syncSummary;

    private void OnEnable()
    {
        sourceEngineTelegraphConfig = serializedObject.FindProperty("sourceEngineTelegraphConfig");
        sectorAudioEntries = serializedObject.FindProperty("sectorAudioEntries");
        defaultConfirmDelay = serializedObject.FindProperty("defaultConfirmDelay");
        defaultSwitchVolume = serializedObject.FindProperty("defaultSwitchVolume");
        defaultConfirmVolume = serializedObject.FindProperty("defaultConfirmVolume");
        logValidationWarnings = serializedObject.FindProperty("logValidationWarnings");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(sourceEngineTelegraphConfig);

        if (sourceEngineTelegraphConfig.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Assign Source Engine Telegraph Config to enable sync.", MessageType.Info);
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
        EditorGUILayout.PropertyField(sectorAudioEntries, true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Common", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(defaultConfirmDelay);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fallbacks", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(defaultSwitchVolume);
        EditorGUILayout.PropertyField(defaultConfirmVolume);
        EditorGUILayout.PropertyField(logValidationWarnings);

        serializedObject.ApplyModifiedProperties();
    }

    private void SyncFromSource()
    {
        serializedObject.ApplyModifiedProperties();

        EngineTelegraphAudioConfig audioConfig = (EngineTelegraphAudioConfig)target;
        int syncedCount = audioConfig.SyncFromSource();

        EditorUtility.SetDirty(audioConfig);
        AssetDatabase.SaveAssets();
        serializedObject.Update();

        syncSummary = syncedCount > 0
            ? $"Synced {syncedCount} telegraph sector entries."
            : "Sync skipped: source config is empty or not assigned.";
    }
}
