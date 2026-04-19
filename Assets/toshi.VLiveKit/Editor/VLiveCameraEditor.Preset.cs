#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace toshi.VLiveKit.Photography.Editor
{
    public partial class VLiveCameraEditor
    {
        private void DrawPresetSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.PropertyField(preset);
            EditorGUILayout.PropertyField(applyPresetOnStart);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = preset.objectReferenceValue != null;
                if (GUILayout.Button("Apply Preset"))
                {
                    CallPublic("ApplyPreset");
                    serializedObject.Update();
                }

                if (GUILayout.Button("Capture To Preset"))
                {
                    CallPublic("CaptureToPreset");
                    serializedObject.Update();
                }

                if (GUILayout.Button("Ping Preset"))
                {
                    EditorGUIUtility.PingObject(preset.objectReferenceValue);
                }
                GUI.enabled = true;
            }

            EditorGUILayout.Space(4f);

            if (GUILayout.Button("Create New Preset"))
            {
                CreateNewPreset();
                serializedObject.Update();
            }

            if (preset.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("まずはカメラをいい感じに作ってから Preset に保存すると強いです。", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void CreateNewPreset()
        {
            string defaultFolder = "Assets/toshi.VLiveKit/Camera/Presets";
            if (!AssetDatabase.IsValidFolder(defaultFolder))
            {
                EnsureFolderRecursive(defaultFolder);
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "Create VLiveCamera Preset",
                "NewVLiveCameraPreset",
                "asset",
                "保存先を選択してください",
                defaultFolder
            );

            if (string.IsNullOrEmpty(path))
                return;

            VLiveCameraPreset newPreset = ScriptableObject.CreateInstance<VLiveCameraPreset>();
            newPreset.presetDisplayName = Path.GetFileNameWithoutExtension(path);

            AssetDatabase.CreateAsset(newPreset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            preset.objectReferenceValue = newPreset;
            serializedObject.ApplyModifiedProperties();

            EditorGUIUtility.PingObject(newPreset);
            Debug.Log($"[VLiveCamera] New Preset Created → {path}");
        }

        private static void EnsureFolderRecursive(string targetFolder)
        {
            string[] parts = targetFolder.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
                return;

            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
#endif