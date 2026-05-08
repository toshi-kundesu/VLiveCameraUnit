using UnityEngine;

namespace toshi.VLiveKit.Photography
{
    [CreateAssetMenu(
        fileName = "VLiveCameraPreset",
        menuName = "toshi/VLiveKit/Photography/VLive Camera Preset")]
    public class VLiveCameraPreset : ScriptableObject
    {
        [System.Flags]
        public enum ShotScaleTag
        {
            None = 0,
            [InspectorName("寄り")]
            Close = 1 << 0,
            [InspectorName("引き")]
            Wide = 1 << 1
        }

        [System.Flags]
        public enum StageSideTag
        {
            None = 0,
            [InspectorName("センター")]
            Center = 1 << 0,
            [InspectorName("カミテ")]
            Kamite = 1 << 1,
            [InspectorName("シモテ")]
            Shimote = 1 << 2
        }

        [System.Flags]
        public enum CameraRigTag
        {
            None = 0,
            [InspectorName("固定")]
            Fixed = 1 << 0,
            [InspectorName("レール")]
            Rail = 1 << 1,
            [InspectorName("ドローン")]
            Drone = 1 << 2,
            [InspectorName("クレーン")]
            Crane = 1 << 3,
            [InspectorName("ハンディ")]
            Handheld = 1 << 4
        }

        [Header("▼ Meta")]
        public string presetDisplayName = "歌アップ";

        [TextArea(2, 5)]
        public string presetDescription = "ライブ用カメラの演出値プリセット";

        [Header("▼ Tags")]
        public ShotScaleTag shotScaleTags = ShotScaleTag.None;
        public StageSideTag stageSideTags = StageSideTag.None;
        public CameraRigTag cameraRigTags = CameraRigTag.None;
        public string[] customTags = new string[0];

        public bool MatchesTags(
            ShotScaleTag shotScaleFilter,
            StageSideTag stageSideFilter,
            CameraRigTag cameraRigFilter,
            string customTagFilter)
        {
            if (shotScaleFilter != ShotScaleTag.None && (shotScaleTags & shotScaleFilter) == 0)
                return false;

            if (stageSideFilter != StageSideTag.None && (stageSideTags & stageSideFilter) == 0)
                return false;

            if (cameraRigFilter != CameraRigTag.None && (cameraRigTags & cameraRigFilter) == 0)
                return false;

            if (!string.IsNullOrWhiteSpace(customTagFilter) && !HasCustomTag(customTagFilter))
                return false;

            return true;
        }

        public string GetTagSummary()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            AppendShotScaleTags(builder, shotScaleTags);
            AppendStageSideTags(builder, stageSideTags);
            AppendCameraRigTags(builder, cameraRigTags);

            if (customTags != null)
            {
                for (int i = 0; i < customTags.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(customTags[i]))
                        continue;

                    AppendTag(builder, customTags[i]);
                }
            }

            return builder.Length > 0 ? builder.ToString() : "No Tags";
        }

        private bool HasCustomTag(string customTagFilter)
        {
            if (customTags == null)
                return false;

            for (int i = 0; i < customTags.Length; i++)
            {
                if (string.Equals(customTags[i], customTagFilter, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void AppendShotScaleTags(System.Text.StringBuilder builder, ShotScaleTag tags)
        {
            if ((tags & ShotScaleTag.Close) != 0) AppendTag(builder, "寄り");
            if ((tags & ShotScaleTag.Wide) != 0) AppendTag(builder, "引き");
        }

        private static void AppendStageSideTags(System.Text.StringBuilder builder, StageSideTag tags)
        {
            if ((tags & StageSideTag.Center) != 0) AppendTag(builder, "センター");
            if ((tags & StageSideTag.Kamite) != 0) AppendTag(builder, "カミテ");
            if ((tags & StageSideTag.Shimote) != 0) AppendTag(builder, "シモテ");
        }

        private static void AppendCameraRigTags(System.Text.StringBuilder builder, CameraRigTag tags)
        {
            if ((tags & CameraRigTag.Fixed) != 0) AppendTag(builder, "固定");
            if ((tags & CameraRigTag.Rail) != 0) AppendTag(builder, "レール");
            if ((tags & CameraRigTag.Drone) != 0) AppendTag(builder, "ドローン");
            if ((tags & CameraRigTag.Crane) != 0) AppendTag(builder, "クレーン");
            if ((tags & CameraRigTag.Handheld) != 0) AppendTag(builder, "ハンディ");
        }

        private static void AppendTag(System.Text.StringBuilder builder, string tag)
        {
            if (builder.Length > 0)
                builder.Append(", ");

            builder.Append(tag);
        }

        [Header("▼ Look Target Module")]
        public bool enableLookTargetModule = true;
        public bool assignLookTargetOnStart = true;
        public HumanBodyBones lookTargetBone = HumanBodyBones.Head;

        [Header("▼ Follow Target Module")]
        public bool enableFollowTargetModule = false;
        public bool assignFollowTargetOnStart = true;
        public HumanBodyBones followTargetBone = HumanBodyBones.Hips;

        [Header("▼ Breathing Zoom Module")]
        public bool enableBreathingZoomModule = false;
        public bool useDirectorTimeForBreathingZoom = true;
        public float breathingZoomTimeOffset = 0f;
        public float breathingZoomTimeScalePrimary = 1f;
        public float breathingZoomTimeScaleSecondary = 1f;
        public float breathingZoomFovMin = 30f;
        public float breathingZoomFovMax = 60f;
        public float breathingZoomFrequencyHz = 1f;

        [Header("▼ Camera Rig Drift Module")]
        public bool enableRigDriftModule = false;
        public VLiveCamera.CameraRigSpace driftSpace = VLiveCamera.CameraRigSpace.Global;
        public bool syncRigDriftToDirector = false;
        public float rigDriftTimeScalePrimary = 1f;
        public float rigDriftTimeScaleSecondary = 1f;
        public Vector3 driftAxisWeight = Vector3.one;
        public Vector3 driftFrequency = Vector3.one;
        public Vector3 driftAmplitude = Vector3.one;
        public Vector3 driftPhaseOffset = Vector3.zero;
        public Vector3 driftRangeMin = new Vector3(-1f, -1f, -1f);
        public Vector3 driftRangeMax = new Vector3(1f, 1f, 1f);
        public Vector3 rigDriftOffset = Vector3.zero;
        public bool useFigureEightDrift = false;

        [Header("▼ Accent Zoom Module")]
        public bool enableAccentZoomModule = false;
        public float accentZoomFovMin = 30f;
        public float accentZoomFovMax = 60f;
        public float accentZoomBaseFov = 0f;
        public float accentZoomAmountMin = -6f;
        public float accentZoomAmountMax = -18f;
        public int accentZoomSeed = 20250926;
        public float accentAttack = 0.06f;
        public float accentAttackSharpness = 8f;
        public float accentHoldZoom = 0.15f;
        public float accentReturnDuration = 0.30f;
        public float accentReturnEpsilon = 0.02f;
        public float accentHoldBase = 0.25f;
        public float accentStartOffset = 0f;
        public bool freezeAccentZoomWhenDirectorStopped = false;
        public float accentZoomTimeScalePrimary = 1f;
        public float accentZoomTimeScaleSecondary = 1f;
        public VLiveCamera.AccentZoomFilterMode accentZoomFilterMode = VLiveCamera.AccentZoomFilterMode.DampedSpring;
        public float accentLowPassTimeConstant = 0.12f;
        public float accentSpringFrequency = 2.0f;
        public float accentSpringDampingRatio = 1.0f;
        public bool resetAccentOnLargeJump = true;
        public float accentJumpThreshold = 0.5f;

        [Header("▼ Dolly Body Offset Module")]
        public bool enableDollyBodyOffsetModule = false;
        public float dollyBodyOffsetTimeScalePrimary = 1f;
        public float dollyBodyOffsetTimeScaleSecondary = 1f;
        public Vector3 dollyBodyOffsetBase = new Vector3(0f, 2f, -4f);
        public Vector3 dollyBodyOffsetAmplitude = new Vector3(0.1f, 0.1f, 0.1f);
        public Vector3 dollyBodyOffsetFrequency = new Vector3(0.25f, 0.35f, 0.45f);
        public Vector3 dollyBodyOffsetPhaseDeg = new Vector3(0f, 90f, 180f);
        public bool previewDollyOffsetInEditMode = true;
    }
}
