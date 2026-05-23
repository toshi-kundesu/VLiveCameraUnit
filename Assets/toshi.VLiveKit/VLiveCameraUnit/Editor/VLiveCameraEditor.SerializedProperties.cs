#if UNITY_EDITOR
using UnityEditor;

namespace toshi.VLiveKit.Photography.Editor
{
    public partial class VLiveCameraEditor
    {
        private SerializedProperty preset;
        private SerializedProperty applyPresetOnStart;
        private SerializedProperty autoBindPlayableDirectorFromTimeTable;
        private SerializedProperty liveTimelineSectionName;
        private SerializedProperty sharedPlayableDirector;
        private SerializedProperty sharedLookTargetRig;
        private SerializedProperty stageVirtualCamera;

        private SerializedProperty enableLookTargetModule;
        private SerializedProperty assignLookTargetOnStart;
        private SerializedProperty lookTargetRig;
        private SerializedProperty lookTargetMode;
        private SerializedProperty lookTargetBone;
        private SerializedProperty lookTargetTransform;
        private SerializedProperty lookTargetMarker;

        private SerializedProperty enableFollowTargetModule;
        private SerializedProperty assignFollowTargetOnStart;
        private SerializedProperty followTargetRig;
        private SerializedProperty followTargetMode;
        private SerializedProperty followTargetBone;
        private SerializedProperty followTargetTransform;
        private SerializedProperty followTargetMarker;

        private SerializedProperty enableScreenPositionModule;
        private SerializedProperty screenPositionDirector;
        private SerializedProperty useDirectorTimeForScreenPosition;
        private SerializedProperty useScreenPositionSinWobble;
        private SerializedProperty useScreenPositionPerlinWobble;
        private SerializedProperty screenPositionMotionMode;
        private SerializedProperty screenPositionTimeOffset;
        private SerializedProperty screenPositionTimeScalePrimary;
        private SerializedProperty screenPositionTimeScaleSecondary;
        private SerializedProperty screenPositionIntensityScalePrimary;
        private SerializedProperty screenPositionIntensityScaleSecondary;
        private SerializedProperty screenPositionBase;
        private SerializedProperty screenPositionAmplitude;
        private SerializedProperty screenPositionFrequency;
        private SerializedProperty screenPositionPhaseDeg;
        private SerializedProperty screenPositionPerlinOffset;
        private SerializedProperty previewScreenPositionInEditMode;
        private SerializedProperty screenPositionEvaluatedTimeDebug;
        private SerializedProperty screenPositionOutputDebug;

        private SerializedProperty enableDutchRollModule;
        private SerializedProperty dutchRollDirector;
        private SerializedProperty useDirectorTimeForDutchRoll;
        private SerializedProperty dutchRollMotionMode;
        private SerializedProperty dutchRollTimeOffset;
        private SerializedProperty dutchRollTimeScalePrimary;
        private SerializedProperty dutchRollTimeScaleSecondary;
        private SerializedProperty dutchRollIntensityScalePrimary;
        private SerializedProperty dutchRollIntensityScaleSecondary;
        private SerializedProperty dutchRollBaseDegrees;
        private SerializedProperty dutchRollAmplitudeDegrees;
        private SerializedProperty dutchRollFrequency;
        private SerializedProperty dutchRollPhaseDeg;
        private SerializedProperty dutchRollPerlinOffset;
        private SerializedProperty previewDutchRollInEditMode;
        private SerializedProperty dutchRollEvaluatedTimeDebug;
        private SerializedProperty dutchRollOutputDegreesDebug;

        private SerializedProperty enableBreathingZoomModule;
        private SerializedProperty breathingZoomDirector;
        private SerializedProperty useDirectorTimeForBreathingZoom;
        private SerializedProperty breathingZoomTimeOffset;
        private SerializedProperty breathingZoomMotionMode;
        private SerializedProperty breathingZoomTimeScalePrimary;
        private SerializedProperty breathingZoomTimeScaleSecondary;
        private SerializedProperty breathingZoomFovMin;
        private SerializedProperty breathingZoomFovMax;
        private SerializedProperty breathingZoomFrequencyHz;
        private SerializedProperty breathingZoomPerlinOffset;
        private SerializedProperty breathingZoomEvaluatedTimeDebug;

        private SerializedProperty enableRigDriftModule;
        private SerializedProperty driftRigTarget;
        private SerializedProperty driftSpace;
        private SerializedProperty syncRigDriftToDirector;
        private SerializedProperty rigDriftDirector;
        private SerializedProperty rigDriftTimeScalePrimary;
        private SerializedProperty rigDriftTimeScaleSecondary;
        private SerializedProperty rigDriftMotionMode;
        private SerializedProperty driftAxisWeight;
        private SerializedProperty driftFrequency;
        private SerializedProperty driftAmplitude;
        private SerializedProperty driftPhaseOffset;
        private SerializedProperty driftPerlinOffset;
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
        private SerializedProperty dollyBodyOffsetMotionMode;
        private SerializedProperty dollyBodyOffsetPerlinOffset;
        private SerializedProperty previewDollyOffsetInEditMode;

        private void CacheSerializedProperties()
        {
            preset = serializedObject.FindProperty("preset");
            applyPresetOnStart = serializedObject.FindProperty("applyPresetOnStart");
            autoBindPlayableDirectorFromTimeTable = serializedObject.FindProperty("autoBindPlayableDirectorFromTimeTable");
            liveTimelineSectionName = serializedObject.FindProperty("liveTimelineSectionName");
            sharedPlayableDirector = serializedObject.FindProperty("sharedPlayableDirector");
            sharedLookTargetRig = serializedObject.FindProperty("sharedLookTargetRig");
            stageVirtualCamera = serializedObject.FindProperty("stageVirtualCamera");

            enableLookTargetModule = serializedObject.FindProperty("enableLookTargetModule");
            assignLookTargetOnStart = serializedObject.FindProperty("assignLookTargetOnStart");
            lookTargetRig = serializedObject.FindProperty("lookTargetRig");
            lookTargetMode = serializedObject.FindProperty("lookTargetMode");
            lookTargetBone = serializedObject.FindProperty("lookTargetBone");
            lookTargetTransform = serializedObject.FindProperty("lookTargetTransform");
            lookTargetMarker = serializedObject.FindProperty("lookTargetMarker");

            enableFollowTargetModule = serializedObject.FindProperty("enableFollowTargetModule");
            assignFollowTargetOnStart = serializedObject.FindProperty("assignFollowTargetOnStart");
            followTargetRig = serializedObject.FindProperty("followTargetRig");
            followTargetMode = serializedObject.FindProperty("followTargetMode");
            followTargetBone = serializedObject.FindProperty("followTargetBone");
            followTargetTransform = serializedObject.FindProperty("followTargetTransform");
            followTargetMarker = serializedObject.FindProperty("followTargetMarker");

            enableScreenPositionModule = serializedObject.FindProperty("enableScreenPositionModule");
            screenPositionDirector = serializedObject.FindProperty("screenPositionDirector");
            useDirectorTimeForScreenPosition = serializedObject.FindProperty("useDirectorTimeForScreenPosition");
            useScreenPositionSinWobble = serializedObject.FindProperty("useScreenPositionSinWobble");
            useScreenPositionPerlinWobble = serializedObject.FindProperty("useScreenPositionPerlinWobble");
            screenPositionMotionMode = serializedObject.FindProperty("screenPositionMotionMode");
            screenPositionTimeOffset = serializedObject.FindProperty("screenPositionTimeOffset");
            screenPositionTimeScalePrimary = serializedObject.FindProperty("screenPositionTimeScalePrimary");
            screenPositionTimeScaleSecondary = serializedObject.FindProperty("screenPositionTimeScaleSecondary");
            screenPositionIntensityScalePrimary = serializedObject.FindProperty("screenPositionIntensityScalePrimary");
            screenPositionIntensityScaleSecondary = serializedObject.FindProperty("screenPositionIntensityScaleSecondary");
            screenPositionBase = serializedObject.FindProperty("screenPositionBase");
            screenPositionAmplitude = serializedObject.FindProperty("screenPositionAmplitude");
            screenPositionFrequency = serializedObject.FindProperty("screenPositionFrequency");
            screenPositionPhaseDeg = serializedObject.FindProperty("screenPositionPhaseDeg");
            screenPositionPerlinOffset = serializedObject.FindProperty("screenPositionPerlinOffset");
            previewScreenPositionInEditMode = serializedObject.FindProperty("previewScreenPositionInEditMode");
            screenPositionEvaluatedTimeDebug = serializedObject.FindProperty("screenPositionEvaluatedTimeDebug");
            screenPositionOutputDebug = serializedObject.FindProperty("screenPositionOutputDebug");

            enableDutchRollModule = serializedObject.FindProperty("enableDutchRollModule");
            dutchRollDirector = serializedObject.FindProperty("dutchRollDirector");
            useDirectorTimeForDutchRoll = serializedObject.FindProperty("useDirectorTimeForDutchRoll");
            dutchRollMotionMode = serializedObject.FindProperty("dutchRollMotionMode");
            dutchRollTimeOffset = serializedObject.FindProperty("dutchRollTimeOffset");
            dutchRollTimeScalePrimary = serializedObject.FindProperty("dutchRollTimeScalePrimary");
            dutchRollTimeScaleSecondary = serializedObject.FindProperty("dutchRollTimeScaleSecondary");
            dutchRollIntensityScalePrimary = serializedObject.FindProperty("dutchRollIntensityScalePrimary");
            dutchRollIntensityScaleSecondary = serializedObject.FindProperty("dutchRollIntensityScaleSecondary");
            dutchRollBaseDegrees = serializedObject.FindProperty("dutchRollBaseDegrees");
            dutchRollAmplitudeDegrees = serializedObject.FindProperty("dutchRollAmplitudeDegrees");
            dutchRollFrequency = serializedObject.FindProperty("dutchRollFrequency");
            dutchRollPhaseDeg = serializedObject.FindProperty("dutchRollPhaseDeg");
            dutchRollPerlinOffset = serializedObject.FindProperty("dutchRollPerlinOffset");
            previewDutchRollInEditMode = serializedObject.FindProperty("previewDutchRollInEditMode");
            dutchRollEvaluatedTimeDebug = serializedObject.FindProperty("dutchRollEvaluatedTimeDebug");
            dutchRollOutputDegreesDebug = serializedObject.FindProperty("dutchRollOutputDegreesDebug");

            enableBreathingZoomModule = serializedObject.FindProperty("enableBreathingZoomModule");
            breathingZoomDirector = serializedObject.FindProperty("breathingZoomDirector");
            useDirectorTimeForBreathingZoom = serializedObject.FindProperty("useDirectorTimeForBreathingZoom");
            breathingZoomTimeOffset = serializedObject.FindProperty("breathingZoomTimeOffset");
            breathingZoomMotionMode = serializedObject.FindProperty("breathingZoomMotionMode");
            breathingZoomTimeScalePrimary = serializedObject.FindProperty("breathingZoomTimeScalePrimary");
            breathingZoomTimeScaleSecondary = serializedObject.FindProperty("breathingZoomTimeScaleSecondary");
            breathingZoomFovMin = serializedObject.FindProperty("breathingZoomFovMin");
            breathingZoomFovMax = serializedObject.FindProperty("breathingZoomFovMax");
            breathingZoomFrequencyHz = serializedObject.FindProperty("breathingZoomFrequencyHz");
            breathingZoomPerlinOffset = serializedObject.FindProperty("breathingZoomPerlinOffset");
            breathingZoomEvaluatedTimeDebug = serializedObject.FindProperty("breathingZoomEvaluatedTimeDebug");

            enableRigDriftModule = serializedObject.FindProperty("enableRigDriftModule");
            driftRigTarget = serializedObject.FindProperty("driftRigTarget");
            driftSpace = serializedObject.FindProperty("driftSpace");
            syncRigDriftToDirector = serializedObject.FindProperty("syncRigDriftToDirector");
            rigDriftDirector = serializedObject.FindProperty("rigDriftDirector");
            rigDriftTimeScalePrimary = serializedObject.FindProperty("rigDriftTimeScalePrimary");
            rigDriftTimeScaleSecondary = serializedObject.FindProperty("rigDriftTimeScaleSecondary");
            rigDriftMotionMode = serializedObject.FindProperty("rigDriftMotionMode");
            driftAxisWeight = serializedObject.FindProperty("driftAxisWeight");
            driftFrequency = serializedObject.FindProperty("driftFrequency");
            driftAmplitude = serializedObject.FindProperty("driftAmplitude");
            driftPhaseOffset = serializedObject.FindProperty("driftPhaseOffset");
            driftPerlinOffset = serializedObject.FindProperty("driftPerlinOffset");
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
            dollyBodyOffsetMotionMode = serializedObject.FindProperty("dollyBodyOffsetMotionMode");
            dollyBodyOffsetPerlinOffset = serializedObject.FindProperty("dollyBodyOffsetPerlinOffset");
            previewDollyOffsetInEditMode = serializedObject.FindProperty("previewDollyOffsetInEditMode");
        }
    }
}
#endif
