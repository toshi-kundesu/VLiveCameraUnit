#if UNITY_EDITOR
using UnityEditor;
using Cinemachine;

namespace toshi.VLiveKit.Photography.Editor
{
    public partial class VLiveCameraEditor
    {
        private void DrawMonitorSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            VLiveCamera liveCamera = (VLiveCamera)target;
            CinemachineVirtualCamera vcam = stageVirtualCamera.objectReferenceValue as CinemachineVirtualCamera;

            DrawStateLine("Owner", true, liveCamera.gameObject.name);
            DrawStateLine("Virtual Camera", vcam != null, vcam ? "Bound" : "Missing");

            if (vcam != null)
            {
                DrawStateLine("LookAt", vcam.LookAt != null, vcam.LookAt ? vcam.LookAt.name : "None");
                DrawStateLine("Follow", vcam.Follow != null, vcam.Follow ? vcam.Follow.name : "None");
            }

            DrawStateLine("Look Resolved", lookTargetMarker.objectReferenceValue != null, lookTargetMarker.objectReferenceValue ? lookTargetMarker.objectReferenceValue.name : "Unresolved");
            DrawStateLine("Follow Resolved", followTargetMarker.objectReferenceValue != null, followTargetMarker.objectReferenceValue ? followTargetMarker.objectReferenceValue.name : "Unresolved");

            EditorGUILayout.EndVertical();
        }
    }
}
#endif