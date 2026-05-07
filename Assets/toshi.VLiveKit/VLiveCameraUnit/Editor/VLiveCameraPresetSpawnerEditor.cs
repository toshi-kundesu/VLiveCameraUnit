using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using toshi.VLiveKit.Photography;

[CustomEditor(typeof(VLiveCameraPresetSpawner))]
public class VLiveCameraPresetSpawnerEditor : Editor
{
    private SerializedProperty presetFolderPathProperty;
    private SerializedProperty presetsProperty;
    private SerializedProperty cameraPresetSlotsProperty;
    private SerializedProperty presetControlModeProperty;
    private SerializedProperty applySlotWhenManualPresetChangesProperty;
    private SerializedProperty shuffleOnStartProperty;
    private SerializedProperty shuffleIntervalSecondsProperty;
    private SerializedProperty firstLocalPositionProperty;
    private SerializedProperty perCameraLocalOffsetProperty;
    private Object presetFolderObject;
    private bool showSpawnLayout;
    private bool showPresetList;

    private void OnEnable()
    {
        presetFolderPathProperty = serializedObject.FindProperty("presetFolderPath");
        presetsProperty = serializedObject.FindProperty("presets");
        cameraPresetSlotsProperty = serializedObject.FindProperty("cameraPresetSlots");
        presetControlModeProperty = serializedObject.FindProperty("presetControlMode");
        applySlotWhenManualPresetChangesProperty = serializedObject.FindProperty("applySlotWhenManualPresetChanges");
        shuffleOnStartProperty = serializedObject.FindProperty("shuffleOnStart");
        shuffleIntervalSecondsProperty = serializedObject.FindProperty("shuffleIntervalSeconds");
        firstLocalPositionProperty = serializedObject.FindProperty("firstLocalPosition");
        perCameraLocalOffsetProperty = serializedObject.FindProperty("perCameraLocalOffset");
        presetFolderObject = ResolveFolderObject(presetFolderPathProperty.stringValue);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EnsureSlotsForInspector();

        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "presetFolderPath",
            "presets",
            "cameraPresetSlots",
            "presetControlMode",
            "applySlotWhenManualPresetChanges",
            "shuffleOnStart",
            "shuffleIntervalSeconds",
            "firstLocalPosition",
            "perCameraLocalOffset");

        showSpawnLayout = EditorGUILayout.Foldout(showSpawnLayout, "Spawn Layout", true);
        if (showSpawnLayout)
        {
            EditorGUILayout.PropertyField(firstLocalPositionProperty, new GUIContent("First Local Position"));
            EditorGUILayout.PropertyField(perCameraLocalOffsetProperty, new GUIContent("Per Camera Offset"));
        }

        EditorGUILayout.Space();
        DrawPresetControlMode();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preset Folder", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        presetFolderObject = EditorGUILayout.ObjectField("Folder", presetFolderObject, typeof(DefaultAsset), false);
        if (EditorGUI.EndChangeCheck())
        {
            string path = AssetDatabase.GetAssetPath(presetFolderObject);
            presetFolderPathProperty.stringValue = AssetDatabase.IsValidFolder(path) ? path : string.Empty;
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("Path", presetFolderPathProperty.stringValue);
        }

        EditorGUILayout.LabelField("Loaded Presets", presetsProperty.arraySize.ToString());
        DrawPresetRefreshHelp();

        EditorGUILayout.Space();
        DrawCameraPresetSlots();

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh Presets"))
            {
                RefreshPresetList();
            }

            if (GUILayout.Button("Generate Cameras"))
            {
                GenerateCameras();
            }
        }

        if (GUILayout.Button("Assign Runtime References"))
        {
            AssignRuntimeReferences();
        }

        if (GUILayout.Button("Apply Camera Preset Slots"))
        {
            ApplyCameraPresetSlots();
        }

        if (GUILayout.Button("Shuffle And Apply Camera Preset Slots"))
        {
            ShuffleAndApplyCameraPresetSlots();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPresetControlMode()
    {
        EditorGUILayout.LabelField("Preset Control", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.PropertyField(presetControlModeProperty, new GUIContent("Mode"));

            VLiveCameraPresetSpawner.PresetControlMode mode =
                (VLiveCameraPresetSpawner.PresetControlMode)presetControlModeProperty.enumValueIndex;

            if (mode == VLiveCameraPresetSpawner.PresetControlMode.Manual)
            {
                EditorGUILayout.PropertyField(
                    applySlotWhenManualPresetChangesProperty,
                    new GUIContent("Apply On Manual Preset Change"));
                EditorGUILayout.PropertyField(shuffleOnStartProperty, new GUIContent("Shuffle Once On Start"));
                EditorGUILayout.HelpBox(
                    "Manual mode applies the selected preset to that slot camera when the dropdown changes. Start shuffle is a one-shot random assignment.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.PropertyField(shuffleOnStartProperty, new GUIContent("Shuffle On Start"));
                EditorGUILayout.PropertyField(shuffleIntervalSecondsProperty, new GUIContent("Shuffle Interval Seconds"));
                EditorGUILayout.HelpBox(
                    "Auto Shuffle mode randomizes slot presets at runtime on the configured interval.",
                    MessageType.None);
            }
        }
    }

    private void DrawPresetRefreshHelp()
    {
        if (presetsProperty.arraySize > 0)
            return;

        EditorGUILayout.HelpBox(
            "Preset list is empty. Refresh Presets or press Generate/Apply to scan the folder automatically.",
            MessageType.Info);
    }

    private void DrawCameraPresetSlots()
    {
        EditorGUILayout.LabelField("Camera Preset Slots", EditorStyles.boldLabel);

        if (cameraPresetSlotsProperty.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No camera slots. They will be created automatically.", MessageType.Info);
            return;
        }

        string[] presetNames = BuildPresetPopupNames();
        int pendingManualApplySlotIndex = -1;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            for (int i = 0; i < cameraPresetSlotsProperty.arraySize; i++)
            {
                SerializedProperty slotProperty = cameraPresetSlotsProperty.GetArrayElementAtIndex(i);
                SerializedProperty cameraNameProperty = slotProperty.FindPropertyRelative("cameraName");
                SerializedProperty presetProperty = slotProperty.FindPropertyRelative("preset");
                SerializedProperty cameraManProperty = slotProperty.FindPropertyRelative("cameraMan");

                using (new EditorGUILayout.HorizontalScope())
                {
                    cameraNameProperty.stringValue = EditorGUILayout.TextField(
                        cameraNameProperty.stringValue,
                        GUILayout.Width(80));

                    int selectedIndex = FindPresetPopupIndex((VLiveCameraPreset)presetProperty.objectReferenceValue);
                    int nextIndex = EditorGUILayout.Popup(selectedIndex, presetNames);
                    if (nextIndex != selectedIndex)
                    {
                        presetProperty.objectReferenceValue = ResolvePresetFromPopupIndex(nextIndex);
                        pendingManualApplySlotIndex = i;
                    }

                    cameraManProperty.objectReferenceValue = EditorGUILayout.ObjectField(
                        cameraManProperty.objectReferenceValue,
                        typeof(VLiveCamera),
                        true,
                        GUILayout.Width(180));

                    if (GUILayout.Button("Ping", GUILayout.Width(44)))
                    {
                        PingObject(cameraManProperty.objectReferenceValue ?? presetProperty.objectReferenceValue);
                    }
                }
            }
        }

        if (pendingManualApplySlotIndex >= 0)
        {
            ApplyManualSlotChangeIfNeeded(pendingManualApplySlotIndex);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Auto Fill cam01-cam08 + Apply"))
            {
                AutoFillSlotNames();
                ApplyCameraPresetSlots();
            }

            if (GUILayout.Button("Clear Slot Cameras"))
            {
                ClearSlotCameraReferences();
            }
        }

        showPresetList = EditorGUILayout.Foldout(showPresetList, "Raw Preset List", true);
        if (showPresetList)
        {
            EditorGUILayout.PropertyField(presetsProperty, true);
        }
    }

    private void RefreshPresetList()
    {
        serializedObject.ApplyModifiedProperties();

        VLiveCameraPresetSpawner spawner = (VLiveCameraPresetSpawner)target;
        Undo.RecordObject(spawner, "Refresh VLive Camera Presets");

        int count = RefreshPresetList(spawner);
        Debug.Log($"[VLiveCameraPresetSpawner] Presets refreshed: {count}", spawner);

        serializedObject.Update();
    }

    private int RefreshPresetList(VLiveCameraPresetSpawner spawner)
    {
        spawner.Presets.Clear();

        foreach (VLiveCameraPreset preset in LoadPresetsFromFolder(spawner.PresetFolderPath))
        {
            spawner.Presets.Add(preset);
        }

        EditorUtility.SetDirty(spawner);
        return spawner.Presets.Count;
    }

    private void GenerateCameras()
    {
        serializedObject.ApplyModifiedProperties();

        VLiveCameraPresetSpawner spawner = (VLiveCameraPresetSpawner)target;
        Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Generate VLive Cameras");
        int presetCount = RefreshPresetList(spawner);
        if (presetCount == 0)
        {
            Debug.LogWarning($"[VLiveCameraPresetSpawner] No VLiveCameraPreset assets found in {spawner.PresetFolderPath}", spawner);
            serializedObject.Update();
            return;
        }

        spawner.GenerateCamerasFromPresets();
        EditorUtility.SetDirty(spawner);
        serializedObject.Update();
    }

    private void AssignRuntimeReferences()
    {
        serializedObject.ApplyModifiedProperties();

        VLiveCameraPresetSpawner spawner = (VLiveCameraPresetSpawner)target;
        Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Assign VLive Camera Runtime References");
        spawner.AssignRuntimeReferences();
        EditorUtility.SetDirty(spawner);
        serializedObject.Update();
    }

    private void ApplyCameraPresetSlots()
    {
        serializedObject.ApplyModifiedProperties();

        VLiveCameraPresetSpawner spawner = (VLiveCameraPresetSpawner)target;
        RefreshPresetList(spawner);
        Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Apply VLive Camera Preset Slots");
        RecordSlotCamerasUndo("Apply VLive Camera Preset Slots");
        spawner.ApplyCameraPresetSlots();
        EditorUtility.SetDirty(spawner);
        serializedObject.Update();
    }

    private void ShuffleAndApplyCameraPresetSlots()
    {
        serializedObject.ApplyModifiedProperties();

        VLiveCameraPresetSpawner spawner = (VLiveCameraPresetSpawner)target;
        RefreshPresetList(spawner);
        Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Shuffle VLive Camera Preset Slots");
        RecordSlotCamerasUndo("Shuffle VLive Camera Preset Slots");
        spawner.ShuffleAndApplyCameraPresetSlots();
        EditorUtility.SetDirty(spawner);
        serializedObject.Update();
    }

    private void ApplyManualSlotChangeIfNeeded(int slotIndex)
    {
        VLiveCameraPresetSpawner.PresetControlMode mode =
            (VLiveCameraPresetSpawner.PresetControlMode)presetControlModeProperty.enumValueIndex;

        if (mode != VLiveCameraPresetSpawner.PresetControlMode.Manual ||
            !applySlotWhenManualPresetChangesProperty.boolValue)
        {
            return;
        }

        serializedObject.ApplyModifiedProperties();

        VLiveCameraPresetSpawner spawner = (VLiveCameraPresetSpawner)target;
        Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Apply VLive Camera Preset Slot");
        RecordSlotCamerasUndo("Apply VLive Camera Preset Slot");
        spawner.ApplyCameraPresetSlot(slotIndex);
        EditorUtility.SetDirty(spawner);
        serializedObject.Update();
    }

    private void RecordSlotCamerasUndo(string undoName)
    {
        for (int i = 0; i < cameraPresetSlotsProperty.arraySize; i++)
        {
            SerializedProperty slotProperty = cameraPresetSlotsProperty.GetArrayElementAtIndex(i);
            SerializedProperty cameraManProperty = slotProperty.FindPropertyRelative("cameraMan");
            VLiveCamera cameraMan = cameraManProperty.objectReferenceValue as VLiveCamera;
            if (cameraMan == null)
                continue;

            Undo.RecordObject(cameraMan, undoName);
            Undo.RecordObject(cameraMan.gameObject, undoName);
        }
    }

    private void EnsureSlotsForInspector()
    {
        VLiveCameraPresetSpawner spawner = (VLiveCameraPresetSpawner)target;
        if (spawner.EnsureDefaultCameraPresetSlots())
        {
            EditorUtility.SetDirty(spawner);
            serializedObject.Update();
        }
    }

    private string[] BuildPresetPopupNames()
    {
        List<string> names = new List<string> { "(None)" };

        VLiveCameraPresetSpawner spawner = (VLiveCameraPresetSpawner)target;
        for (int i = 0; i < spawner.Presets.Count; i++)
        {
            VLiveCameraPreset preset = spawner.Presets[i];
            names.Add(preset != null ? preset.name : "(Missing)");
        }

        return names.ToArray();
    }

    private int FindPresetPopupIndex(VLiveCameraPreset preset)
    {
        if (preset == null)
            return 0;

        VLiveCameraPresetSpawner spawner = (VLiveCameraPresetSpawner)target;
        for (int i = 0; i < spawner.Presets.Count; i++)
        {
            if (spawner.Presets[i] == preset)
            {
                return i + 1;
            }
        }

        return 0;
    }

    private VLiveCameraPreset ResolvePresetFromPopupIndex(int popupIndex)
    {
        if (popupIndex <= 0)
            return null;

        VLiveCameraPresetSpawner spawner = (VLiveCameraPresetSpawner)target;
        int presetIndex = popupIndex - 1;
        return presetIndex >= 0 && presetIndex < spawner.Presets.Count
            ? spawner.Presets[presetIndex]
            : null;
    }

    private void AutoFillSlotNames()
    {
        Undo.RecordObject(target, "Auto Fill VLive Camera Slot Names");

        for (int i = 0; i < cameraPresetSlotsProperty.arraySize; i++)
        {
            SerializedProperty slotProperty = cameraPresetSlotsProperty.GetArrayElementAtIndex(i);
            SerializedProperty cameraNameProperty = slotProperty.FindPropertyRelative("cameraName");
            cameraNameProperty.stringValue = $"cam{i + 1:00}";
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private void ClearSlotCameraReferences()
    {
        Undo.RecordObject(target, "Clear VLive Camera Slot References");

        for (int i = 0; i < cameraPresetSlotsProperty.arraySize; i++)
        {
            SerializedProperty slotProperty = cameraPresetSlotsProperty.GetArrayElementAtIndex(i);
            SerializedProperty cameraManProperty = slotProperty.FindPropertyRelative("cameraMan");
            cameraManProperty.objectReferenceValue = null;
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private static void PingObject(Object targetObject)
    {
        if (targetObject == null)
            return;

        EditorGUIUtility.PingObject(targetObject);
        Selection.activeObject = targetObject;
    }

    private static IReadOnlyList<VLiveCameraPreset> LoadPresetsFromFolder(string folderPath)
    {
        List<VLiveCameraPreset> presets = new List<VLiveCameraPreset>();

        if (string.IsNullOrWhiteSpace(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
            return presets;

        string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { folderPath });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            VLiveCameraPreset preset = AssetDatabase.LoadAssetAtPath<VLiveCameraPreset>(path);
            if (preset != null)
            {
                presets.Add(preset);
            }
        }

        presets.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        return presets;
    }

    private static Object ResolveFolderObject(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return null;

        return AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);
    }
}
