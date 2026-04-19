#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using toshi.VLiveKit.Photography;

namespace toshi.VLiveKit.Photography.Editor
{
    [CustomEditor(typeof(VLiveCamera))]
    public partial class VLiveCameraEditor : UnityEditor.Editor
    {
        private bool showPreset = true;
        private bool showSharedReferences = true;
        private bool showStageCamera = true;
        private bool showLookTarget = true;
        private bool showFollowTarget = true;
        private bool showBreathingZoom = false;
        private bool showRigDrift = false;
        private bool showAccentZoom = false;
        private bool showDollyOffset = false;
        private bool showMonitor = true;

        private void OnEnable()
        {
            CacheSerializedProperties();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EnsureStyles();

            DrawHeader();

            showPreset = EditorGUILayout.Foldout(showPreset, "Preset", true, foldoutStyle);
            if (showPreset) DrawPresetSection();

            showSharedReferences = EditorGUILayout.Foldout(showSharedReferences, "Shared References", true, foldoutStyle);
            if (showSharedReferences) DrawSharedReferencesSection();

            showStageCamera = EditorGUILayout.Foldout(showStageCamera, "Stage Camera", true, foldoutStyle);
            if (showStageCamera) DrawStageCameraSection();

            showLookTarget = DrawModuleHeader(showLookTarget, "Look Target", enableLookTargetModule);
            if (showLookTarget) DrawLookTargetSectionBody();

            showFollowTarget = DrawModuleHeader(showFollowTarget, "Follow Target", enableFollowTargetModule);
            if (showFollowTarget) DrawFollowTargetSectionBody();

            showBreathingZoom = DrawModuleHeader(showBreathingZoom, "Breathing Zoom", enableBreathingZoomModule);
            if (showBreathingZoom) DrawBreathingZoomSectionBody();

            showRigDrift = DrawModuleHeader(showRigDrift, "Rig Drift", enableRigDriftModule);
            if (showRigDrift) DrawRigDriftSectionBody();

            showAccentZoom = DrawModuleHeader(showAccentZoom, "Accent Zoom", enableAccentZoomModule);
            if (showAccentZoom) DrawAccentZoomSectionBody();

            showDollyOffset = DrawModuleHeader(showDollyOffset, "Dolly Offset", enableDollyBodyOffsetModule);
            if (showDollyOffset) DrawDollyOffsetSectionBody();

            showMonitor = EditorGUILayout.Foldout(showMonitor, "Monitor", true, foldoutStyle);
            if (showMonitor) DrawMonitorSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 46f);
            EditorGUI.DrawRect(rect, HeaderBg);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), HeaderAccent);

            GUI.Label(new Rect(rect.x + 12f, rect.y + 7f, rect.width - 24f, 18f), "VLive Camera Console", headerStyle);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 24f, rect.width - 24f, 14f), "preset first / fast setup / visible state", subHeaderStyle);
        }
    }
}
#endif