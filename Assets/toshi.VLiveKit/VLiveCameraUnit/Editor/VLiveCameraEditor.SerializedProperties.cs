#if UNITY_EDITOR
using UnityEditor;

namespace toshi.VLiveKit.Photography.Editor
{
    public partial class VLiveCameraEditor
    {
        private SerializedProperty preset;
        private SerializedProperty applyPresetOnStart;
        private SerializedProperty sharedPlayableDirector;
        private SerializedProperty sharedLookTargetRig;
        private SerializedProperty stageVirtualCamera;

        private SerializedProperty enableLookTargetModule;
        private SerializedProperty assignLookTargetOnStart;
        private SerializedProperty lookTargetRig;
        private SerializedProperty lookTargetBone;
        private SerializedProperty lookTargetMarker;

        private SerializedProperty enableFollowTargetModule;
        private SerializedProperty assignFollowTargetOnStart;
        private SerializedProperty followTargetRig;
        private SerializedProperty followTargetBone;
        private SerializedProperty followTargetMarker;

        private SerializedProperty enableBreathingZoomModule;
        private SerializedProperty breathingZoomDirector;
        private SerializedProperty useDirectorTimeForBreathingZoom;
        private SerializedProperty breathingZoomTimeOffset;
        private SerializedProperty breathingZoomTimeScalePrimary;
        private SerializedProperty breathingZoomTimeScaleSecondary;
        private SerializedProperty breathingZoomFovMin;
        private SerializedProperty breathingZoomFovMax;
        private SerializedProperty breathingZoomFrequencyHz;
        private SerializedProperty breathingZoomEvaluatedTimeDebug;

        private SerializedProperty enableRigDriftModule;
        private SerializedProperty driftRigTarget;
        private SerializedProperty driftSpace;
        private SerializedProperty syncRigDriftToDirector;
        private SerializedProperty rigDriftDirector;
        private SerializedProperty rigDriftTimeScalePrimary;
        private SerializedProperty rigDriftTimeScaleSecondary;
        private SerializedProperty driftAxisWeight;
        private SerializedProperty driftFrequency;
        private SerializedProperty driftAmplitude;
        private SerializedProperty driftPhaseOffset;
        private SerializedProperty driftRangeMin;
        private SerializedProperty driftRangeMax;
        private SerializedProperty rigDriftOffset;
        private SerializedProperty useFigureEightDrift;

        private SerializedProperty enableAccentZoomModule;
        private SerializedProperty accentZoomDirector;
        private SerializedProperty accentZoomFovMin;
        private SerializedProperty accentZoomFovMax;
        private SerializedProperty accentZoomBaseFov;
        private SerializedProperty accentZoomAmountMin;
        private SerializedProperty accentZoomAmountMax;
        private SerializedProperty accentZoomSeed;
        private SerializedProperty accentAttack;
        private SerializedProperty accentAttackSharpness;
        private SerializedProperty accentHoldZoom;
        private SerializedProperty accentReturnDuration;
        private SerializedProperty accentReturnEpsilon;
        private SerializedProperty accentHoldBase;
        private SerializedProperty accentStartOffset;
        private SerializedProperty freezeAccentZoomWhenDirectorStopped;
        private SerializedProperty accentZoomTimeScalePrimary;
        private SerializedProperty accentZoomTimeScaleSecondary;
        private SerializedProperty accentZoomFilterMode;
        private SerializedProperty accentLowPassTimeConstant;
        private SerializedProperty accentSpringFrequency;
        private SerializedProperty accentSpringDampingRatio;
        private SerializedProperty resetAccentOnLargeJump;
        private SerializedProperty accentJumpThreshold;

        private SerializedProperty enableDollyBodyOffsetModule;
        private SerializedProperty dollyBodyOffsetTimeScalePrimary;
        private SerializedProperty dollyBodyOffsetTimeScaleSecondary;
        private SerializedProperty dollyBodyOffsetDirector;
        private SerializedProperty dollyBodyOffsetBase;
        private SerializedProperty dollyBodyOffsetAmplitude;
        private SerializedProperty dollyBodyOffsetFrequency;
        private SerializedProperty dollyBodyOffsetPhaseDeg;
        private SerializedProperty previewDollyOffsetInEditMode;

        private void CacheSerializedProperties()
        {
            preset = serializedObject.FindProperty("preset");
            applyPresetOnStart = serializedObject.FindProperty("applyPresetOnStart");
            sharedPlayableDirector = serializedObject.FindProperty("sharedPlayableDirector");
            sharedLookTargetRig = serializedObject.FindProperty("sharedLookTargetRig");
            stageVirtualCamera = serializedObject.FindProperty("stageVirtualCamera");

            enableLookTargetModule = serializedObject.FindProperty("enableLookTargetModule");
            assignLookTargetOnStart = serializedObject.FindProperty("assignLookTargetOnStart");
            lookTargetRig = serializedObject.FindProperty("lookTargetRig");
            lookTargetBone = serializedObject.FindProperty("lookTargetBone");
            lookTargetMarker = serializedObject.FindProperty("lookTargetMarker");

            enableFollowTargetModule = serializedObject.FindProperty("enableFollowTargetModule");
            assignFollowTargetOnStart = serializedObject.FindProperty("assignFollowTargetOnStart");
            followTargetRig = serializedObject.FindProperty("followTargetRig");
            followTargetBone = serializedObject.FindProperty("followTargetBone");
            followTargetMarker = serializedObject.FindProperty("followTargetMarker");

            enableBreathingZoomModule = serializedObject.FindProperty("enableBreathingZoomModule");
            breathingZoomDirector = serializedObject.FindProperty("breathingZoomDirector");
            useDirectorTimeForBreathingZoom = serializedObject.FindProperty("useDirectorTimeForBreathingZoom");
            breathingZoomTimeOffset = serializedObject.FindProperty("breathingZoomTimeOffset");
            breathingZoomTimeScalePrimary = serializedObject.FindProperty("breathingZoomTimeScalePrimary");
            breathingZoomTimeScaleSecondary = serializedObject.FindProperty("breathingZoomTimeScaleSecondary");
            breathingZoomFovMin = serializedObject.FindProperty("breathingZoomFovMin");
            breathingZoomFovMax = serializedObject.FindProperty("breathingZoomFovMax");
            breathingZoomFrequencyHz = serializedObject.FindProperty("breathingZoomFrequencyHz");
            breathingZoomEvaluatedTimeDebug = serializedObject.FindProperty("breathingZoomEvaluatedTimeDebug");

            enableRigDriftModule = serializedObject.FindProperty("enableRigDriftModule");
            driftRigTarget = serializedObject.FindProperty("driftRigTarget");
            driftSpace = serializedObject.FindProperty("driftSpace");
            syncRigDriftToDirector = serializedObject.FindProperty("syncRigDriftToDirector");
            rigDriftDirector = serializedObject.FindProperty("rigDriftDirector");
            rigDriftTimeScalePrimary = serializedObject.FindProperty("rigDriftTimeScalePrimary");
            rigDriftTimeScaleSecondary = serializedObject.FindProperty("rigDriftTimeScaleSecondary");
            driftAxisWeight = serializedObject.FindProperty("driftAxisWeight");
            driftFrequency = serializedObject.FindProperty("driftFrequency");
            driftAmplitude = serializedObject.FindProperty("driftAmplitude");
            driftPhaseOffset = serializedObject.FindProperty("driftPhaseOffset");
            driftRangeMin = serializedObject.FindProperty("driftRangeMin");
            driftRangeMax = serializedObject.FindProperty("driftRangeMax");
            rigDriftOffset = serializedObject.FindProperty("rigDriftOffset");
            useFigureEightDrift = serializedObject.FindProperty("useFigureEightDrift");

            enableAccentZoomModule = serializedObject.FindProperty("enableAccentZoomModule");
            accentZoomDirector = serializedObject.FindProperty("accentZoomDirector");
            accentZoomFovMin = serializedObject.FindProperty("accentZoomFovMin");
            accentZoomFovMax = serializedObject.FindProperty("accentZoomFovMax");
            accentZoomBaseFov = serializedObject.FindProperty("accentZoomBaseFov");
            accentZoomAmountMin = serializedObject.FindProperty("accentZoomAmountMin");
            accentZoomAmountMax = serializedObject.FindProperty("accentZoomAmountMax");
            accentZoomSeed = serializedObject.FindProperty("accentZoomSeed");
            accentAttack = serializedObject.FindProperty("accentAttack");
            accentAttackSharpness = serializedObject.FindProperty("accentAttackSharpness");
            accentHoldZoom = serializedObject.FindProperty("accentHoldZoom");
            accentReturnDuration = serializedObject.FindProperty("accentReturnDuration");
            accentReturnEpsilon = serializedObject.FindProperty("accentReturnEpsilon");
            accentHoldBase = serializedObject.FindProperty("accentHoldBase");
            accentStartOffset = serializedObject.FindProperty("accentStartOffset");
            freezeAccentZoomWhenDirectorStopped = serializedObject.FindProperty("freezeAccentZoomWhenDirectorStopped");
            accentZoomTimeScalePrimary = serializedObject.FindProperty("accentZoomTimeScalePrimary");
            accentZoomTimeScaleSecondary = serializedObject.FindProperty("accentZoomTimeScaleSecondary");
            accentZoomFilterMode = serializedObject.FindProperty("accentZoomFilterMode");
            accentLowPassTimeConstant = serializedObject.FindProperty("accentLowPassTimeConstant");
            accentSpringFrequency = serializedObject.FindProperty("accentSpringFrequency");
            accentSpringDampingRatio = serializedObject.FindProperty("accentSpringDampingRatio");
            resetAccentOnLargeJump = serializedObject.FindProperty("resetAccentOnLargeJump");
            accentJumpThreshold = serializedObject.FindProperty("accentJumpThreshold");

            enableDollyBodyOffsetModule = serializedObject.FindProperty("enableDollyBodyOffsetModule");
            dollyBodyOffsetTimeScalePrimary = serializedObject.FindProperty("dollyBodyOffsetTimeScalePrimary");
            dollyBodyOffsetTimeScaleSecondary = serializedObject.FindProperty("dollyBodyOffsetTimeScaleSecondary");
            dollyBodyOffsetDirector = serializedObject.FindProperty("dollyBodyOffsetDirector");
            dollyBodyOffsetBase = serializedObject.FindProperty("dollyBodyOffsetBase");
            dollyBodyOffsetAmplitude = serializedObject.FindProperty("dollyBodyOffsetAmplitude");
            dollyBodyOffsetFrequency = serializedObject.FindProperty("dollyBodyOffsetFrequency");
            dollyBodyOffsetPhaseDeg = serializedObject.FindProperty("dollyBodyOffsetPhaseDeg");
            previewDollyOffsetInEditMode = serializedObject.FindProperty("previewDollyOffsetInEditMode");
        }
    }
}
#endif