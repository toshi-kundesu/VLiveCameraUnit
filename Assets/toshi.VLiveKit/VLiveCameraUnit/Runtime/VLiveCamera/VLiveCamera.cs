using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;
using Cinemachine;
using toshi.VLiveKit;

namespace toshi.VLiveKit.Photography
{
    [DefaultExecutionOrder(120)]
    public partial class VLiveCamera : MonoBehaviour
    {
        public enum CameraRigSpace
        {
            Global,
            Local
        }

        public enum AccentZoomFilterMode
        {
            None,
            ExponentialLowPass,
            DampedSpring
        }

        public enum CameraMotionSignalMode
        {
            Sin,
            PerlinNoise
        }

        public enum TargetReferenceMode
        {
            HumanoidBone,
            DirectTransform
        }

        private const float Tau = 2f * Mathf.PI;
        private static readonly Vector3 DefaultLookTargetPosition = new Vector3(0f, 1.5f, 0f);

#if UNITY_EDITOR
        private const string DefaultPresetFolderPath = "Assets/toshi.VLiveKit/VLiveCameraUnit/Presets";
#endif

        [Header("▼ Camera Preset")]
        [SerializeField] private VLiveCameraPreset preset;
        [SerializeField] private bool applyPresetOnStart = false;

        [Header("▼ Shared References")]
        [SerializeField] private bool autoBindPlayableDirectorFromTimeTable = true;
        [SerializeField] private string liveTimelineSectionName;
        [SerializeField] private PlayableDirector sharedPlayableDirector;
        [SerializeField] private VLiveLookTargetRig sharedLookTargetRig;

        [Header("▼ Stage Camera")]
        [FormerlySerializedAs("liveVirtualCamera")]
        [SerializeField] private CinemachineVirtualCamera stageVirtualCamera;

        [Header("▼ Look Target Module")]
        [FormerlySerializedAs("enableAimModule")]
        [SerializeField] private bool enableLookTargetModule = true;

        [FormerlySerializedAs("assignAimOnStart")]
        [SerializeField] private bool assignLookTargetOnStart = true;

        [FormerlySerializedAs("performerBoneCollector")]
        [SerializeField] private VLiveLookTargetRig lookTargetRig;

        [SerializeField] private TargetReferenceMode lookTargetMode = TargetReferenceMode.HumanoidBone;

        [FormerlySerializedAs("aimBone")]
        [SerializeField] private HumanBodyBones lookTargetBone = HumanBodyBones.Head;

        [SerializeField] private Transform lookTargetTransform;

        [FormerlySerializedAs("aimTargetGO")]
        [SerializeField] private GameObject lookTargetMarker;

        [SerializeField, HideInInspector] private Transform fallbackLookTargetTransform;

        [Header("▼ Follow Target Module")]
        [FormerlySerializedAs("enableTrackingModule")]
        [SerializeField] private bool enableFollowTargetModule = false;

        [FormerlySerializedAs("assignTrackingOnStart")]
        [SerializeField] private bool assignFollowTargetOnStart = true;

        [FormerlySerializedAs("trackingBoneCollector")]
        [SerializeField] private VLiveLookTargetRig followTargetRig;

        [SerializeField] private TargetReferenceMode followTargetMode = TargetReferenceMode.HumanoidBone;

        [FormerlySerializedAs("trackingBone")]
        [SerializeField] private HumanBodyBones followTargetBone = HumanBodyBones.Hips;

        [SerializeField] private Transform followTargetTransform;

        [FormerlySerializedAs("trackingTargetGO")]
        [SerializeField] private GameObject followTargetMarker;

        [Header("Screen Position Module")]
        [SerializeField] private bool enableScreenPositionModule = false;
        [SerializeField] private PlayableDirector screenPositionDirector;
        [SerializeField] private bool useDirectorTimeForScreenPosition = true;
        [SerializeField] private bool useScreenPositionSinWobble = true;
        [SerializeField] private bool useScreenPositionPerlinWobble = false;
        [SerializeField] private CameraMotionSignalMode screenPositionMotionMode = CameraMotionSignalMode.Sin;
        [SerializeField] private float screenPositionTimeOffset = 0f;
        [SerializeField] private float screenPositionTimeScalePrimary = 1f;
        [SerializeField] private float screenPositionTimeScaleSecondary = 1f;
        [SerializeField] private float screenPositionIntensityScalePrimary = 1f;
        [SerializeField] private float screenPositionIntensityScaleSecondary = 1f;
        [SerializeField] private Vector2 screenPositionBase = new Vector2(0.5f, 0.5f);
        [SerializeField] private Vector2 screenPositionAmplitude = new Vector2(0.05f, 0.05f);
        [SerializeField] private Vector2 screenPositionFrequency = new Vector2(0.25f, 0.35f);
        [SerializeField] private Vector2 screenPositionPhaseDeg = new Vector2(0f, 90f);
        [SerializeField] private Vector2 screenPositionPerlinOffset = new Vector2(0f, 17f);
        [SerializeField] private bool previewScreenPositionInEditMode = true;

        [Header("Dutch Roll Module")]
        [SerializeField] private bool enableDutchRollModule = false;
        [SerializeField] private PlayableDirector dutchRollDirector;
        [SerializeField] private bool useDirectorTimeForDutchRoll = true;
        [SerializeField] private CameraMotionSignalMode dutchRollMotionMode = CameraMotionSignalMode.Sin;
        [SerializeField] private float dutchRollTimeOffset = 0f;
        [SerializeField] private float dutchRollTimeScalePrimary = 1f;
        [SerializeField] private float dutchRollTimeScaleSecondary = 1f;
        [SerializeField] private float dutchRollIntensityScalePrimary = 1f;
        [SerializeField] private float dutchRollIntensityScaleSecondary = 1f;
        [SerializeField] private float dutchRollBaseDegrees = 0f;
        [Min(0f)]
        [SerializeField] private float dutchRollAmplitudeDegrees = 5f;
        [Min(0f)]
        [SerializeField] private float dutchRollFrequency = 0.25f;
        [SerializeField] private float dutchRollPhaseDeg = 0f;
        [SerializeField] private float dutchRollPerlinOffset = 0f;
        [SerializeField] private bool previewDutchRollInEditMode = true;

        [Header("▼ Breathing Zoom Module")]
        [FormerlySerializedAs("enableZoomSineModule")]
        [SerializeField] private bool enableBreathingZoomModule = false;

        [FormerlySerializedAs("zoomCueDirector")]
        [SerializeField] private PlayableDirector breathingZoomDirector;

        [FormerlySerializedAs("useCueTimeForZoom")]
        [SerializeField] private bool useDirectorTimeForBreathingZoom = true;

        [FormerlySerializedAs("zoomCueTimeOffset")]
        [SerializeField] private float breathingZoomTimeOffset = 0f;

        [SerializeField] private CameraMotionSignalMode breathingZoomMotionMode = CameraMotionSignalMode.Sin;

        [FormerlySerializedAs("zoomTimeScalePrimary")]
        [SerializeField] private float breathingZoomTimeScalePrimary = 1f;

        [FormerlySerializedAs("zoomTimeScaleSecondary")]
        [SerializeField] private float breathingZoomTimeScaleSecondary = 1f;

        [FormerlySerializedAs("zoomFovMin")]
        [Min(1f)]
        [SerializeField] private float breathingZoomFovMin = 30f;

        [FormerlySerializedAs("zoomFovMax")]
        [Min(1f)]
        [SerializeField] private float breathingZoomFovMax = 60f;

        [FormerlySerializedAs("zoomFrequencyHz")]
        [SerializeField] private float breathingZoomFrequencyHz = 1f;

        [SerializeField] private float breathingZoomPerlinOffset = 0f;

        [Header("▼ Camera Rig Drift Module")]
        [FormerlySerializedAs("enableRigSwayModule")]
        [SerializeField] private bool enableRigDriftModule = false;

        [FormerlySerializedAs("swayRigTarget")]
        [SerializeField] private Transform driftRigTarget;

        [FormerlySerializedAs("swaySpace")]
        [SerializeField] private CameraRigSpace driftSpace = CameraRigSpace.Global;

        [FormerlySerializedAs("syncRigSwayToCue")]
        [SerializeField] private bool syncRigDriftToDirector = false;

        [FormerlySerializedAs("rigSwayDirector")]
        [SerializeField] private PlayableDirector rigDriftDirector;

        [FormerlySerializedAs("rigSwayTimeScalePrimary")]
        [SerializeField] private float rigDriftTimeScalePrimary = 1f;

        [FormerlySerializedAs("rigSwayTimeScaleSecondary")]
        [SerializeField] private float rigDriftTimeScaleSecondary = 1f;

        [SerializeField] private CameraMotionSignalMode rigDriftMotionMode = CameraMotionSignalMode.Sin;

        [FormerlySerializedAs("swayAxisWeight")]
        [SerializeField] private Vector3 driftAxisWeight = Vector3.one;

        [FormerlySerializedAs("swayFrequency")]
        [SerializeField] private Vector3 driftFrequency = Vector3.one;

        [FormerlySerializedAs("swayAmplitude")]
        [SerializeField] private Vector3 driftAmplitude = Vector3.one;

        [FormerlySerializedAs("swayPhaseOffset")]
        [SerializeField] private Vector3 driftPhaseOffset = Vector3.zero;

        [SerializeField] private Vector3 driftPerlinOffset = new Vector3(0f, 17f, 31f);

        [FormerlySerializedAs("swayRangeMin")]
        [SerializeField] private Vector3 driftRangeMin = new Vector3(-1f, -1f, -1f);

        [FormerlySerializedAs("swayRangeMax")]
        [SerializeField] private Vector3 driftRangeMax = new Vector3(1f, 1f, 1f);

        [FormerlySerializedAs("rigOffset")]
        [SerializeField] private Vector3 rigDriftOffset = Vector3.zero;

        [FormerlySerializedAs("enableFigureEightPattern")]
        [SerializeField] private bool useFigureEightDrift = false;

        [FormerlySerializedAs("rigSwayTime")]
        [SerializeField, HideInInspector] private float rigDriftTime = 0f;

        [Header("▼ Accent Zoom Module")]
        [FormerlySerializedAs("enablePunchZoomModule")]
        [SerializeField] private bool enableAccentZoomModule = false;

        [FormerlySerializedAs("punchZoomDirector")]
        [SerializeField] private PlayableDirector accentZoomDirector;

        [FormerlySerializedAs("punchZoomFovMin")]
        [Min(1f)]
        [SerializeField] private float accentZoomFovMin = 30f;

        [FormerlySerializedAs("punchZoomFovMax")]
        [Min(1f)]
        [SerializeField] private float accentZoomFovMax = 60f;

        [FormerlySerializedAs("punchZoomBaseFov")]
        [SerializeField] private float accentZoomBaseFov = 0f;

        [FormerlySerializedAs("punchZoomAmountMin")]
        [SerializeField] private float accentZoomAmountMin = -6f;

        [FormerlySerializedAs("punchZoomAmountMax")]
        [SerializeField] private float accentZoomAmountMax = -18f;

        [FormerlySerializedAs("punchZoomSeed")]
        [SerializeField] private int accentZoomSeed = 20250926;

        [FormerlySerializedAs("punchAttack")]
        [Min(0.005f)]
        [SerializeField] private float accentAttack = 0.06f;

        [FormerlySerializedAs("punchAttackSharpness")]
        [Range(2f, 16f)]
        [SerializeField] private float accentAttackSharpness = 8f;

        [FormerlySerializedAs("punchHoldZoom")]
        [Min(0f)]
        [SerializeField] private float accentHoldZoom = 0.15f;

        [FormerlySerializedAs("punchReturnDuration")]
        [Min(0.01f)]
        [SerializeField] private float accentReturnDuration = 0.30f;

        [FormerlySerializedAs("punchReturnEpsilon")]
        [Range(1e-4f, 0.1f)]
        [SerializeField] private float accentReturnEpsilon = 0.02f;

        [FormerlySerializedAs("punchHoldBase")]
        [Min(0f)]
        [SerializeField] private float accentHoldBase = 0.25f;

        [FormerlySerializedAs("punchStartOffset")]
        [SerializeField] private float accentStartOffset = 0f;

        [FormerlySerializedAs("freezePunchZoomWhenCueStopped")]
        [SerializeField] private bool freezeAccentZoomWhenDirectorStopped = false;

        [FormerlySerializedAs("punchZoomTimeScalePrimary")]
        [SerializeField] private float accentZoomTimeScalePrimary = 1f;

        [FormerlySerializedAs("punchZoomTimeScaleSecondary")]
        [SerializeField] private float accentZoomTimeScaleSecondary = 1f;

        [FormerlySerializedAs("punchZoomFilterMode")]
        [SerializeField] private AccentZoomFilterMode accentZoomFilterMode = AccentZoomFilterMode.DampedSpring;

        [FormerlySerializedAs("punchLowPassTimeConstant")]
        [Min(0.001f)]
        [SerializeField] private float accentLowPassTimeConstant = 0.12f;

        [FormerlySerializedAs("punchSpringFrequency")]
        [Min(0.01f)]
        [SerializeField] private float accentSpringFrequency = 2.0f;

        [FormerlySerializedAs("punchSpringDampingRatio")]
        [Range(0.1f, 2.0f)]
        [SerializeField] private float accentSpringDampingRatio = 1.0f;

        [FormerlySerializedAs("resetPunchOnLargeJump")]
        [SerializeField] private bool resetAccentOnLargeJump = true;

        [FormerlySerializedAs("punchJumpThreshold")]
        [Min(0.01f)]
        [SerializeField] private float accentJumpThreshold = 0.5f;

        [FormerlySerializedAs("punchZoomOutFov")]
        [SerializeField, HideInInspector] private float accentZoomOutputFov;

        [FormerlySerializedAs("punchZoomVelocityFov")]
        [SerializeField, HideInInspector] private float accentZoomVelocityFov;

        [FormerlySerializedAs("punchZoomPrevEvalTime")]
        [SerializeField, HideInInspector] private double accentZoomPreviousEvalTime = double.NaN;

        [FormerlySerializedAs("punchZoomResolvedBaseFov")]
        [SerializeField, HideInInspector] private float accentZoomResolvedBaseFov;

        [FormerlySerializedAs("punchZoomCycleLength")]
        [SerializeField, HideInInspector] private float accentZoomCycleLength;

        [Header("▼ Dolly Body Offset Module")]
        [FormerlySerializedAs("enableBodyOffsetSineModule")]
        [SerializeField] private bool enableDollyBodyOffsetModule = false;

        [FormerlySerializedAs("bodyOffsetTimeScalePrimary")]
        [SerializeField] private float dollyBodyOffsetTimeScalePrimary = 1f;

        [FormerlySerializedAs("bodyOffsetTimeScaleSecondary")]
        [SerializeField] private float dollyBodyOffsetTimeScaleSecondary = 1f;

        [FormerlySerializedAs("bodyOffsetDirector")]
        [SerializeField] private PlayableDirector dollyBodyOffsetDirector;

        [FormerlySerializedAs("bodyOffsetBase")]
        [SerializeField] private Vector3 dollyBodyOffsetBase = new Vector3(0f, 2f, -4f);

        [FormerlySerializedAs("bodyOffsetAmplitude")]
        [SerializeField] private Vector3 dollyBodyOffsetAmplitude = new Vector3(0.1f, 0.1f, 0.1f);

        [FormerlySerializedAs("bodyOffsetFrequency")]
        [SerializeField] private Vector3 dollyBodyOffsetFrequency = new Vector3(0.25f, 0.35f, 0.45f);

        [FormerlySerializedAs("bodyOffsetPhaseDeg")]
        [SerializeField] private Vector3 dollyBodyOffsetPhaseDeg = new Vector3(0f, 90f, 180f);

        [SerializeField] private CameraMotionSignalMode dollyBodyOffsetMotionMode = CameraMotionSignalMode.Sin;

        [SerializeField] private Vector3 dollyBodyOffsetPerlinOffset = new Vector3(0f, 17f, 31f);

        [FormerlySerializedAs("previewBodyOffsetInEditMode")]
        [SerializeField] private bool previewDollyOffsetInEditMode = true;

        [FormerlySerializedAs("bodyOffsetInitialValue")]
        [SerializeField, HideInInspector] private Vector3 dollyBodyOffsetInitialValue;

        [FormerlySerializedAs("bodyOffsetInitialized")]
        [SerializeField, HideInInspector] private bool dollyBodyOffsetInitialized = false;

#if UNITY_EDITOR
        [Header("▼ Debug")]
        [FormerlySerializedAs("zoomEvalTimeDebug")]
        [SerializeField] private float breathingZoomEvaluatedTimeDebug;
        [SerializeField] private float screenPositionEvaluatedTimeDebug;
        [SerializeField] private Vector2 screenPositionOutputDebug;
        [SerializeField] private float dutchRollEvaluatedTimeDebug;
        [SerializeField] private float dutchRollOutputDegreesDebug;
#endif

        private Transform resolvedDriftRigTarget;
        private CinemachineTransposer cachedBodyTransposer;
        private CinemachineComposer cachedComposer;
        private CinemachineFramingTransposer cachedFramingTransposer;
        private VLiveTimeTable cachedTimeTable;
        private bool previousEnableLookTargetModule;
        private bool previousEnableFollowTargetModule;
        private bool previousEnableScreenPositionModule;
        private bool previousEnableDutchRollModule;
        private bool previousEnableBreathingZoomModule;
        private bool previousEnableRigDriftModule;
        private bool previousEnableAccentZoomModule;
        private bool previousEnableDollyBodyOffsetModule;
        private bool lookTargetBeforeModuleCaptured;
        private bool followTargetBeforeModuleCaptured;
        private Transform lookTargetBeforeModule;
        private Transform followTargetBeforeModule;
        private bool fovBeforeModulesCaptured;
        private float fovBeforeModules;
        private bool rigDriftPoseBeforeModuleCaptured;
        private Vector3 rigDriftPositionBeforeModule;
        private Vector3 rigDriftLocalPositionBeforeModule;
        private CameraRigSpace rigDriftSpaceBeforeModule;

        public VLiveCameraPreset Preset => preset;

        private void Awake()
        {
            ResolveStageCameraReference();

            if (stageVirtualCamera == null)
            {
                Debug.LogError("[VLiveCamera] CinemachineVirtualCamera が見つかりません。", this);
                enabled = false;
                return;
            }

            ResolveRigDriftTarget();
            CacheBodyDriverComponents();
            AutoBindPlayableDirectorFromTimeTableIfNeeded();

            if (!dollyBodyOffsetDirector && sharedPlayableDirector == null)
                dollyBodyOffsetDirector = GetComponentInParent<PlayableDirector>();
        }

        private void Start()
        {
            if (applyPresetOnStart && preset != null)
            {
                ApplyPreset();
            }

            if (enableLookTargetModule && assignLookTargetOnStart)
            {
                ActivateLookTargetModule();
            }

            if (enableFollowTargetModule && assignFollowTargetOnStart)
            {
                ActivateFollowTargetModule();
            }

            ResolveRigDriftTarget();
            CacheBodyDriverComponents();
            AutoBindPlayableDirectorFromTimeTableIfNeeded();

            if (!dollyBodyOffsetDirector && sharedPlayableDirector == null)
                dollyBodyOffsetDirector = GetComponentInParent<PlayableDirector>();

            if (!dollyBodyOffsetInitialized)
                InitializeDollyBodyOffsetBase();

            PrepareRuntimeModulesForCurrentState();
            SyncRuntimeModuleStateSnapshot();
        }

        private void Update()
        {
            HandleRuntimeModuleSwitches();

            if (enableScreenPositionModule)
                DriveScreenPosition();

            if (enableDutchRollModule)
                DriveDutchRoll();

            if (enableBreathingZoomModule)
                DriveBreathingZoom();

            if (enableRigDriftModule)
                DriveRigDrift();

            if (enableAccentZoomModule)
                DriveAccentZoom();

            if (enableDollyBodyOffsetModule)
                DriveDollyBodyOffset();
        }

        private void HandleRuntimeModuleSwitches()
        {
            if (enableLookTargetModule != previousEnableLookTargetModule)
            {
                if (enableLookTargetModule)
                    ActivateLookTargetModule();
                else
                    DeactivateLookTargetModule();
            }

            if (enableFollowTargetModule != previousEnableFollowTargetModule)
            {
                if (enableFollowTargetModule)
                    ActivateFollowTargetModule();
                else
                    DeactivateFollowTargetModule();
            }

            if (enableScreenPositionModule != previousEnableScreenPositionModule)
            {
                if (enableScreenPositionModule)
                    DriveScreenPosition();
                else
                    RestoreScreenPositionBase();
            }

            if (enableDutchRollModule != previousEnableDutchRollModule)
            {
                if (enableDutchRollModule)
                    DriveDutchRoll();
                else
                    RestoreDutchRollBase();
            }

            bool previousFovModulesEnabled = previousEnableBreathingZoomModule || previousEnableAccentZoomModule;
            bool fovModulesEnabled = enableBreathingZoomModule || enableAccentZoomModule;
            if (!previousFovModulesEnabled && fovModulesEnabled)
            {
                CaptureFovBeforeModules();
            }
            else if (previousFovModulesEnabled && !fovModulesEnabled)
            {
                RestoreFovBeforeModules();
            }

            if (enableBreathingZoomModule != previousEnableBreathingZoomModule && enableBreathingZoomModule)
            {
                DriveBreathingZoom();
            }

            if (enableRigDriftModule != previousEnableRigDriftModule)
            {
                if (enableRigDriftModule)
                    CaptureRigDriftPoseBeforeModule();
                else
                    RestoreRigDriftPoseBeforeModule();
            }

            if (enableAccentZoomModule != previousEnableAccentZoomModule)
            {
                if (enableAccentZoomModule)
                    ResetAccentZoomRuntimeState();
            }

            if (enableDollyBodyOffsetModule != previousEnableDollyBodyOffsetModule)
            {
                if (enableDollyBodyOffsetModule)
                    ActivateDollyBodyOffsetModule();
                else
                    RestoreInitialDollyOffsetRuntime();
            }

            SyncRuntimeModuleStateSnapshot();
        }

        private void PrepareRuntimeModulesForCurrentState()
        {
            if (enableLookTargetModule)
                CaptureLookTargetBeforeModule();

            if (enableFollowTargetModule)
                CaptureFollowTargetBeforeModule();

            if (enableBreathingZoomModule || enableAccentZoomModule)
                CaptureFovBeforeModules();

            if (enableRigDriftModule)
                CaptureRigDriftPoseBeforeModule();

            if (enableDollyBodyOffsetModule)
                ActivateDollyBodyOffsetModule();
        }

        private void SyncRuntimeModuleStateSnapshot()
        {
            previousEnableLookTargetModule = enableLookTargetModule;
            previousEnableFollowTargetModule = enableFollowTargetModule;
            previousEnableScreenPositionModule = enableScreenPositionModule;
            previousEnableDutchRollModule = enableDutchRollModule;
            previousEnableBreathingZoomModule = enableBreathingZoomModule;
            previousEnableRigDriftModule = enableRigDriftModule;
            previousEnableAccentZoomModule = enableAccentZoomModule;
            previousEnableDollyBodyOffsetModule = enableDollyBodyOffsetModule;
        }

        private void OnValidate()
        {
            if (driftRigTarget == null)
            {
                driftRigTarget = transform;
            }

            if (sharedLookTargetRig == null)
            {
                sharedLookTargetRig = FindLookTargetRigAutomatically();
            }

            SyncPresetDisplayNameFromAsset();
            NormalizeValuesAfterPresetApply();
        }

        private void ResolveStageCameraReference()
        {
            if (stageVirtualCamera != null)
                return;

            stageVirtualCamera = GetComponent<CinemachineVirtualCamera>();

            if (stageVirtualCamera == null)
            {
                stageVirtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
            }
        }

        [ContextMenu("Find Stage Virtual Camera In Children")]
        private void FindStageVirtualCameraInChildren()
        {
            stageVirtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
        }

        private void ResolveRigDriftTarget()
        {
            resolvedDriftRigTarget = driftRigTarget != null ? driftRigTarget : transform;
        }

        private void CacheBodyDriverComponents()
        {
            ResolveStageCameraReference();

            if (stageVirtualCamera == null)
                return;

            cachedBodyTransposer = stageVirtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            cachedComposer = stageVirtualCamera.GetCinemachineComponent<CinemachineComposer>();
            cachedFramingTransposer = stageVirtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        }

        private float RemapValue(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        private PlayableDirector ResolveDirector(PlayableDirector overrideDirector)
        {
            if (overrideDirector != null)
                return overrideDirector;

            if (sharedPlayableDirector != null)
                return sharedPlayableDirector;

            PlayableDirector timeTableDirector = FindPlayableDirectorFromTimeTable();
            if (timeTableDirector != null)
            {
                sharedPlayableDirector = timeTableDirector;
            }

            return timeTableDirector;
        }

        [ContextMenu("Resolve PlayableDirector From TimeTable")]
        public void ResolvePlayableDirectorFromTimeTable()
        {
            AssignPlayableDirectorFromTimeTable(true);
        }

        private void AutoBindPlayableDirectorFromTimeTableIfNeeded()
        {
            if (!autoBindPlayableDirectorFromTimeTable)
                return;

            AssignPlayableDirectorFromTimeTable(false);
        }

        private void AssignPlayableDirectorFromTimeTable(bool overwriteExisting)
        {
            if (!overwriteExisting && sharedPlayableDirector != null)
                return;

            PlayableDirector timeTableDirector = FindPlayableDirectorFromTimeTable(overwriteExisting);
            if (timeTableDirector == null)
                return;

            sharedPlayableDirector = timeTableDirector;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

        private PlayableDirector FindPlayableDirectorFromTimeTable(bool allowManualResolve = false)
        {
            if (!allowManualResolve && !autoBindPlayableDirectorFromTimeTable)
                return null;

            VLiveTimeTable timeTable = ResolveTimeTable();
            return timeTable != null ? timeTable.GetTimelineOrMaster(liveTimelineSectionName) : null;
        }

        private VLiveTimeTable ResolveTimeTable()
        {
            if (cachedTimeTable != null)
                return cachedTimeTable;

            cachedTimeTable = VLiveTimeTable.Get(this);
            return cachedTimeTable;
        }

        private VLiveLookTargetRig ResolveLookTargetRig(VLiveLookTargetRig overrideRig)
        {
            if (overrideRig != null)
                return overrideRig;

            if (sharedLookTargetRig != null)
                return sharedLookTargetRig;

            VLiveLookTargetRig autoFound = FindLookTargetRigAutomatically();
            if (autoFound != null)
            {
                sharedLookTargetRig = autoFound;

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }

            return autoFound;
        }

        private VLiveLookTargetRig FindLookTargetRigAutomatically()
        {
            VLiveLookTargetRig self = GetComponent<VLiveLookTargetRig>();
            if (self != null)
                return self;

            VLiveLookTargetRig[] parents = GetComponentsInParent<VLiveLookTargetRig>(true);
            if (parents.Length > 0)
            {
                VLiveLookTargetRig nearestParent = null;

                for (int i = 0; i < parents.Length; i++)
                {
                    if (parents[i] != null && parents[i].gameObject != gameObject)
                    {
                        nearestParent = parents[i];
                        break;
                    }
                }

                if (nearestParent != null)
                {
                    if (parents.Length > 1)
                    {
                        Debug.LogWarning("[VLiveCamera] 親階層に複数の VLiveLookTargetRig が見つかりました。最も近いものを使用します。", this);
                    }

                    return nearestParent;
                }
            }

            VLiveLookTargetRig[] children = GetComponentsInChildren<VLiveLookTargetRig>(true);
            if (children.Length > 0)
            {
                VLiveLookTargetRig firstChild = null;

                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i] != null && children[i].gameObject != gameObject)
                    {
                        firstChild = children[i];
                        break;
                    }
                }

                if (firstChild != null)
                {
                    if (children.Length > 1)
                    {
                        Debug.LogWarning("[VLiveCamera] 子階層に複数の VLiveLookTargetRig が見つかりました。最初のものを使用します。", this);
                    }

                    return firstChild;
                }
            }

            return FindAnyObjectByType<VLiveLookTargetRig>(FindObjectsInactive.Include);
        }

        private void SyncPresetDisplayNameFromAsset()
        {
            if (preset == null)
                return;

#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(preset);
            if (string.IsNullOrEmpty(path))
                return;

            string assetName = System.IO.Path.GetFileNameWithoutExtension(path);

            if (preset.presetDisplayName != assetName)
            {
                preset.presetDisplayName = assetName;
                UnityEditor.EditorUtility.SetDirty(preset);
            }
#endif
        }

#if UNITY_EDITOR
        [ContextMenu("Create Preset From This Camera")]
        public void CreatePresetFromThisCamera()
        {
            EnsurePresetFolderExists(DefaultPresetFolderPath);

            string baseName = gameObject != null ? gameObject.name : nameof(VLiveCamera);
            baseName = UnityEditor.ObjectNames.NicifyVariableName(baseName);
            baseName = SanitizeAssetFileName(baseName);

            string assetPath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
                $"{DefaultPresetFolderPath}/{baseName}.asset"
            );

            VLiveCameraPreset newPreset = ScriptableObject.CreateInstance<VLiveCameraPreset>();

            string displayName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            newPreset.presetDisplayName = displayName;

            CaptureCurrentValuesToPreset(newPreset);

            UnityEditor.AssetDatabase.CreateAsset(newPreset, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            preset = newPreset;
            SyncPresetDisplayNameFromAsset();

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.Selection.activeObject = newPreset;

            Debug.Log($"[VLiveCamera] Preset Created → {assetPath}", this);
        }

        private static void EnsurePresetFolderExists(string folderPath)
        {
            if (UnityEditor.AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";

                if (!UnityEditor.AssetDatabase.IsValidFolder(next))
                {
                    UnityEditor.AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string SanitizeAssetFileName(string fileName)
        {
            foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            fileName = fileName.Trim();

            return string.IsNullOrEmpty(fileName) ? nameof(VLiveCamera) : fileName;
        }
#endif

        [ContextMenu("Apply Preset")]
        public void ApplyPreset()
        {
            if (preset == null)
            {
                Debug.LogWarning("[VLiveCamera] preset が未設定です。", this);
                return;
            }

            SyncPresetDisplayNameFromAsset();
            ApplyPresetValues(preset);

            ResolveRigDriftTarget();
            CacheBodyDriverComponents();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif

            Debug.Log($"[VLiveCamera] Preset Applied → {preset.presetDisplayName}", this);
        }

        [ContextMenu("Capture Current Values To Preset")]
        public void CaptureToPreset()
        {
            if (preset == null)
            {
                Debug.LogWarning("[VLiveCamera] preset が未設定です。", this);
                return;
            }

            SyncPresetDisplayNameFromAsset();
            CaptureCurrentValuesToPreset(preset);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(preset);
            UnityEditor.AssetDatabase.SaveAssets();
#endif

            Debug.Log($"[VLiveCamera] Current Values Captured → {preset.presetDisplayName}", this);
        }

        public void SetPreset(VLiveCameraPreset newPreset, bool applyImmediately = false)
        {
            preset = newPreset;
            SyncPresetDisplayNameFromAsset();

            if (applyImmediately && preset != null)
            {
                ApplyPreset();
            }
#if UNITY_EDITOR
            else if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

        public void ConfigureRuntimeReferences(
            VLiveTimeTable timeTable,
            VLiveLookTargetRig targetRig,
            string timelineSectionName = null,
            bool overwriteExisting = true,
            bool assignTargetsImmediately = false)
        {
            if (timelineSectionName != null)
            {
                liveTimelineSectionName = timelineSectionName;
            }

            if (overwriteExisting || timeTable != null)
            {
                cachedTimeTable = timeTable;
                sharedPlayableDirector = timeTable != null
                    ? timeTable.GetTimelineOrMaster(liveTimelineSectionName)
                    : null;
            }

            if (overwriteExisting || targetRig != null)
            {
                sharedLookTargetRig = targetRig;
                lookTargetRig = targetRig;
                followTargetRig = targetRig;
            }

            if (assignTargetsImmediately)
            {
                AssignConfiguredTargets();
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

        [ContextMenu("Assign Configured Targets")]
        public void AssignConfiguredTargets()
        {
            if (enableLookTargetModule)
            {
                AssignLookTarget();
            }

            if (enableFollowTargetModule)
            {
                AssignFollowTarget();
            }
        }

        private void ApplyPresetValues(VLiveCameraPreset source)
        {
            enableLookTargetModule = source.enableLookTargetModule;
            assignLookTargetOnStart = source.assignLookTargetOnStart;
            lookTargetBone = source.lookTargetBone;

            enableFollowTargetModule = source.enableFollowTargetModule;
            assignFollowTargetOnStart = source.assignFollowTargetOnStart;
            followTargetBone = source.followTargetBone;

            enableScreenPositionModule = source.enableScreenPositionModule;
            useDirectorTimeForScreenPosition = source.useDirectorTimeForScreenPosition;
            useScreenPositionSinWobble = source.useScreenPositionSinWobble;
            useScreenPositionPerlinWobble = source.useScreenPositionPerlinWobble;
            screenPositionMotionMode = source.screenPositionMotionMode;
            screenPositionTimeOffset = source.screenPositionTimeOffset;
            screenPositionTimeScalePrimary = source.screenPositionTimeScalePrimary;
            screenPositionTimeScaleSecondary = source.screenPositionTimeScaleSecondary;
            screenPositionIntensityScalePrimary = source.screenPositionIntensityScalePrimary;
            screenPositionIntensityScaleSecondary = source.screenPositionIntensityScaleSecondary;
            screenPositionBase = source.screenPositionBase;
            screenPositionAmplitude = source.screenPositionAmplitude;
            screenPositionFrequency = source.screenPositionFrequency;
            screenPositionPhaseDeg = source.screenPositionPhaseDeg;
            screenPositionPerlinOffset = source.screenPositionPerlinOffset;
            previewScreenPositionInEditMode = source.previewScreenPositionInEditMode;

            enableDutchRollModule = source.enableDutchRollModule;
            useDirectorTimeForDutchRoll = source.useDirectorTimeForDutchRoll;
            dutchRollMotionMode = source.dutchRollMotionMode;
            dutchRollTimeOffset = source.dutchRollTimeOffset;
            dutchRollTimeScalePrimary = source.dutchRollTimeScalePrimary;
            dutchRollTimeScaleSecondary = source.dutchRollTimeScaleSecondary;
            dutchRollIntensityScalePrimary = source.dutchRollIntensityScalePrimary;
            dutchRollIntensityScaleSecondary = source.dutchRollIntensityScaleSecondary;
            dutchRollBaseDegrees = source.dutchRollBaseDegrees;
            dutchRollAmplitudeDegrees = source.dutchRollAmplitudeDegrees;
            dutchRollFrequency = source.dutchRollFrequency;
            dutchRollPhaseDeg = source.dutchRollPhaseDeg;
            dutchRollPerlinOffset = source.dutchRollPerlinOffset;
            previewDutchRollInEditMode = source.previewDutchRollInEditMode;

            enableBreathingZoomModule = source.enableBreathingZoomModule;
            useDirectorTimeForBreathingZoom = source.useDirectorTimeForBreathingZoom;
            breathingZoomTimeOffset = source.breathingZoomTimeOffset;
            breathingZoomMotionMode = source.breathingZoomMotionMode;
            breathingZoomTimeScalePrimary = source.breathingZoomTimeScalePrimary;
            breathingZoomTimeScaleSecondary = source.breathingZoomTimeScaleSecondary;
            breathingZoomFovMin = source.breathingZoomFovMin;
            breathingZoomFovMax = source.breathingZoomFovMax;
            breathingZoomFrequencyHz = source.breathingZoomFrequencyHz;
            breathingZoomPerlinOffset = source.breathingZoomPerlinOffset;

            enableRigDriftModule = source.enableRigDriftModule;
            driftSpace = source.driftSpace;
            syncRigDriftToDirector = source.syncRigDriftToDirector;
            rigDriftTimeScalePrimary = source.rigDriftTimeScalePrimary;
            rigDriftTimeScaleSecondary = source.rigDriftTimeScaleSecondary;
            rigDriftMotionMode = source.rigDriftMotionMode;
            driftAxisWeight = source.driftAxisWeight;
            driftFrequency = source.driftFrequency;
            driftAmplitude = source.driftAmplitude;
            driftPhaseOffset = source.driftPhaseOffset;
            driftPerlinOffset = source.driftPerlinOffset;
            driftRangeMin = source.driftRangeMin;
            driftRangeMax = source.driftRangeMax;
            rigDriftOffset = source.rigDriftOffset;
            useFigureEightDrift = source.useFigureEightDrift;

            enableAccentZoomModule = source.enableAccentZoomModule;
            accentZoomFovMin = source.accentZoomFovMin;
            accentZoomFovMax = source.accentZoomFovMax;
            accentZoomBaseFov = source.accentZoomBaseFov;
            accentZoomAmountMin = source.accentZoomAmountMin;
            accentZoomAmountMax = source.accentZoomAmountMax;
            accentZoomSeed = source.accentZoomSeed;
            accentAttack = source.accentAttack;
            accentAttackSharpness = source.accentAttackSharpness;
            accentHoldZoom = source.accentHoldZoom;
            accentReturnDuration = source.accentReturnDuration;
            accentReturnEpsilon = source.accentReturnEpsilon;
            accentHoldBase = source.accentHoldBase;
            accentStartOffset = source.accentStartOffset;
            freezeAccentZoomWhenDirectorStopped = source.freezeAccentZoomWhenDirectorStopped;
            accentZoomTimeScalePrimary = source.accentZoomTimeScalePrimary;
            accentZoomTimeScaleSecondary = source.accentZoomTimeScaleSecondary;
            accentZoomFilterMode = source.accentZoomFilterMode;
            accentLowPassTimeConstant = source.accentLowPassTimeConstant;
            accentSpringFrequency = source.accentSpringFrequency;
            accentSpringDampingRatio = source.accentSpringDampingRatio;
            resetAccentOnLargeJump = source.resetAccentOnLargeJump;
            accentJumpThreshold = source.accentJumpThreshold;

            enableDollyBodyOffsetModule = source.enableDollyBodyOffsetModule;
            dollyBodyOffsetTimeScalePrimary = source.dollyBodyOffsetTimeScalePrimary;
            dollyBodyOffsetTimeScaleSecondary = source.dollyBodyOffsetTimeScaleSecondary;
            dollyBodyOffsetBase = source.dollyBodyOffsetBase;
            dollyBodyOffsetAmplitude = source.dollyBodyOffsetAmplitude;
            dollyBodyOffsetFrequency = source.dollyBodyOffsetFrequency;
            dollyBodyOffsetPhaseDeg = source.dollyBodyOffsetPhaseDeg;
            dollyBodyOffsetMotionMode = source.dollyBodyOffsetMotionMode;
            dollyBodyOffsetPerlinOffset = source.dollyBodyOffsetPerlinOffset;
            previewDollyOffsetInEditMode = source.previewDollyOffsetInEditMode;

            NormalizeValuesAfterPresetApply();
        }

        private void CaptureCurrentValuesToPreset(VLiveCameraPreset destination)
        {
            destination.enableLookTargetModule = enableLookTargetModule;
            destination.assignLookTargetOnStart = assignLookTargetOnStart;
            destination.lookTargetBone = lookTargetBone;

            destination.enableFollowTargetModule = enableFollowTargetModule;
            destination.assignFollowTargetOnStart = assignFollowTargetOnStart;
            destination.followTargetBone = followTargetBone;

            destination.enableScreenPositionModule = enableScreenPositionModule;
            destination.useDirectorTimeForScreenPosition = useDirectorTimeForScreenPosition;
            destination.useScreenPositionSinWobble = useScreenPositionSinWobble;
            destination.useScreenPositionPerlinWobble = useScreenPositionPerlinWobble;
            destination.screenPositionMotionMode = screenPositionMotionMode;
            destination.screenPositionTimeOffset = screenPositionTimeOffset;
            destination.screenPositionTimeScalePrimary = screenPositionTimeScalePrimary;
            destination.screenPositionTimeScaleSecondary = screenPositionTimeScaleSecondary;
            destination.screenPositionIntensityScalePrimary = screenPositionIntensityScalePrimary;
            destination.screenPositionIntensityScaleSecondary = screenPositionIntensityScaleSecondary;
            destination.screenPositionBase = screenPositionBase;
            destination.screenPositionAmplitude = screenPositionAmplitude;
            destination.screenPositionFrequency = screenPositionFrequency;
            destination.screenPositionPhaseDeg = screenPositionPhaseDeg;
            destination.screenPositionPerlinOffset = screenPositionPerlinOffset;
            destination.previewScreenPositionInEditMode = previewScreenPositionInEditMode;

            destination.enableDutchRollModule = enableDutchRollModule;
            destination.useDirectorTimeForDutchRoll = useDirectorTimeForDutchRoll;
            destination.dutchRollMotionMode = dutchRollMotionMode;
            destination.dutchRollTimeOffset = dutchRollTimeOffset;
            destination.dutchRollTimeScalePrimary = dutchRollTimeScalePrimary;
            destination.dutchRollTimeScaleSecondary = dutchRollTimeScaleSecondary;
            destination.dutchRollIntensityScalePrimary = dutchRollIntensityScalePrimary;
            destination.dutchRollIntensityScaleSecondary = dutchRollIntensityScaleSecondary;
            destination.dutchRollBaseDegrees = dutchRollBaseDegrees;
            destination.dutchRollAmplitudeDegrees = dutchRollAmplitudeDegrees;
            destination.dutchRollFrequency = dutchRollFrequency;
            destination.dutchRollPhaseDeg = dutchRollPhaseDeg;
            destination.dutchRollPerlinOffset = dutchRollPerlinOffset;
            destination.previewDutchRollInEditMode = previewDutchRollInEditMode;

            destination.enableBreathingZoomModule = enableBreathingZoomModule;
            destination.useDirectorTimeForBreathingZoom = useDirectorTimeForBreathingZoom;
            destination.breathingZoomTimeOffset = breathingZoomTimeOffset;
            destination.breathingZoomMotionMode = breathingZoomMotionMode;
            destination.breathingZoomTimeScalePrimary = breathingZoomTimeScalePrimary;
            destination.breathingZoomTimeScaleSecondary = breathingZoomTimeScaleSecondary;
            destination.breathingZoomFovMin = breathingZoomFovMin;
            destination.breathingZoomFovMax = breathingZoomFovMax;
            destination.breathingZoomFrequencyHz = breathingZoomFrequencyHz;
            destination.breathingZoomPerlinOffset = breathingZoomPerlinOffset;

            destination.enableRigDriftModule = enableRigDriftModule;
            destination.driftSpace = driftSpace;
            destination.syncRigDriftToDirector = syncRigDriftToDirector;
            destination.rigDriftTimeScalePrimary = rigDriftTimeScalePrimary;
            destination.rigDriftTimeScaleSecondary = rigDriftTimeScaleSecondary;
            destination.rigDriftMotionMode = rigDriftMotionMode;
            destination.driftAxisWeight = driftAxisWeight;
            destination.driftFrequency = driftFrequency;
            destination.driftAmplitude = driftAmplitude;
            destination.driftPhaseOffset = driftPhaseOffset;
            destination.driftPerlinOffset = driftPerlinOffset;
            destination.driftRangeMin = driftRangeMin;
            destination.driftRangeMax = driftRangeMax;
            destination.rigDriftOffset = rigDriftOffset;
            destination.useFigureEightDrift = useFigureEightDrift;

            destination.enableAccentZoomModule = enableAccentZoomModule;
            destination.accentZoomFovMin = accentZoomFovMin;
            destination.accentZoomFovMax = accentZoomFovMax;
            destination.accentZoomBaseFov = accentZoomBaseFov;
            destination.accentZoomAmountMin = accentZoomAmountMin;
            destination.accentZoomAmountMax = accentZoomAmountMax;
            destination.accentZoomSeed = accentZoomSeed;
            destination.accentAttack = accentAttack;
            destination.accentAttackSharpness = accentAttackSharpness;
            destination.accentHoldZoom = accentHoldZoom;
            destination.accentReturnDuration = accentReturnDuration;
            destination.accentReturnEpsilon = accentReturnEpsilon;
            destination.accentHoldBase = accentHoldBase;
            destination.accentStartOffset = accentStartOffset;
            destination.freezeAccentZoomWhenDirectorStopped = freezeAccentZoomWhenDirectorStopped;
            destination.accentZoomTimeScalePrimary = accentZoomTimeScalePrimary;
            destination.accentZoomTimeScaleSecondary = accentZoomTimeScaleSecondary;
            destination.accentZoomFilterMode = accentZoomFilterMode;
            destination.accentLowPassTimeConstant = accentLowPassTimeConstant;
            destination.accentSpringFrequency = accentSpringFrequency;
            destination.accentSpringDampingRatio = accentSpringDampingRatio;
            destination.resetAccentOnLargeJump = resetAccentOnLargeJump;
            destination.accentJumpThreshold = accentJumpThreshold;

            destination.enableDollyBodyOffsetModule = enableDollyBodyOffsetModule;
            destination.dollyBodyOffsetTimeScalePrimary = dollyBodyOffsetTimeScalePrimary;
            destination.dollyBodyOffsetTimeScaleSecondary = dollyBodyOffsetTimeScaleSecondary;
            destination.dollyBodyOffsetBase = dollyBodyOffsetBase;
            destination.dollyBodyOffsetAmplitude = dollyBodyOffsetAmplitude;
            destination.dollyBodyOffsetFrequency = dollyBodyOffsetFrequency;
            destination.dollyBodyOffsetPhaseDeg = dollyBodyOffsetPhaseDeg;
            destination.dollyBodyOffsetMotionMode = dollyBodyOffsetMotionMode;
            destination.dollyBodyOffsetPerlinOffset = dollyBodyOffsetPerlinOffset;
            destination.previewDollyOffsetInEditMode = previewDollyOffsetInEditMode;
        }

        private void NormalizeValuesAfterPresetApply()
        {
            screenPositionBase.x = Mathf.Clamp01(screenPositionBase.x);
            screenPositionBase.y = Mathf.Clamp01(screenPositionBase.y);
            screenPositionFrequency.x = Mathf.Max(0f, screenPositionFrequency.x);
            screenPositionFrequency.y = Mathf.Max(0f, screenPositionFrequency.y);
            dutchRollAmplitudeDegrees = Mathf.Max(0f, dutchRollAmplitudeDegrees);
            dutchRollFrequency = Mathf.Max(0f, dutchRollFrequency);
            breathingZoomFrequencyHz = Mathf.Max(0f, breathingZoomFrequencyHz);
            driftFrequency.x = Mathf.Max(0f, driftFrequency.x);
            driftFrequency.y = Mathf.Max(0f, driftFrequency.y);
            driftFrequency.z = Mathf.Max(0f, driftFrequency.z);
            dollyBodyOffsetFrequency.x = Mathf.Max(0f, dollyBodyOffsetFrequency.x);
            dollyBodyOffsetFrequency.y = Mathf.Max(0f, dollyBodyOffsetFrequency.y);
            dollyBodyOffsetFrequency.z = Mathf.Max(0f, dollyBodyOffsetFrequency.z);

            if (breathingZoomFovMax < breathingZoomFovMin)
            {
                (breathingZoomFovMin, breathingZoomFovMax) = (breathingZoomFovMax, breathingZoomFovMin);
            }

            if (accentZoomFovMax < accentZoomFovMin)
            {
                (accentZoomFovMin, accentZoomFovMax) = (accentZoomFovMax, accentZoomFovMin);
            }

            if (accentZoomAmountMax > accentZoomAmountMin)
            {
                (accentZoomAmountMin, accentZoomAmountMax) = (accentZoomAmountMax, accentZoomAmountMin);
            }
        }
    }
}
