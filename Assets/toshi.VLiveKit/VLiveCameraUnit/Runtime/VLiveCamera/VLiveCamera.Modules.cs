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

            Transform targetTransform = ResolveLookTargetTransform();
            if (targetTransform == null)
            {
                return;
            }

            stageVirtualCamera.LookAt = targetTransform;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(stageVirtualCamera);
#endif

            Debug.Log($"[VLiveCamera] Look Target → {targetTransform.name}", this);
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

            Transform targetTransform = ResolveFollowTargetTransform();
            if (targetTransform == null)
            {
                return;
            }

            stageVirtualCamera.Follow = targetTransform;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(stageVirtualCamera);
#endif

            Debug.Log($"[VLiveCamera] Follow Target → {targetTransform.name}", this);
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

        private void ActivateLookTargetModule()
        {
            CaptureLookTargetBeforeModule();
            AssignLookTarget();
        }

        private void DeactivateLookTargetModule()
        {
            ResolveStageCameraReference();

            if (stageVirtualCamera == null)
                return;

            if (lookTargetBeforeModuleCaptured)
            {
                stageVirtualCamera.LookAt = lookTargetBeforeModule;
            }
            else if (lookTargetMarker != null && stageVirtualCamera.LookAt == lookTargetMarker.transform)
            {
                stageVirtualCamera.LookAt = null;
            }

            lookTargetBeforeModule = null;
            lookTargetBeforeModuleCaptured = false;
        }

        private void CaptureLookTargetBeforeModule()
        {
            if (lookTargetBeforeModuleCaptured)
                return;

            ResolveStageCameraReference();

            if (stageVirtualCamera == null)
                return;

            lookTargetBeforeModule = stageVirtualCamera.LookAt;
            lookTargetBeforeModuleCaptured = true;
        }

        private void ActivateFollowTargetModule()
        {
            CaptureFollowTargetBeforeModule();
            AssignFollowTarget();
        }

        private void DeactivateFollowTargetModule()
        {
            ResolveStageCameraReference();

            if (stageVirtualCamera == null)
                return;

            if (followTargetBeforeModuleCaptured)
            {
                stageVirtualCamera.Follow = followTargetBeforeModule;
            }
            else if (followTargetMarker != null && stageVirtualCamera.Follow == followTargetMarker.transform)
            {
                stageVirtualCamera.Follow = null;
            }

            followTargetBeforeModule = null;
            followTargetBeforeModuleCaptured = false;
        }

        private void CaptureFollowTargetBeforeModule()
        {
            if (followTargetBeforeModuleCaptured)
                return;

            ResolveStageCameraReference();

            if (stageVirtualCamera == null)
                return;

            followTargetBeforeModule = stageVirtualCamera.Follow;
            followTargetBeforeModuleCaptured = true;
        }

        private Transform ResolveLookTargetTransform()
        {
            if (lookTargetMode == TargetReferenceMode.DirectTransform)
            {
                if (lookTargetTransform == null)
                {
                    return ResolveFallbackLookTargetTransform();
                }

                lookTargetMarker = lookTargetTransform.gameObject;
                return lookTargetTransform;
            }

            VLiveLookTargetRig rig = ResolveLookTargetRig(lookTargetRig);
            if (rig == null)
            {
                return ResolveFallbackLookTargetTransform();
            }

            var lookTargetChannels = rig.LookTargetChannels;
            if (lookTargetChannels == null || lookTargetChannels.Count == 0)
            {
                return ResolveFallbackLookTargetTransform();
            }

            lookTargetMarker = rig.GetBoneTG(lookTargetBone);

            if (lookTargetMarker == null)
            {
                return ResolveFallbackLookTargetTransform();
            }

            return lookTargetMarker.transform;
        }

        private Transform ResolveFallbackLookTargetTransform()
        {
            if (fallbackLookTargetTransform == null)
            {
                GameObject fallbackTarget = new GameObject($"{name}_DefaultLookTarget");
                fallbackLookTargetTransform = fallbackTarget.transform;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEditor.Undo.RegisterCreatedObjectUndo(fallbackTarget, "Create Default Look Target");
                    UnityEditor.EditorUtility.SetDirty(this);
                }
#endif
            }

            fallbackLookTargetTransform.position = DefaultLookTargetPosition;
            fallbackLookTargetTransform.rotation = Quaternion.identity;
            lookTargetMarker = fallbackLookTargetTransform.gameObject;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.EditorUtility.SetDirty(fallbackLookTargetTransform);
            }
#endif

            return fallbackLookTargetTransform;
        }

        private Transform ResolveFollowTargetTransform()
        {
            if (followTargetMode == TargetReferenceMode.DirectTransform)
            {
                if (followTargetTransform == null)
                {
                    followTargetMarker = null;
                    Debug.LogWarning("[VLiveCamera] Follow Target Transform が指定されていません。", this);
                    return null;
                }

                followTargetMarker = followTargetTransform.gameObject;
                return followTargetTransform;
            }

            VLiveLookTargetRig rig = ResolveLookTargetRig(followTargetRig);
            if (rig == null)
            {
                followTargetMarker = null;
                Debug.LogWarning("[VLiveCamera] Follow 用 VLiveLookTargetRig が指定されていません。", this);
                return null;
            }

            followTargetMarker = rig.GetBoneTG(followTargetBone);

            if (followTargetMarker == null)
            {
                Debug.LogWarning($"[VLiveCamera] Follow 用 {followTargetBone} のターゲットが見つかりません。", this);
                return null;
            }

            return followTargetMarker.transform;
        }

        private static float EvaluateMotionSignal(
            CameraMotionSignalMode mode,
            double time,
            float frequency,
            float phaseRadians,
            float perlinOffset,
            float perlinLane,
            bool frequencyIsCycles)
        {
            float scaledTime = (float)time * Mathf.Max(0f, frequency);

            switch (mode)
            {
                case CameraMotionSignalMode.PerlinNoise:
                    return (Mathf.PerlinNoise(scaledTime + perlinOffset, perlinLane) * 2f) - 1f;

                case CameraMotionSignalMode.Sin:
                default:
                    float angle = frequencyIsCycles ? (scaledTime * Tau) + phaseRadians : scaledTime + phaseRadians;
                    return Mathf.Sin(angle);
            }
        }

        private double GetMotionEvaluatedTime(
            PlayableDirector overrideDirector,
            bool useDirectorTime,
            float timeOffset,
            float timeScalePrimary,
            float timeScaleSecondary)
        {
            PlayableDirector director = ResolveDirector(overrideDirector);
            double baseTime = useDirectorTime && director != null
                ? director.time + timeOffset
                : Time.timeSinceLevelLoad + timeOffset;

            double multiplier = (double)(timeScalePrimary * timeScaleSecondary);
            return double.IsNaN(multiplier) || double.IsInfinity(multiplier)
                ? baseTime
                : baseTime * multiplier;
        }

        // ============================================================
        // Screen Position
        // ============================================================

        private void DriveScreenPosition()
        {
            if (!Application.isPlaying && !previewScreenPositionInEditMode)
                return;

            if (!TryResolveScreenDriver())
                return;

            double time = GetMotionEvaluatedTime(
                screenPositionDirector,
                useDirectorTimeForScreenPosition,
                screenPositionTimeOffset,
                screenPositionTimeScalePrimary,
                screenPositionTimeScaleSecondary);

            float intensity = screenPositionIntensityScalePrimary * screenPositionIntensityScaleSecondary;
            Vector2 signal = Vector2.zero;

            if (useScreenPositionSinWobble)
            {
                signal += new Vector2(
                    EvaluateMotionSignal(
                        CameraMotionSignalMode.Sin,
                        time,
                        screenPositionFrequency.x,
                        screenPositionPhaseDeg.x * Mathf.Deg2Rad,
                        screenPositionPerlinOffset.x,
                        0f,
                        true),
                    EvaluateMotionSignal(
                        CameraMotionSignalMode.Sin,
                        time,
                        screenPositionFrequency.y,
                        screenPositionPhaseDeg.y * Mathf.Deg2Rad,
                        screenPositionPerlinOffset.y,
                        17f,
                        true));
            }

            if (useScreenPositionPerlinWobble)
            {
                signal += new Vector2(
                    EvaluateMotionSignal(
                        CameraMotionSignalMode.PerlinNoise,
                        time,
                        screenPositionFrequency.x,
                        screenPositionPhaseDeg.x * Mathf.Deg2Rad,
                        screenPositionPerlinOffset.x,
                        0f,
                        true),
                    EvaluateMotionSignal(
                        CameraMotionSignalMode.PerlinNoise,
                        time,
                        screenPositionFrequency.y,
                        screenPositionPhaseDeg.y * Mathf.Deg2Rad,
                        screenPositionPerlinOffset.y,
                        17f,
                        true));
            }

            Vector2 output = screenPositionBase + Vector2.Scale(screenPositionAmplitude, signal) * intensity;
            output.x = Mathf.Clamp01(output.x);
            output.y = Mathf.Clamp01(output.y);

            ApplyScreenPosition(output);

#if UNITY_EDITOR
            screenPositionEvaluatedTimeDebug = (float)time;
            screenPositionOutputDebug = output;
#endif
        }

        private bool TryResolveScreenDriver()
        {
            ResolveStageCameraReference();

            if (stageVirtualCamera == null)
                return false;

            if (cachedComposer == null)
                cachedComposer = stageVirtualCamera.GetCinemachineComponent<CinemachineComposer>();

            if (cachedFramingTransposer == null)
                cachedFramingTransposer = stageVirtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();

            return cachedComposer != null || cachedFramingTransposer != null;
        }

        private void ApplyScreenPosition(Vector2 screenPosition)
        {
            if (cachedComposer != null)
            {
                cachedComposer.m_ScreenX = screenPosition.x;
                cachedComposer.m_ScreenY = screenPosition.y;
                return;
            }

            if (cachedFramingTransposer != null)
            {
                cachedFramingTransposer.m_ScreenX = screenPosition.x;
                cachedFramingTransposer.m_ScreenY = screenPosition.y;
            }
        }

        private bool TryReadCurrentScreenPosition(out Vector2 screenPosition)
        {
            screenPosition = screenPositionBase;

            if (!TryResolveScreenDriver())
                return false;

            if (cachedComposer != null)
            {
                screenPosition = new Vector2(cachedComposer.m_ScreenX, cachedComposer.m_ScreenY);
                return true;
            }

            if (cachedFramingTransposer != null)
            {
                screenPosition = new Vector2(cachedFramingTransposer.m_ScreenX, cachedFramingTransposer.m_ScreenY);
                return true;
            }

            return false;
        }

        private void RestoreScreenPositionBase()
        {
            if (!TryResolveScreenDriver())
                return;

            ApplyScreenPosition(screenPositionBase);

#if UNITY_EDITOR
            screenPositionOutputDebug = screenPositionBase;
#endif
        }

        [ContextMenu("Apply Current Screen Position Once")]
        private void ApplyCurrentScreenPositionOnce()
        {
            if (!enableScreenPositionModule)
                return;

            DriveScreenPosition();
        }

        [ContextMenu("Record Current Screen Position As Base")]
        private void RecordCurrentScreenPositionAsBase()
        {
            if (TryReadCurrentScreenPosition(out Vector2 currentScreenPosition))
            {
                screenPositionBase = currentScreenPosition;
            }
        }

        // ============================================================
        // Dutch Roll
        // ============================================================

        private void DriveDutchRoll()
        {
            if (!Application.isPlaying && !previewDutchRollInEditMode)
                return;

            ResolveStageCameraReference();

            if (stageVirtualCamera == null)
                return;

            double time = GetMotionEvaluatedTime(
                dutchRollDirector,
                useDirectorTimeForDutchRoll,
                dutchRollTimeOffset,
                dutchRollTimeScalePrimary,
                dutchRollTimeScaleSecondary);

            float signal = EvaluateMotionSignal(
                dutchRollMotionMode,
                time,
                dutchRollFrequency,
                dutchRollPhaseDeg * Mathf.Deg2Rad,
                dutchRollPerlinOffset,
                0f,
                true);

            float intensity = dutchRollIntensityScalePrimary * dutchRollIntensityScaleSecondary;
            float dutchDegrees = dutchRollBaseDegrees + dutchRollAmplitudeDegrees * signal * intensity;

            LensSettings lens = stageVirtualCamera.m_Lens;
            lens.Dutch = dutchDegrees;
            stageVirtualCamera.m_Lens = lens;

#if UNITY_EDITOR
            dutchRollEvaluatedTimeDebug = (float)time;
            dutchRollOutputDegreesDebug = dutchDegrees;
#endif
        }

        private void RestoreDutchRollBase()
        {
            ResolveStageCameraReference();

            if (stageVirtualCamera == null)
                return;

            LensSettings lens = stageVirtualCamera.m_Lens;
            lens.Dutch = dutchRollBaseDegrees;
            stageVirtualCamera.m_Lens = lens;

#if UNITY_EDITOR
            dutchRollOutputDegreesDebug = dutchRollBaseDegrees;
#endif
        }

        [ContextMenu("Apply Current Dutch Roll Once")]
        private void ApplyCurrentDutchRollOnce()
        {
            if (!enableDutchRollModule)
                return;

            DriveDutchRoll();
        }

        [ContextMenu("Record Current Dutch As Base")]
        private void RecordCurrentDutchAsBase()
        {
            ResolveStageCameraReference();

            if (stageVirtualCamera == null)
                return;

            dutchRollBaseDegrees = stageVirtualCamera.m_Lens.Dutch;
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
            float signal = EvaluateMotionSignal(
                breathingZoomMotionMode,
                time,
                breathingZoomFrequencyHz,
                0f,
                breathingZoomPerlinOffset,
                0f,
                true);

            return center + amplitude * signal;
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

        private void CaptureFovBeforeModules()
        {
            if (fovBeforeModulesCaptured)
                return;

            ResolveStageCameraReference();

            if (stageVirtualCamera == null)
                return;

            fovBeforeModules = stageVirtualCamera.m_Lens.FieldOfView;
            fovBeforeModulesCaptured = true;
        }

        private void RestoreFovBeforeModules()
        {
            ResolveStageCameraReference();

            if (stageVirtualCamera == null || !fovBeforeModulesCaptured)
                return;

            LensSettings lens = stageVirtualCamera.m_Lens;
            lens.FieldOfView = fovBeforeModules;
            stageVirtualCamera.m_Lens = lens;

            fovBeforeModulesCaptured = false;
            accentZoomOutputFov = 0f;
            accentZoomVelocityFov = 0f;
            accentZoomPreviousEvalTime = double.NaN;
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

        private void CaptureRigDriftPoseBeforeModule()
        {
            if (rigDriftPoseBeforeModuleCaptured)
                return;

            ResolveRigDriftTarget();

            if (resolvedDriftRigTarget == null)
                return;

            rigDriftPositionBeforeModule = resolvedDriftRigTarget.position;
            rigDriftLocalPositionBeforeModule = resolvedDriftRigTarget.localPosition;
            rigDriftSpaceBeforeModule = driftSpace;
            rigDriftPoseBeforeModuleCaptured = true;
        }

        private void RestoreRigDriftPoseBeforeModule()
        {
            if (!rigDriftPoseBeforeModuleCaptured)
                return;

            ResolveRigDriftTarget();

            if (resolvedDriftRigTarget != null)
            {
                if (rigDriftSpaceBeforeModule == CameraRigSpace.Global)
                    resolvedDriftRigTarget.position = rigDriftPositionBeforeModule;
                else
                    resolvedDriftRigTarget.localPosition = rigDriftLocalPositionBeforeModule;
            }

            rigDriftPoseBeforeModuleCaptured = false;
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
            float signalX = EvaluateMotionSignal(
                rigDriftMotionMode,
                rigDriftTime,
                driftFrequency.x,
                driftPhaseOffset.x,
                driftPerlinOffset.x,
                0f,
                false);
            float signalY = EvaluateMotionSignal(
                rigDriftMotionMode,
                useFigureEightDrift ? rigDriftTime * 2f : rigDriftTime,
                driftFrequency.y,
                driftPhaseOffset.y,
                driftPerlinOffset.y,
                17f,
                false);
            float signalZ = EvaluateMotionSignal(
                rigDriftMotionMode,
                rigDriftTime,
                driftFrequency.z,
                driftPhaseOffset.z,
                driftPerlinOffset.z,
                31f,
                false);

            float moveX = RemapValue(signalX * driftAmplitude.x * driftAxisWeight.x, -1f, 1f, driftRangeMin.x, driftRangeMax.x);
            float moveY = RemapValue(signalY * driftAmplitude.y * driftAxisWeight.y, -1f, 1f, driftRangeMin.y, driftRangeMax.y);
            float moveZ = RemapValue(signalZ * driftAmplitude.z * driftAxisWeight.z, -1f, 1f, driftRangeMin.z, driftRangeMax.z);

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

        private void ResetAccentZoomRuntimeState()
        {
            ResolveAccentZoomBaseFov();
            ResetAccentZoomOutputToBase();
            accentZoomPreviousEvalTime = double.NaN;
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
            bool initialized = false;

            if (cachedBodyTransposer != null)
            {
                dollyBodyOffsetInitialValue = cachedBodyTransposer.m_FollowOffset;
                if (dollyBodyOffsetBase == Vector3.zero)
                    dollyBodyOffsetBase = dollyBodyOffsetInitialValue;
                initialized = true;
            }
            else if (cachedFramingTransposer != null)
            {
                dollyBodyOffsetInitialValue = cachedFramingTransposer.m_TrackedObjectOffset;
                if (dollyBodyOffsetBase == Vector3.zero)
                    dollyBodyOffsetBase = dollyBodyOffsetInitialValue;
                initialized = true;
            }

            dollyBodyOffsetInitialized = initialized;
        }

        private void ActivateDollyBodyOffsetModule()
        {
            CacheBodyDriverComponents();

            if (!dollyBodyOffsetInitialized)
                InitializeDollyBodyOffsetBase();

            DriveDollyBodyOffset();
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

            if (!dollyBodyOffsetInitialized)
                InitializeDollyBodyOffsetBase();

            PlayableDirector director = ResolveDirector(dollyBodyOffsetDirector);

            float t = director
                ? (float)director.time * dollyBodyOffsetTimeScalePrimary * dollyBodyOffsetTimeScaleSecondary
                : Time.time * dollyBodyOffsetTimeScalePrimary * dollyBodyOffsetTimeScaleSecondary;

            Vector3 rad = dollyBodyOffsetPhaseDeg * Mathf.Deg2Rad;
            Vector3 wobble = new Vector3(
                dollyBodyOffsetAmplitude.x * EvaluateMotionSignal(
                    dollyBodyOffsetMotionMode,
                    t,
                    dollyBodyOffsetFrequency.x,
                    rad.x,
                    dollyBodyOffsetPerlinOffset.x,
                    0f,
                    true),
                dollyBodyOffsetAmplitude.y * EvaluateMotionSignal(
                    dollyBodyOffsetMotionMode,
                    t,
                    dollyBodyOffsetFrequency.y,
                    rad.y,
                    dollyBodyOffsetPerlinOffset.y,
                    17f,
                    true),
                dollyBodyOffsetAmplitude.z * EvaluateMotionSignal(
                    dollyBodyOffsetMotionMode,
                    t,
                    dollyBodyOffsetFrequency.z,
                    rad.z,
                    dollyBodyOffsetPerlinOffset.z,
                    31f,
                    true)
            );

            Vector3 target = dollyBodyOffsetBase + wobble;

            if (cachedBodyTransposer != null)
                cachedBodyTransposer.m_FollowOffset = target;
            else if (cachedFramingTransposer != null)
                cachedFramingTransposer.m_TrackedObjectOffset = target;
        }

        private void RestoreInitialDollyOffsetRuntime()
        {
            if (!dollyBodyOffsetInitialized)
                return;

            if (cachedBodyTransposer == null && cachedFramingTransposer == null)
                CacheBodyDriverComponents();

            if (cachedBodyTransposer != null)
                cachedBodyTransposer.m_FollowOffset = dollyBodyOffsetInitialValue;
            else if (cachedFramingTransposer != null)
                cachedFramingTransposer.m_TrackedObjectOffset = dollyBodyOffsetInitialValue;
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
            RestoreInitialDollyOffsetRuntime();
        }
#endif
    }
}
