// VLiveKit is all Unlicense.
// last update: 2025/05/29

using UnityEditor;
using UnityEngine;
using Cinemachine;   

namespace toshi.VLiveKit.Photography
{
    [ExecuteAlways]
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class DirectorsViewFinderCinemachine : MonoBehaviour
    {
#if UNITY_EDITOR
        public enum Mode { OFF, ON }

        [SerializeField] private Mode currentMode = Mode.ON;
        private CinemachineVirtualCamera vcam;

        private void OnEnable()
        {
            vcam = GetComponent<CinemachineVirtualCamera>();
            EditorApplication.update += UpdateView;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateView;
        }

        private void UpdateView()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || vcam == null || currentMode == Mode.OFF) return;

            TransferSceneToVirtualCamera(vcam, sceneView.camera);
        }

        /// <summary>
        /// シーンビューカメラの Transform と Lens を Virtual Camera にコピー
        /// </summary>
        private static void TransferSceneToVirtualCamera(
            CinemachineVirtualCamera virtualCamera,
            Camera sceneViewCamera)
        {

            virtualCamera.transform.SetPositionAndRotation(
                sceneViewCamera.transform.position,
                sceneViewCamera.transform.rotation);

#if CINEMACHINE_3_0_0_OR_NEWER     
            virtualCamera.Lens = LensSettings.FromCamera(sceneViewCamera);

#else                              
            var lens = virtualCamera.m_Lens;   
            lens.FieldOfView    = sceneViewCamera.fieldOfView;
            lens.NearClipPlane  = sceneViewCamera.nearClipPlane;
            lens.FarClipPlane   = sceneViewCamera.farClipPlane;
            lens.OrthographicSize = sceneViewCamera.orthographicSize; 

            #if !CM_DISABLE_ORTHO_SWITCH
            if (lens.GetType().GetField("ModeOverride") != null)
            {
                // enum CameraMode { Perspective = 0, Orthographic = 1, Physical = 2 }
                var mode = sceneViewCamera.orthographic ? 1 : 0;
                lens.GetType().GetField("ModeOverride").SetValueDirect(__makeref(lens), mode);
            }
            #endif

            virtualCamera.m_Lens = lens;
#endif
        }
#endif
    }
}
