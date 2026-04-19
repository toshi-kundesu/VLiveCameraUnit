using UnityEngine;
using UnityEngine.Playables;
using Cinemachine;

namespace toshi.VLiveKit.Photography
{
    public partial class VLiveCamera
    {
        // ============================================================
        // Look Target
        // ============================================================

        [ContextMenu("Assign Look Target")]
        public void AssignLookTarget()
        {
            if (!enableLookTargetModule)
                return;

            ResolveStageCameraReference();

            if (stageVirtualCamera == null)
            {
                Debug.LogWarning("[VLiveCamera] CinemachineVirtualCamera が見つかりません。", this);
                return;
            }

            VLiveLookTargetRig rig = ResolveLookTargetRig(lookTargetRig);
            if (rig == null)
            {
                Debug.LogWarning("[VLiveCamera] Look 用 VLiveLookTargetRig が指定されていません。", this);
                return;
            }

            lookTargetMarker = rig.GetBoneTG(lookTargetBone);

            if (lookTargetMarker == null)
            {
                Debug.LogWarning($"[VLiveCamera] {lookTargetBone} のターゲットが見つかりません。", this);
                return;
            }

            stageVirtualCamera.LookAt = lookTargetMarker.transform;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(stageVirtualCamera);
#endif

            Debug.Log($"[VLiveCamera] Look Target → {lookTargetMarker.name}", this);
        }

        [ContextMenu("Assign Aim Target")]
        public void AssignAimTarget()
        {
            AssignLookTarget();
        }

        [ContextMenu("Assign LookAt")]
        public void AssignLookAt()
        {
            AssignLookTarget();
        }

        // ============================================================
        // Follow Target
        // ============================================================

        [ContextMenu("Assign Follow Target")]
        public void AssignFollowTarget()
        {
            if (!enableFollowTargetModule)
                return;

            ResolveStageCameraReference();

            if (stageVirtualCamera == null)
            {
                Debug.LogWarning("[VLiveCamera] CinemachineVirtualCamera が見つかりません。", this);
                return;
            }

            VLiveLookTargetRig rig = ResolveLookTargetRig(followTargetRig);
            if (rig == null)
            {
                Debug.LogWarning("[VLiveCamera] Follow 用 VLiveLookTargetRig が指定されていません。", this);
                return;
            }

            followTargetMarker = rig.GetBoneTG(followTargetBone);

            if (followTargetMarker == null)
            {
                Debug.LogWarning($"[VLiveCamera] Follow 用 {followTargetBone} のターゲットが見つかりません。", this);
                return;
            }

            stageVirtualCamera.Follow = followTargetMarker.transform;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(stageVirtualCamera);
#endif

            Debug.Log($"[VLiveCamera] Follow Target → {followTargetMarker.name}", this);
        }

        [ContextMenu("Assign Tracking Target")]
        public void AssignTrackingTarget()
        {
            AssignFollowTarget();
        }

        [ContextMenu("Assign Follow")]
        public void AssignFollow()
        {
            AssignFollowTarget();
        }

        // ============================================================
        // Breathing Zoom
        // ============================================================

        private void DriveBreathingZoom()
        {
            ResolveStageCameraReference();

            if (stageVirtualCamera == null)
                return;

            float fov = EvaluateBreathingZoomFov();
            ApplyBreathingZoomFov(fov);
        }

        private float EvaluateBreathingZoomFov()
        {
            double time = GetBreathingZoomEvaluatedTime();
            float center = (breathingZoomFovMin + breathingZoomFovMax) * 0.5f;
            float amplitude = (breathingZoomFovMax - breathingZoomFovMin) * 0.5f;
            float angle = (float)(time * breathingZoomFrequencyHz * Mathf.PI * 2.0);

            return center + amplitude * Mathf.Sin(angle);
        }

        private double GetBreathingZoomEvaluatedTime()
        {
            double baseTime;
            PlayableDirector director = ResolveDirector(breathingZoomDirector);

            if (useDirectorTimeForBreathingZoom && director != null)
            {
                baseTime = director.time + breathingZoomTimeOffset;
            }
            else
            {
                baseTime = Time.timeSinceLevelLoad;
            }

            double evaluatedTime = baseTime * breathingZoomTimeScalePrimary * breathingZoomTimeScaleSecondary;

#if UNITY_EDITOR
            breathingZoomEvaluatedTimeDebug = (float)evaluatedTime;
#endif

            return evaluatedTime;
        }

        private void ApplyBreathingZoomFov(float fov)
        {
            LensSettings lens = stageVirtualCamera.m_Lens;
            lens.FieldOfView = fov;
            stageVirtualCamera.m_Lens = lens;
        }

        [ContextMenu("Apply Current Breathing Zoom Once")]
        private void ApplyCurrentBreathingZoomOnce()
        {
            if (!enableBreathingZoomModule)
                return;

            DriveBreathingZoom();
        }

        // ============================================================
        // Rig Drift
        // ============================================================

        [ContextMenu("Resolve Rig Drift Target")]
        private void ResolveRigDriftTargetContext()
        {
            ResolveRigDriftTarget();
        }

        [ContextMenu("Apply Current Rig Drift Once")]
        private void ApplyCurrentRigDriftOnce()
        {
            if (!enableRigDriftModule)
                return;

            ResolveRigDriftTarget();
            ApplyRigDriftPose();
        }

        private void DriveRigDrift()
        {
            ResolveRigDriftTarget();

            if (resolvedDriftRigTarget == null)
            {
                Debug.LogWarning("[VLiveCamera] Rig Drift target が見つかりません。", this);
                return;
            }

            UpdateRigDriftTime();
            ApplyRigDriftPose();
        }

        private void UpdateRigDriftTime()
        {
            PlayableDirector director = ResolveDirector(rigDriftDirector);

            if (syncRigDriftToDirector && director != null)
            {
                rigDriftTime = (float)director.time * rigDriftTimeScalePrimary * rigDriftTimeScaleSecondary;
            }
            else
            {
                rigDriftTime += Time.deltaTime * rigDriftTimeScalePrimary * rigDriftTimeScaleSecondary;
            }
        }

        private void ApplyRigDriftPose()
        {
            float sinX = Mathf.Sin((rigDriftTime * driftFrequency.x) + driftPhaseOffset.x);
            float sinY = useFigureEightDrift
                ? Mathf.Sin((2f * rigDriftTime * driftFrequency.y) + driftPhaseOffset.y)
                : Mathf.Sin((rigDriftTime * driftFrequency.y) + driftPhaseOffset.y);

            float moveX = RemapValue(sinX * driftAmplitude.x * driftAxisWeight.x, -1f, 1f, driftRangeMin.x, driftRangeMax.x);
            float moveY = RemapValue(sinY * driftAmplitude.y * driftAxisWeight.y, -1f, 1f, driftRangeMin.y, driftRangeMax.y);
            float moveZ = RemapValue(sinX * driftAmplitude.z * driftAxisWeight.z, -1f, 1f, driftRangeMin.z, driftRangeMax.z);

            Vector3 finalPosition = new Vector3(moveX, moveY, moveZ) + rigDriftOffset;

            if (driftSpace == CameraRigSpace.Global)
            {
                resolvedDriftRigTarget.position = finalPosition;
            }
            else
            {
                resolvedDriftRigTarget.localPosition = finalPosition;
            }
        }

        // ============================================================
        // Accent Zoom
        // ============================================================

        private void DriveAccentZoom()
        {
            ResolveStageCameraReference();
            PlayableDirector director = ResolveDirector(accentZoomDirector);

            if (stageVirtualCamera == null || director == null)
                return;

            accentZoomCycleLength = Mathf.Max(accentAttack + accentHoldZoom + accentReturnDuration + accentHoldBase, 1e-4f);
            ResolveAccentZoomBaseFov();

            double evaluatedTime = GetAccentZoomEvaluatedTime(director);

            if (freezeAccentZoomWhenDirectorStopped && !director.playableGraph.IsValid())
                evaluatedTime = 0.0;

            float dt = CalculateAccentZoomDeltaTime(evaluatedTime);

            if (resetAccentOnLargeJump && Mathf.Abs(dt) > accentJumpThreshold)
            {
                ResetAccentZoomOutputToBase();
                dt = 0f;
            }

            accentZoomPreviousEvalTime = evaluatedTime;

            int cycleIndex = GetAccentZoomCycleIndex(evaluatedTime, accentZoomCycleLength, accentStartOffset);
            float phase = GetAccentZoomPhaseWithinCycle(evaluatedTime, accentZoomCycleLength, accentStartOffset);

            float rand01 = Hash01(cycleIndex ^ accentZoomSeed);
            float zoomAmountThisCycle = Mathf.Lerp(accentZoomAmountMin, accentZoomAmountMax, rand01);
            float targetPeak = Mathf.Clamp(accentZoomResolvedBaseFov + zoomAmountThisCycle, accentZoomFovMin, accentZoomFovMax);

            float rawFov = EvaluateAccentZoomAtPhase(phase, accentZoomResolvedBaseFov, targetPeak);

            ApplyAccentZoomFilter(rawFov, dt);

            accentZoomOutputFov = Mathf.Clamp(accentZoomOutputFov, accentZoomFovMin, accentZoomFovMax);

            LensSettings lens = stageVirtualCamera.m_Lens;
            lens.FieldOfView = accentZoomOutputFov;
            stageVirtualCamera.m_Lens = lens;
        }

        private float CalculateAccentZoomDeltaTime(double evaluatedTime)
        {
            if (double.IsNaN(accentZoomPreviousEvalTime))
                return 0f;

            return Mathf.Clamp((float)(evaluatedTime - accentZoomPreviousEvalTime), -1f, 1f);
        }

        private void ResetAccentZoomOutputToBase()
        {
            accentZoomOutputFov = Mathf.Clamp(accentZoomResolvedBaseFov, accentZoomFovMin, accentZoomFovMax);
            accentZoomVelocityFov = 0f;
        }

        private void ApplyAccentZoomFilter(float rawFov, float dt)
        {
            switch (accentZoomFilterMode)
            {
                case AccentZoomFilterMode.None:
                    accentZoomOutputFov = rawFov;
                    break;

                case AccentZoomFilterMode.ExponentialLowPass:
                    ApplyAccentZoomLowPass(rawFov, dt);
                    break;

                case AccentZoomFilterMode.DampedSpring:
                    ApplyAccentZoomSpring(rawFov, dt);
                    break;
            }
        }

        private void ApplyAccentZoomLowPass(float rawFov, float dt)
        {
            if (dt > 0f)
            {
                float alpha = 1f - Mathf.Exp(-dt / Mathf.Max(1e-4f, accentLowPassTimeConstant));
                accentZoomOutputFov += (rawFov - accentZoomOutputFov) * alpha;
            }
            else
            {
                accentZoomOutputFov = rawFov;
                accentZoomVelocityFov = 0f;
            }
        }

        private void ApplyAccentZoomSpring(float rawFov, float dt)
        {
            if (dt > 0f)
            {
                float w = 2f * Mathf.PI * Mathf.Max(0.01f, accentSpringFrequency);
                float z = Mathf.Clamp(accentSpringDampingRatio, 0.1f, 2.0f);
                float w2 = w * w;
                float d = 2f * z * w;

                accentZoomVelocityFov += (-d * accentZoomVelocityFov - w2 * (accentZoomOutputFov - rawFov)) * dt;
                accentZoomOutputFov += accentZoomVelocityFov * dt;
            }
            else
            {
                accentZoomOutputFov = rawFov;
                accentZoomVelocityFov = 0f;
            }
        }

        private void ResolveAccentZoomBaseFov()
        {
            accentZoomResolvedBaseFov = (accentZoomBaseFov != 0f)
                ? Mathf.Clamp(accentZoomBaseFov, accentZoomFovMin, accentZoomFovMax)
                : (accentZoomFovMin + accentZoomFovMax) * 0.5f;

            if (accentZoomOutputFov <= 0f)
            {
                accentZoomOutputFov = accentZoomResolvedBaseFov;
            }
        }

        private double GetAccentZoomEvaluatedTime(PlayableDirector director)
        {
            if (director == null)
                return 0.0;

            double baseTime = director.time;
            double multiplier = (double)(accentZoomTimeScalePrimary * accentZoomTimeScaleSecondary);

            if (double.IsNaN(multiplier) || double.IsInfinity(multiplier))
                multiplier = 1.0;

            return baseTime * multiplier;
        }

        private float EvaluateAccentZoomAtPhase(float phase, float baseFov, float targetPeak)
        {
            float t1 = accentAttack;
            float t2 = t1 + accentHoldZoom;
            float t3 = t2 + accentReturnDuration;

            if (phase < t1)
            {
                float u = phase / Mathf.Max(accentAttack, 1e-6f);
                float a = 1f - Mathf.Exp(-accentAttackSharpness * u);
                return Mathf.LerpUnclamped(baseFov, targetPeak, a);
            }

            if (phase < t2)
            {
                return targetPeak;
            }

            if (phase < t3)
            {
                float dt = phase - t2;
                float amp0 = Mathf.Max(Mathf.Abs(targetPeak - baseFov), 1e-6f);
                float lam = -Mathf.Log(Mathf.Clamp(accentReturnEpsilon / amp0, 1e-6f, 1f)) / Mathf.Max(accentReturnDuration, 1e-6f);
                float w = Mathf.Exp(-lam * dt);
                return baseFov + (targetPeak - baseFov) * w;
            }

            return baseFov;
        }

        private static int GetAccentZoomCycleIndex(double timeNow, float cycleLen, float offset)
        {
            double x = (timeNow - offset) / Mathf.Max(cycleLen, 1e-6f);
            return (int)System.Math.Floor(x);
        }

        private static float GetAccentZoomPhaseWithinCycle(double timeNow, float cycleLen, float offset)
        {
            float len = Mathf.Max(cycleLen, 1e-6f);
            float r = (float)((timeNow - offset) % len);
            if (r < 0f) r += len;
            return r;
        }

        private static float Hash01(int n)
        {
            uint x = (uint)n;
            x ^= x >> 16;
            x *= 0x7feb352dU;
            x ^= x >> 15;
            x *= 0x846ca68bU;
            x ^= x >> 16;
            return (x & 0xFFFFFF) / (float)0x1000000;
        }

        // ============================================================
        // Dolly Body Offset
        // ============================================================

        private void InitializeDollyBodyOffsetBase()
        {
            if (cachedBodyTransposer != null)
            {
                dollyBodyOffsetInitialValue = cachedBodyTransposer.m_FollowOffset;
                if (dollyBodyOffsetBase == Vector3.zero)
                    dollyBodyOffsetBase = dollyBodyOffsetInitialValue;
            }
            else if (cachedFramingTransposer != null)
            {
                dollyBodyOffsetInitialValue = cachedFramingTransposer.m_TrackedObjectOffset;
                if (dollyBodyOffsetBase == Vector3.zero)
                    dollyBodyOffsetBase = dollyBodyOffsetInitialValue;
            }

            dollyBodyOffsetInitialized = true;
        }

        private void DriveDollyBodyOffset()
        {
            if (!Application.isPlaying && !previewDollyOffsetInEditMode)
                return;

            if (cachedBodyTransposer == null && cachedFramingTransposer == null)
            {
                CacheBodyDriverComponents();
                if (cachedBodyTransposer == null && cachedFramingTransposer == null)
                    return;
            }

            PlayableDirector director = ResolveDirector(dollyBodyOffsetDirector);

            float t = director
                ? (float)director.time * dollyBodyOffsetTimeScalePrimary * dollyBodyOffsetTimeScaleSecondary
                : Time.time * dollyBodyOffsetTimeScalePrimary * dollyBodyOffsetTimeScaleSecondary;

            Vector3 rad = dollyBodyOffsetPhaseDeg * Mathf.Deg2Rad;
            Vector3 wobble = new Vector3(
                dollyBodyOffsetAmplitude.x * Mathf.Sin(Tau * dollyBodyOffsetFrequency.x * t + rad.x),
                dollyBodyOffsetAmplitude.y * Mathf.Sin(Tau * dollyBodyOffsetFrequency.y * t + rad.y),
                dollyBodyOffsetAmplitude.z * Mathf.Sin(Tau * dollyBodyOffsetFrequency.z * t + rad.z)
            );

            Vector3 target = dollyBodyOffsetBase + wobble;

            if (cachedBodyTransposer != null)
                cachedBodyTransposer.m_FollowOffset = target;
            else if (cachedFramingTransposer != null)
                cachedFramingTransposer.m_TrackedObjectOffset = target;
        }

#if UNITY_EDITOR
        [ContextMenu("Record Current Dolly Offset As Base")]
        private void RecordCurrentDollyOffsetAsBase()
        {
            CacheBodyDriverComponents();

            if (cachedBodyTransposer != null)
                dollyBodyOffsetBase = cachedBodyTransposer.m_FollowOffset;
            else if (cachedFramingTransposer != null)
                dollyBodyOffsetBase = cachedFramingTransposer.m_TrackedObjectOffset;

            Debug.Log("[VLiveCamera] dollyBodyOffsetBase を記録しました。");
        }

        [ContextMenu("Restore Initial Dolly Offset")]
        private void RestoreInitialDollyOffset()
        {
            if (!dollyBodyOffsetInitialized)
                return;

            if (cachedBodyTransposer != null)
                cachedBodyTransposer.m_FollowOffset = dollyBodyOffsetInitialValue;
            else if (cachedFramingTransposer != null)
                cachedFramingTransposer.m_TrackedObjectOffset = dollyBodyOffsetInitialValue;
        }
#endif
    }
}