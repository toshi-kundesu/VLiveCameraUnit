using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;
using Cinemachine;

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

        private const float Tau = 2f * Mathf.PI;

        [Header("▼ Camera Preset")]
        [SerializeField] private VLiveCameraPreset preset;
        [SerializeField] private bool applyPresetOnStart = false;

        [Header("▼ Shared References")]
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

        [FormerlySerializedAs("aimBone")]
        [SerializeField] private HumanBodyBones lookTargetBone = HumanBodyBones.Head;

        [FormerlySerializedAs("aimTargetGO")]
        [SerializeField] private GameObject lookTargetMarker;

        [Header("▼ Follow Target Module")]
        [FormerlySerializedAs("enableTrackingModule")]
        [SerializeField] private bool enableFollowTargetModule = false;

        [FormerlySerializedAs("assignTrackingOnStart")]
        [SerializeField] private bool assignFollowTargetOnStart = true;

        [FormerlySerializedAs("trackingBoneCollector")]
        [SerializeField] private VLiveLookTargetRig followTargetRig;

        [FormerlySerializedAs("trackingBone")]
        [SerializeField] private HumanBodyBones followTargetBone = HumanBodyBones.Hips;

        [FormerlySerializedAs("trackingTargetGO")]
        [SerializeField] private GameObject followTargetMarker;

        [Header("▼ Breathing Zoom Module")]
        [FormerlySerializedAs("enableZoomSineModule")]
        [SerializeField] private bool enableBreathingZoomModule = false;

        [FormerlySerializedAs("zoomCueDirector")]
        [SerializeField] private PlayableDirector breathingZoomDirector;

        [FormerlySerializedAs("useCueTimeForZoom")]
        [SerializeField] private bool useDirectorTimeForBreathingZoom = true;

        [FormerlySerializedAs("zoomCueTimeOffset")]
        [SerializeField] private float breathingZoomTimeOffset = 0f;

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

        [FormerlySerializedAs("swayAxisWeight")]
        [SerializeField] private Vector3 driftAxisWeight = Vector3.one;

        [FormerlySerializedAs("swayFrequency")]
        [SerializeField] private Vector3 driftFrequency = Vector3.one;

        [FormerlySerializedAs("swayAmplitude")]
        [SerializeField] private Vector3 driftAmplitude = Vector3.one;

        [FormerlySerializedAs("swayPhaseOffset")]
        [SerializeField] private Vector3 driftPhaseOffset = Vector3.zero;

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
#endif

        private Transform resolvedDriftRigTarget;
        private CinemachineTransposer cachedBodyTransposer;
        private CinemachineFramingTransposer cachedFramingTransposer;

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

            if (!dollyBodyOffsetDirector)
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
                AssignLookTarget();
            }

            if (enableFollowTargetModule && assignFollowTargetOnStart)
            {
                AssignFollowTarget();
            }

            ResolveRigDriftTarget();
            CacheBodyDriverComponents();

            if (!dollyBodyOffsetDirector)
                dollyBodyOffsetDirector = GetComponentInParent<PlayableDirector>();

            if (!dollyBodyOffsetInitialized)
                InitializeDollyBodyOffsetBase();
        }

        private void Update()
        {
            if (enableBreathingZoomModule)
                DriveBreathingZoom();

            if (enableRigDriftModule)
                DriveRigDrift();

            if (enableAccentZoomModule)
                DriveAccentZoom();

            if (enableDollyBodyOffsetModule)
                DriveDollyBodyOffset();
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
            cachedFramingTransposer = stageVirtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        }

        private float RemapValue(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        private PlayableDirector ResolveDirector(PlayableDirector overrideDirector)
        {
            return overrideDirector != null ? overrideDirector : sharedPlayableDirector;
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

            return FindObjectOfType<VLiveLookTargetRig>(true);
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
        }

        private void ApplyPresetValues(VLiveCameraPreset source)
        {
            enableLookTargetModule = source.enableLookTargetModule;
            assignLookTargetOnStart = source.assignLookTargetOnStart;
            lookTargetBone = source.lookTargetBone;

            enableFollowTargetModule = source.enableFollowTargetModule;
            assignFollowTargetOnStart = source.assignFollowTargetOnStart;
            followTargetBone = source.followTargetBone;

            enableBreathingZoomModule = source.enableBreathingZoomModule;
            useDirectorTimeForBreathingZoom = source.useDirectorTimeForBreathingZoom;
            breathingZoomTimeOffset = source.breathingZoomTimeOffset;
            breathingZoomTimeScalePrimary = source.breathingZoomTimeScalePrimary;
            breathingZoomTimeScaleSecondary = source.breathingZoomTimeScaleSecondary;
            breathingZoomFovMin = source.breathingZoomFovMin;
            breathingZoomFovMax = source.breathingZoomFovMax;
            breathingZoomFrequencyHz = source.breathingZoomFrequencyHz;

            enableRigDriftModule = source.enableRigDriftModule;
            driftSpace = source.driftSpace;
            syncRigDriftToDirector = source.syncRigDriftToDirector;
            rigDriftTimeScalePrimary = source.rigDriftTimeScalePrimary;
            rigDriftTimeScaleSecondary = source.rigDriftTimeScaleSecondary;
            driftAxisWeight = source.driftAxisWeight;
            driftFrequency = source.driftFrequency;
            driftAmplitude = source.driftAmplitude;
            driftPhaseOffset = source.driftPhaseOffset;
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

            destination.enableBreathingZoomModule = enableBreathingZoomModule;
            destination.useDirectorTimeForBreathingZoom = useDirectorTimeForBreathingZoom;
            destination.breathingZoomTimeOffset = breathingZoomTimeOffset;
            destination.breathingZoomTimeScalePrimary = breathingZoomTimeScalePrimary;
            destination.breathingZoomTimeScaleSecondary = breathingZoomTimeScaleSecondary;
            destination.breathingZoomFovMin = breathingZoomFovMin;
            destination.breathingZoomFovMax = breathingZoomFovMax;
            destination.breathingZoomFrequencyHz = breathingZoomFrequencyHz;

            destination.enableRigDriftModule = enableRigDriftModule;
            destination.driftSpace = driftSpace;
            destination.syncRigDriftToDirector = syncRigDriftToDirector;
            destination.rigDriftTimeScalePrimary = rigDriftTimeScalePrimary;
            destination.rigDriftTimeScaleSecondary = rigDriftTimeScaleSecondary;
            destination.driftAxisWeight = driftAxisWeight;
            destination.driftFrequency = driftFrequency;
            destination.driftAmplitude = driftAmplitude;
            destination.driftPhaseOffset = driftPhaseOffset;
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
            destination.previewDollyOffsetInEditMode = previewDollyOffsetInEditMode;
        }

        private void NormalizeValuesAfterPresetApply()
        {
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