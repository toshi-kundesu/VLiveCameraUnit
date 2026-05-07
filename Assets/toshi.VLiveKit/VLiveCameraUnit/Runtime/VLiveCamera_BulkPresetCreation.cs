using System.IO;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
#endif

namespace toshi.VLiveKit.Photography
{
    public partial class VLiveCamera
    {
#if UNITY_EDITOR
        private const string DefaultBulkPresetFolderPath =
            "Assets/toshi.VLiveKit/VLiveCameraUnit/Presets/SceneCameras";

        [MenuItem("toshi/VLiveKit/Camera/Create Presets For All VLiveCameras In Scene")]
        public static void CreatePresetsForAllSceneCamerasMenu()
        {
            CreatePresetsForAllSceneCameras(DefaultBulkPresetFolderPath);
        }

        public static void CreatePresetsForAllSceneCameras()
        {
            CreatePresetsForAllSceneCameras(DefaultBulkPresetFolderPath);
        }

        public static void CreatePresetsForAllSceneCameras(string folderPath)
        {
            EnsurePresetFolderExists(folderPath);

            VLiveCamera[] cameras = FindAllSceneVLiveCameras();

            if (cameras == null || cameras.Length == 0)
            {
                Debug.LogWarning("[VLiveCamera] シーン内に VLiveCamera が見つかりません。");
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

                baseName = SanitizeAssetFileName(baseName);

                string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                    $"{folderPath}/{baseName}.asset"
                );

                VLiveCameraPreset newPreset = ScriptableObject.CreateInstance<VLiveCameraPreset>();
                newPreset.presetDisplayName = Path.GetFileNameWithoutExtension(assetPath);

                camera.CaptureCurrentValuesToPreset(newPreset);

                AssetDatabase.CreateAsset(newPreset, assetPath);

                camera.preset = newPreset;
                camera.SyncPresetDisplayNameFromAsset();

                EditorUtility.SetDirty(camera);
                EditorUtility.SetDirty(newPreset);

                createdCount++;
            }

            Undo.CollapseUndoOperations(undoGroup);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[VLiveCamera] Scene Presets Created And Captured → {createdCount} cameras / {folderPath}");
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
#endif
    }
}