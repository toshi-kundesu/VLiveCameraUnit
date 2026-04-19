using UnityEngine;

namespace UnityTemplateProjects
{
    public class SimpleCameraController : MonoBehaviour
    {
        // 制御をワールドで行うかローカルで行うかのフラグ
        [SerializeField]
        private bool isWorldControl = true;
        // 角度だけを動かすフラグ
        [SerializeField]
        private bool isRotateOnly = false;

        [SerializeField]
        private bool isScrollZoom = false;

        class CameraState
        {
            private SimpleCameraController controller;

            public CameraState(SimpleCameraController controller)
            {
                this.controller = controller;
            }

            public float yaw;
            public float pitch;
            public float roll;
            public float x;
            public float y;
            public float z;

            public void SetFromTransform(Transform t)
            {
                pitch = t.localEulerAngles.x;
                yaw = t.localEulerAngles.y;
                roll = t.localEulerAngles.z;
                x = t.localPosition.x;
                y = t.localPosition.y;
                z = t.localPosition.z;
            }

            public void Translate(Vector3 translation)
            {
                if (controller.isRotateOnly)
                {
                    return;
                }

                if (controller.isWorldControl)
                {
                    Vector3 rotatedTranslation = Quaternion.Euler(pitch, yaw, roll) * translation;
                    x += rotatedTranslation.x;
                    y += rotatedTranslation.y;
                    z += rotatedTranslation.z;
                }
                else
                {
                    Vector3 rotatedTranslation = Quaternion.identity * translation;
                    x += rotatedTranslation.x;
                    y += rotatedTranslation.y;
                    z += rotatedTranslation.z;
                }
            }

            public void LerpTowards(CameraState target, float positionLerpPct, float rotationLerpPct)
            {
                yaw = Mathf.Lerp(yaw, target.yaw, rotationLerpPct);
                pitch = Mathf.Lerp(pitch, target.pitch, rotationLerpPct);
                roll = Mathf.Lerp(roll, target.roll, rotationLerpPct);
                
                if (!controller.isRotateOnly)
                {
                    x = Mathf.Lerp(x, target.x, positionLerpPct);
                    y = Mathf.Lerp(y, target.y, positionLerpPct);
                    z = Mathf.Lerp(z, target.z, positionLerpPct);
                }
            }

            public void UpdateTransform(Transform t)
{
    if (controller.isWorldControl)
    {
        t.eulerAngles = new Vector3(pitch, yaw, roll);
        if (!controller.isRotateOnly)
        {
            t.position = new Vector3(x, y, z);
        }
    }
    else
    {
        t.localEulerAngles = new Vector3(pitch, yaw, roll);
        if (!controller.isRotateOnly)
        {
            t.localPosition = new Vector3(x, y, z);
        }
    }
}
        }
        
        CameraState m_TargetCameraState;
        CameraState m_InterpolatingCameraState;

        [Header("Movement Settings")]
        [Tooltip("Exponential boost factor on translation, controllable by mouse wheel.")]
        public float boost = 3.5f;

        [Tooltip("Time it takes to interpolate camera position 99% of the way to the target."), Range(0.001f, 1f)]
        public float positionLerpTime = 0.2f;

        [Header("Rotation Settings")]
        [Tooltip("X = Change in mouse position.\nY = Multiplicative factor for camera rotation.")]
        public AnimationCurve mouseSensitivityCurve = new AnimationCurve(new Keyframe(0f, 0.5f, 0f, 5f), new Keyframe(1f, 2.5f, 0f, 0f));

        [Tooltip("Time it takes to interpolate camera rotation 99% of the way to the target."), Range(0.001f, 1f)]
        public float rotationLerpTime = 0.01f;

        [Tooltip("Whether or not to invert our Y axis for mouse input to rotation.")]
        public bool invertY = false;

        [Header("Zoom Settings")]
        [Tooltip("Time it takes to interpolate camera zoom 99% of the way to the target."), Range(0.001f, 1f)]
        public float zoomLerpTime = 0.1f;

        [Tooltip("Scroll sensitivity for zooming.")]
        public float scrollSensitivity = 2.0f;

        private float targetFOV;

        void Awake()
        {
            m_TargetCameraState = new CameraState(this);
            m_InterpolatingCameraState = new CameraState(this);
            Camera camera = GetComponent<Camera>();
            if (camera != null)
            {
                targetFOV = camera.fieldOfView;
            }
        }

        void OnEnable()
        {
            m_TargetCameraState.SetFromTransform(transform);
            m_InterpolatingCameraState.SetFromTransform(transform);
        }

        Vector3 GetInputTranslationDirection()
        {
            Vector3 direction = new Vector3();
            if (Input.GetKey(KeyCode.W))
            {
                direction += Vector3.forward;
            }
            if (Input.GetKey(KeyCode.S))
            {
                direction += Vector3.back;
            }
            if (Input.GetKey(KeyCode.A))
            {
                direction += Vector3.left;
            }
            if (Input.GetKey(KeyCode.D))
            {
                direction += Vector3.right;
            }
            if (Input.GetKey(KeyCode.Q))
            {
                direction += Vector3.down;
            }
            if (Input.GetKey(KeyCode.E))
            {
                direction += Vector3.up;
            }
            return direction;
        }
        
        void Update()
{
    // Exit Sample  
    if (Input.GetKey(KeyCode.Escape))
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; 
        #endif
    }

    // Hide and lock cursor when right mouse button pressed
    if (Input.GetMouseButtonDown(1))
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Unlock and show cursor when right mouse button released
    if (Input.GetMouseButtonUp(1))
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // Rotation
    if (Input.GetMouseButton(1))
    {
        var mouseMovement = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y") * (invertY ? 1 : -1));
        
        var mouseSensitivityFactor = mouseSensitivityCurve.Evaluate(mouseMovement.magnitude);

        m_TargetCameraState.yaw += mouseMovement.x * mouseSensitivityFactor;
        m_TargetCameraState.pitch += mouseMovement.y * mouseSensitivityFactor;
    }
    
    // Translation
    var translation = GetInputTranslationDirection() * Time.deltaTime;

    // Speed up movement when shift key held
    if (Input.GetKey(KeyCode.LeftShift))
    {
        translation *= 10.0f;
    }
    
    // Modify movement by a boost factor (defined in Inspector and modified in play mode through the mouse scroll wheel)
    boost += Input.mouseScrollDelta.y * 0.2f;
    translation *= Mathf.Pow(2.0f, boost);

    m_TargetCameraState.Translate(translation);

    // Zoom
    if (isScrollZoom && Input.mousePresent)
    {
        float scrollDelta = Input.mouseScrollDelta.y;
        Camera camera = GetComponent<Camera>();
        if (camera != null)
        {
            targetFOV -= scrollDelta * scrollSensitivity; // スクロール感度を適用
            targetFOV = Mathf.Clamp(targetFOV, 15.0f, 90.0f); // FOVの範囲を制限
            float zoomLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / zoomLerpTime) * Time.deltaTime);
            camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, targetFOV, zoomLerpPct);
        }
    }

    // Framerate-independent interpolation
    // Calculate the lerp amount, such that we get 99% of the way to our target in the specified time
    var positionLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / positionLerpTime) * Time.deltaTime);
    var rotationLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / rotationLerpTime) * Time.deltaTime);
    m_InterpolatingCameraState.LerpTowards(m_TargetCameraState, positionLerpPct, rotationLerpPct);

    m_InterpolatingCameraState.UpdateTransform(transform);
}
    }
}