using UnityEngine;
using Cinemachine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace toshi.VLiveKit.Photography
{
    [ExecuteAlways]
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class DirectorsViewFinderCinemachine : MonoBehaviour
    {
        public enum Mode
        {
            OFF,
            SceneViewSync,   // シーンビューを VCam に同期
            GameViewBase     // 実行時にゲームっぽく操作
        }

        [SerializeField]
        private Mode currentMode = Mode.SceneViewSync;

        private CinemachineVirtualCamera vcam;

        // ─────────────────────────────────────
        // レンズ設定（mm ベース）
        // ─────────────────────────────────────

        [Header("=== レンズ設定（mm / センサーサイズ） ===")]
        [SerializeField] private Vector2 defaultSensorSizeMm = new Vector2(36f, 24f);
        [SerializeField] private float minFocalLength = 18f;
        [SerializeField] private float maxFocalLength = 135f;
        [SerializeField] private float focalStepPerScroll = 2f;
        [SerializeField] private float focalSmoothTime = 0.08f;

        // ─────────────────────────────────────
        // GameViewBase モード用の設定
        // ─────────────────────────────────────

        [Header("=== GameViewBase カメラ操作（実行時のみ） ===")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float shiftMultiplier = 3f;
        [SerializeField] private float lookSensitivity = 2f;
        [SerializeField] private bool invertY = false;

        [Header("スムージング設定（GameViewBase）")]
        [SerializeField] private float positionSmoothTime = 0.08f;
        [SerializeField] private float rotationSmoothTime = 0.05f;

        [Header("Dutch（ロール）設定（GameViewBase）")]
        [SerializeField] private float dutchStepPerSecond = 60f;
        [SerializeField] private float dutchMaxAbsAngle = 85f;
        [SerializeField] private float dutchSmoothTime = 0.05f;
        [SerializeField] private bool invertDutchKey = false;

        // GameViewBase 内部状態
        private bool hasInitGameViewState = false;
        private bool isRightMouseLooking  = false;

        private Vector3 targetPosition;
        private Vector3 positionVelocity;

        private float targetYaw;
        private float targetPitch;
        private float targetRoll;

        private float smoothYaw;
        private float smoothPitch;
        private float smoothRoll;

        private float yawVelocity;
        private float pitchVelocity;
        private float rollVelocity;

        private float targetFocalLength;
        private float focalVelocity;
        private float sensorHeightMm;

#if UNITY_EDITOR
        private static bool s_RightMousePressedForSceneView;
#endif

        // ─────────────────────────────────────
        // ライフサイクル
        // ─────────────────────────────────────

        private void Awake()
        {
            EnsureVcam();
        }

        private void OnEnable()
        {
            EnsureVcam();

#if UNITY_EDITOR
            EditorApplication.update += EditorUpdateView;
            SceneView.duringSceneGui += OnSceneGUI;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdateView;
            SceneView.duringSceneGui -= OnSceneGUI;
            s_RightMousePressedForSceneView = false;
#endif
            if (isRightMouseLooking)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            isRightMouseLooking  = false;
            hasInitGameViewState = false;
        }

        private void EnsureVcam()
        {
            if (vcam == null)
                vcam = GetComponent<CinemachineVirtualCamera>();
        }

        // ─────────────────────────────────────
        // GameViewBase 実行時操作
        // ─────────────────────────────────────

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (currentMode != Mode.GameViewBase) return;
            if (vcam == null) return;

            HandleGameViewCameraControl();
        }

        private void HandleGameViewCameraControl()
        {
            var t = vcam.transform;

            // 初期化
            if (!hasInitGameViewState)
            {
                targetPosition = t.position;
                var e = t.rotation.eulerAngles;

                targetYaw   = smoothYaw   = e.y;
                targetPitch = smoothPitch = e.x;
                targetRoll  = smoothRoll  = e.z;

#if CINEMACHINE_3_0_0_OR_NEWER
                var lens = vcam.Lens;
#else
                var lens = vcam.m_Lens;
#endif
                var sensor = lens.SensorSize;
                if (sensor.x <= 0f || sensor.y <= 0f)
                    sensor = defaultSensorSizeMm;

                lens.SensorSize = sensor;
                sensorHeightMm = sensor.y;

                float focal = FovToFocalLength(lens.FieldOfView, sensorHeightMm);
                focal = Mathf.Clamp(focal, minFocalLength, maxFocalLength);
                targetFocalLength = focal;

                lens.FieldOfView = FocalLengthToFov(focal, sensorHeightMm);

#if CINEMACHINE_3_0_0_OR_NEWER
                vcam.Lens = lens;
#else
                vcam.m_Lens = lens;
#endif
                hasInitGameViewState = true;
            }

            // RMB Look
            if (Input.GetMouseButtonDown(1))
            {
                isRightMouseLooking = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (Input.GetMouseButtonUp(1))
            {
                isRightMouseLooking = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (isRightMouseLooking)
            {
                float signY = invertY ? 1f : -1f;
                targetYaw   += Input.GetAxis("Mouse X") * lookSensitivity;
                targetPitch += Input.GetAxis("Mouse Y") * lookSensitivity * signY;
                targetPitch = Mathf.Clamp(targetPitch, -89f, 89f);
            }

            // ─── Dutch（Z / C） ───
            float dutchDir = 0f;
            if (Input.GetKey(KeyCode.Z)) dutchDir += 1f;
            if (Input.GetKey(KeyCode.C)) dutchDir -= 1f;
            if (invertDutchKey) dutchDir = -dutchDir;

            if (Mathf.Abs(dutchDir) > 0f)
            {
                float mul = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    ? shiftMultiplier : 1f;

                targetRoll += dutchDir * dutchStepPerSecond * mul * Time.deltaTime;
                targetRoll = Mathf.Clamp(targetRoll, -dutchMaxAbsAngle, dutchMaxAbsAngle);
            }

            smoothYaw   = Mathf.SmoothDampAngle(smoothYaw,   targetYaw,   ref yawVelocity,   rotationSmoothTime);
            smoothPitch = Mathf.SmoothDampAngle(smoothPitch, targetPitch, ref pitchVelocity, rotationSmoothTime);
            smoothRoll  = Mathf.SmoothDampAngle(smoothRoll,  targetRoll,  ref rollVelocity,  dutchSmoothTime);

            t.rotation = Quaternion.Euler(smoothPitch, smoothYaw, smoothRoll);

            // ─── 移動入力 ───
            Vector3 input = Vector3.zero;
            input += Vector3.forward * Input.GetAxisRaw("Vertical");
            input += Vector3.right   * Input.GetAxisRaw("Horizontal");

            float upDown = 0f;
            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space)) upDown += 1f;
            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl)) upDown -= 1f; // ★C を削除

            input += Vector3.up * upDown;
            if (input.sqrMagnitude > 1f) input.Normalize();

            float speed = moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                speed *= shiftMultiplier;

            targetPosition += t.TransformDirection(input) * speed * Time.deltaTime;
            t.position = Vector3.SmoothDamp(t.position, targetPosition, ref positionVelocity, positionSmoothTime);

            // ─── ズーム（mm） ───
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                targetFocalLength = Mathf.Clamp(
                    targetFocalLength - scroll * focalStepPerScroll,
                    minFocalLength, maxFocalLength);
            }

#if CINEMACHINE_3_0_0_OR_NEWER
            var l = vcam.Lens;
#else
            var l = vcam.m_Lens;
#endif
            float currentFocal = FovToFocalLength(l.FieldOfView, sensorHeightMm);
            float smoothedFocal = Mathf.SmoothDamp(
                currentFocal, targetFocalLength, ref focalVelocity, focalSmoothTime);

            l.FieldOfView = FocalLengthToFov(smoothedFocal, sensorHeightMm);

#if CINEMACHINE_3_0_0_OR_NEWER
            vcam.Lens = l;
#else
            vcam.m_Lens = l;
#endif
        }

#if UNITY_EDITOR
        // ─────────────────────────────────────
        // SceneViewSync
        // ─────────────────────────────────────

        private void EditorUpdateView()
        {
            if (currentMode != Mode.SceneViewSync) return;
            if (vcam == null) return;

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) return;

            AlignSceneViewToVcam(sceneView, vcam);
        }

        private void AlignSceneViewToVcam(SceneView sceneView, CinemachineVirtualCamera virtualCamera)
        {
            var cam = sceneView.camera;
            if (cam == null) return;

            cam.transform.SetPositionAndRotation(
                virtualCamera.transform.position,
                virtualCamera.transform.rotation);

            sceneView.Repaint();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (currentMode != Mode.SceneViewSync) return;

            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 1)
                s_RightMousePressedForSceneView = true;
            else if (e.type == EventType.MouseUp && e.button == 1)
                s_RightMousePressedForSceneView = false;
        }
#endif

        // ─────────────────────────────────────
        // FOV ↔ 焦点距離
        // ─────────────────────────────────────

        private static float FovToFocalLength(float verticalFovDeg, float sensorHeightMm)
        {
            return (sensorHeightMm * 0.5f) /
                   Mathf.Tan(verticalFovDeg * Mathf.Deg2Rad * 0.5f);
        }

        private static float FocalLengthToFov(float focalLengthMm, float sensorHeightMm)
        {
            return 2f * Mathf.Atan((sensorHeightMm * 0.5f) /
                   Mathf.Max(focalLengthMm, 0.0001f)) * Mathf.Rad2Deg;
        }
    }
}
