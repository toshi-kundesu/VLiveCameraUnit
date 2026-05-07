#if UNITY_EDITOR
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

            EditorGUILayout.Space(6f);

            VLiveCamera camera = (VLiveCamera)target;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create This Preset", GUILayout.Height(28f)))
                {
                    Undo.RecordObject(camera, "Create VLiveCamera Preset");

                    camera.CreatePresetFromThisCamera();

                    EditorUtility.SetDirty(camera);
                    serializedObject.Update();
                }

                using (new EditorGUI.DisabledScope(preset.objectReferenceValue == null))
                {
                    if (GUILayout.Button("Capture To Preset", GUILayout.Height(28f)))
                    {
                        camera.CaptureToPreset();

                        EditorUtility.SetDirty(camera);
                        serializedObject.Update();
                    }
                }
            }

            EditorGUILayout.Space(4f);

            if (GUILayout.Button("Create Presets For All VLiveCameras In Scene", GUILayout.Height(30f)))
            {
                VLiveCamera.CreatePresetsForAllSceneCameras();

                serializedObject.Update();
            }

            using (new EditorGUI.DisabledScope(preset.objectReferenceValue == null))
            {
                if (GUILayout.Button("Apply Preset", GUILayout.Height(24f)))
                {
                    Undo.RecordObject(camera, "Apply VLiveCamera Preset");

                    camera.ApplyPreset();

                    EditorUtility.SetDirty(camera);
                    serializedObject.Update();
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}
#endif
