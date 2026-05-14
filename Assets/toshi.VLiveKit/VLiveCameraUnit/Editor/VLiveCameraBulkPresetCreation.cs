using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using toshi.VLiveKit.Photography;

namespace toshi.VLiveKit.Photography.Editor
{
    public static class VLiveCameraBulkPresetCreation
    {
        private const string DefaultBulkPresetFolderPath =
            "Assets/toshi.VLiveKit/VLiveCameraUnit/Presets/SceneCameras";

        [MenuItem("toshi/VLiveKit/Camera/Create Presets For All VLiveCameras In Scene")]
        public static void CreatePresetsForAllSceneCamerasMenu()
        {
            CreatePresetsForAllSceneCameras(DefaultBulkPresetFolderPath);
        }

        public static void CreatePresetsForAllSceneCameras(string folderPath)
        {
            EnsurePresetFolderExists(folderPath);

            VLiveCamera[] cameras = FindAllSceneVLiveCameras();

            if (cameras == null || cameras.Length == 0)
            {
                Debug.Log("[VLiveCamera] No VLiveCamera components were found in loaded scenes.");
                return;
            }

            int createdCount = 0;

            Undo.SetCurrentGroupName("Create VLiveCamera Presets For Scene");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (VLiveCamera camera in cameras)
            {
                if (camera == null)
                    continue;

                Undo.RecordObject(camera, "Assign VLiveCamera Preset");

                string baseName = camera.gameObject != null
                    ? camera.gameObject.name
                    : nameof(VLiveCamera);

                baseName = SanitizeAssetFileName(ObjectNames.NicifyVariableName(baseName));

                string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                    $"{folderPath}/{baseName}.asset");

                VLiveCameraPreset newPreset = ScriptableObject.CreateInstance<VLiveCameraPreset>();
                newPreset.presetDisplayName = Path.GetFileNameWithoutExtension(assetPath);

                AssetDatabase.CreateAsset(newPreset, assetPath);
                camera.SetPreset(newPreset, false);
                camera.CaptureToPreset();

                EditorUtility.SetDirty(camera);
                EditorUtility.SetDirty(newPreset);

                createdCount++;
            }

            Undo.CollapseUndoOperations(undoGroup);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[VLiveCamera] Scene presets created and captured: {createdCount} cameras / {folderPath}");
        }

        private static VLiveCamera[] FindAllSceneVLiveCameras()
        {
            VLiveCamera[] all = Resources.FindObjectsOfTypeAll<VLiveCamera>();
            List<VLiveCamera> result = new List<VLiveCamera>();

            foreach (VLiveCamera camera in all)
            {
                if (camera == null)
                    continue;

                if (EditorUtility.IsPersistent(camera))
                    continue;

                GameObject go = camera.gameObject;
                if (go == null)
                    continue;

                Scene scene = go.scene;

                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                result.Add(camera);
            }

            result.Sort((a, b) =>
            {
                string pathA = GetHierarchyPath(a != null ? a.transform : null);
                string pathB = GetHierarchyPath(b != null ? b.transform : null);

                return string.CompareOrdinal(pathA, pathB);
            });

            return result.ToArray();
        }

        private static void EnsurePresetFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
                return string.Empty;

            string path = transform.name;
            Transform parent = transform.parent;

            while (parent != null)
            {
                path = $"{parent.name}/{path}";
                parent = parent.parent;
            }

            return path;
        }

        private static string SanitizeAssetFileName(string fileName)
        {
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            fileName = fileName.Trim();

            return string.IsNullOrEmpty(fileName) ? nameof(VLiveCamera) : fileName;
        }
    }
}
