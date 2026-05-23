#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Cinemachine;

namespace toshi.VLiveKit.Photography.Editor
{
    public partial class VLiveCameraEditor
    {
        private void DrawSharedReferencesSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            DrawSafeProperty(autoBindPlayableDirectorFromTimeTable, "Auto Bind From TimeTable");
            DrawSafeProperty(liveTimelineSectionName, "TimeTable Section");
            DrawSafeProperty(sharedPlayableDirector, "Shared PlayableDirector");
            DrawSafeProperty(sharedLookTargetRig, "Shared VLiveLookTargetRig");

            if (GUILayout.Button("Resolve Director From TimeTable"))
            {
                CallPublic("ResolvePlayableDirectorFromTimeTable");
                serializedObject.Update();
            }

            DrawSafeStateLine("Director", sharedPlayableDirector);
            DrawSafeStateLine("Look Target Rig", sharedLookTargetRig);

            EditorGUILayout.EndVertical();
        }

        private void DrawStageCameraSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            DrawSafeProperty(stageVirtualCamera, "Stage Virtual Camera");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Find In Children"))
                {
                    InvokeNonPublic(target, "FindStageVirtualCameraInChildren");
                    serializedObject.Update();
                }

                GUI.enabled = stageVirtualCamera != null && stageVirtualCamera.objectReferenceValue != null;
                if (GUILayout.Button("Ping Camera"))
                {
                    EditorGUIUtility.PingObject(stageVirtualCamera.objectReferenceValue);
                }
                GUI.enabled = true;
            }

            DrawSafeStateLine("Virtual Camera", stageVirtualCamera, "Bound", "Missing");

            EditorGUILayout.EndVertical();
        }

        private void DrawLookTargetSectionBody()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            using (new EditorGUI.DisabledScope(enableLookTargetModule == null || !enableLookTargetModule.boolValue))
            {
                DrawSafeProperty(assignLookTargetOnStart, "Assign On Start");
                DrawSafeProperty(lookTargetMode, "Target Mode");

                if (IsTargetModeDirect(lookTargetMode))
                {
                    DrawSafeProperty(lookTargetTransform, "Target Transform");
                }
                else
                {
                    DrawSafeProperty(lookTargetRig, "VLiveLookTargetRig Override");
                    DrawBonePicker(lookTargetBone, "Aim Bone");
                }

                DrawSafeProperty(lookTargetMarker, "Resolved Target");

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Assign Look"))
                    {
                        CallPublic("AssignLookTarget");
                        serializedObject.Update();
                    }

                    GUI.enabled = lookTargetMarker != null && lookTargetMarker.objectReferenceValue != null;
                    if (GUILayout.Button("Ping Resolved"))
                    {
                        EditorGUIUtility.PingObject(lookTargetMarker.objectReferenceValue);
                    }
                    GUI.enabled = true;
                }

                if (IsTargetModeDirect(lookTargetMode))
                {
                    DrawSafeStateLine("Direct Target", lookTargetTransform, null, "Missing");
                }
                else
                {
                    DrawSafeStateLine("Rig Override", lookTargetRig, null, "Use Shared");
                }

                DrawSafeStateLine("Resolved", lookTargetMarker, null, "Unresolved");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFollowTargetSectionBody()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            using (new EditorGUI.DisabledScope(enableFollowTargetModule == null || !enableFollowTargetModule.boolValue))
            {
                DrawSafeProperty(assignFollowTargetOnStart, "Assign On Start");
                DrawSafeProperty(followTargetMode, "Target Mode");

                if (IsTargetModeDirect(followTargetMode))
                {
                    DrawSafeProperty(followTargetTransform, "Target Transform");
                }
                else
                {
                    DrawSafeProperty(followTargetRig, "VLiveLookTargetRig Override");
                    DrawBonePicker(followTargetBone, "Follow Bone");
                }

                DrawSafeProperty(followTargetMarker, "Resolved Target");

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Assign Follow"))
                    {
                        CallPublic("AssignFollowTarget");
                        serializedObject.Update();
                    }

                    GUI.enabled = followTargetMarker != null && followTargetMarker.objectReferenceValue != null;
                    if (GUILayout.Button("Ping Resolved"))
                    {
                        EditorGUIUtility.PingObject(followTargetMarker.objectReferenceValue);
                    }
                    GUI.enabled = true;
                }

                if (IsTargetModeDirect(followTargetMode))
                {
                    DrawSafeStateLine("Direct Target", followTargetTransform, null, "Missing");
                }
                else
                {
                    DrawSafeStateLine("Rig Override", followTargetRig, null, "Use Shared");
                }

                DrawSafeStateLine("Resolved", followTargetMarker, null, "Unresolved");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawScreenPositionSectionBody()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            using (new EditorGUI.DisabledScope(enableScreenPositionModule == null || !enableScreenPositionModule.boolValue))
            {
                DrawSafeProperty(screenPositionDirector, "Director Override");
                DrawSafeProperty(useDirectorTimeForScreenPosition, "Use Director Time");
                DrawSafeProperty(useScreenPositionSinWobble, "Use Sin Wobble");
                DrawSafeProperty(useScreenPositionPerlinWobble, "Use Perlin Noise Wobble");
                DrawSafeProperty(screenPositionTimeOffset, "Time Offset");
                DrawDualField(screenPositionTimeScalePrimary, screenPositionTimeScaleSecondary, "Time Scale");
                DrawDualField(screenPositionIntensityScalePrimary, screenPositionIntensityScaleSecondary, "Intensity Scale");
                DrawSafeProperty(screenPositionBase, "Base");
                DrawSafeProperty(screenPositionAmplitude, "Amplitude");
                DrawSafeProperty(screenPositionFrequency, "Frequency");

                if (useScreenPositionSinWobble != null && useScreenPositionSinWobble.boolValue)
                {
                    DrawSafeProperty(screenPositionPhaseDeg, "Sin Phase Deg");
                }

                if (useScreenPositionPerlinWobble != null && useScreenPositionPerlinWobble.boolValue)
                {
                    DrawSafeProperty(screenPositionPerlinOffset, "Perlin Offset");
                }

                DrawSafeProperty(previewScreenPositionInEditMode, "Preview In Edit Mode");

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Preview"))
                    {
                        InvokeNonPublic(target, "ApplyCurrentScreenPositionOnce");
                    }

                    if (GUILayout.Button("Record Current As Base"))
                    {
                        InvokeNonPublic(target, "RecordCurrentScreenPositionAsBase");
                    }
                }

                using (new EditorGUI.DisabledScope(true))
                {
                    DrawSafeProperty(screenPositionEvaluatedTimeDebug, "Eval Time");
                    DrawSafeProperty(screenPositionOutputDebug, "Output");
                }

                DrawSafeStateLine("Director Override", screenPositionDirector, null, "Use Shared/TimeTable");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDutchRollSectionBody()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            using (new EditorGUI.DisabledScope(enableDutchRollModule == null || !enableDutchRollModule.boolValue))
            {
                DrawSafeProperty(dutchRollDirector, "Director Override");
                DrawSafeProperty(useDirectorTimeForDutchRoll, "Use Director Time");
                DrawSafeProperty(dutchRollMotionMode, "Motion Mode");
                DrawSafeProperty(dutchRollTimeOffset, "Time Offset");
                DrawDualField(dutchRollTimeScalePrimary, dutchRollTimeScaleSecondary, "Time Scale");
                DrawDualField(dutchRollIntensityScalePrimary, dutchRollIntensityScaleSecondary, "Intensity Scale");
                DrawSafeProperty(dutchRollBaseDegrees, "Base Degrees");
                DrawSafeProperty(dutchRollAmplitudeDegrees, "Amplitude Degrees");
                DrawSafeProperty(dutchRollFrequency, "Frequency");

                if (IsMotionModePerlin(dutchRollMotionMode))
                    DrawSafeProperty(dutchRollPerlinOffset, "Perlin Offset");
                else
                    DrawSafeProperty(dutchRollPhaseDeg, "Phase Deg");

                DrawSafeProperty(previewDutchRollInEditMode, "Preview In Edit Mode");

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Preview"))
                    {
                        InvokeNonPublic(target, "ApplyCurrentDutchRollOnce");
                    }

                    if (GUILayout.Button("Record Current As Base"))
                    {
                        InvokeNonPublic(target, "RecordCurrentDutchAsBase");
                    }
                }

                using (new EditorGUI.DisabledScope(true))
                {
                    DrawSafeProperty(dutchRollEvaluatedTimeDebug, "Eval Time");
                    DrawSafeProperty(dutchRollOutputDegreesDebug, "Output Degrees");
                }

                DrawSafeStateLine("Director Override", dutchRollDirector, null, "Use Shared/TimeTable");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBreathingZoomSectionBody()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            using (new EditorGUI.DisabledScope(enableBreathingZoomModule == null || !enableBreathingZoomModule.boolValue))
            {
                DrawSafeProperty(breathingZoomDirector, "Director Override");
                DrawSafeProperty(useDirectorTimeForBreathingZoom, "Use Director Time");
                DrawSafeProperty(breathingZoomMotionMode, "Motion Mode");
                DrawSafeProperty(breathingZoomTimeOffset, "Time Offset");
                DrawDualField(breathingZoomTimeScalePrimary, breathingZoomTimeScaleSecondary, "Time Scale");
                DrawSafeProperty(breathingZoomFovMin, "FOV Min");
                DrawSafeProperty(breathingZoomFovMax, "FOV Max");
                DrawSafeProperty(breathingZoomFrequencyHz, "Frequency Hz");
                if (IsMotionModePerlin(breathingZoomMotionMode))
                    DrawSafeProperty(breathingZoomPerlinOffset, "Perlin Offset");

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Preview"))
                    {
                        InvokeNonPublic(target, "ApplyCurrentBreathingZoomOnce");
                    }

                    if (breathingZoomEvaluatedTimeDebug != null)
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.PropertyField(breathingZoomEvaluatedTimeDebug, GUIContent.none);
                        }
                    }
                }

                DrawSafeStateLine("Director Override", breathingZoomDirector, null, "Use Shared/TimeTable");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRigDriftSectionBody()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            using (new EditorGUI.DisabledScope(enableRigDriftModule == null || !enableRigDriftModule.boolValue))
            {
                DrawSafeProperty(driftRigTarget, "Rig Target");
                DrawSafeProperty(driftSpace, "Space");
                DrawSafeProperty(syncRigDriftToDirector, "Sync To Director");
                DrawSafeProperty(rigDriftDirector, "Director Override");
                DrawDualField(rigDriftTimeScalePrimary, rigDriftTimeScaleSecondary, "Time Scale");
                DrawSafeProperty(rigDriftMotionMode, "Motion Mode");
                DrawSafeProperty(driftAxisWeight, "Axis Weight");
                DrawSafeProperty(driftFrequency, "Frequency");
                DrawSafeProperty(driftAmplitude, "Amplitude");
                DrawSafeProperty(driftPhaseOffset, "Phase Offset");
                if (IsMotionModePerlin(rigDriftMotionMode))
                    DrawSafeProperty(driftPerlinOffset, "Perlin Offset");
                DrawSafeProperty(driftRangeMin, "Range Min");
                DrawSafeProperty(driftRangeMax, "Range Max");
                DrawSafeProperty(rigDriftOffset, "Rig Offset");
                DrawSafeProperty(useFigureEightDrift, "Figure Eight");

                if (GUILayout.Button("Preview"))
                {
                    InvokeNonPublic(target, "ApplyCurrentRigDriftOnce");
                }

                DrawSafeStateLine("Director Override", rigDriftDirector, null, "Use Shared/TimeTable");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAccentZoomSectionBody()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            using (new EditorGUI.DisabledScope(enableAccentZoomModule == null || !enableAccentZoomModule.boolValue))
            {
                DrawSafeProperty(accentZoomDirector, "Director Override");
                DrawSafeProperty(accentZoomFovMin, "FOV Min");
                DrawSafeProperty(accentZoomFovMax, "FOV Max");
                DrawSafeProperty(accentZoomBaseFov, "Base FOV");
                DrawSafeProperty(accentZoomAmountMin, "Zoom Amount Min");
                DrawSafeProperty(accentZoomAmountMax, "Zoom Amount Max");
                DrawSafeProperty(accentZoomSeed, "Seed");
                DrawSafeProperty(accentAttack, "Attack");
                DrawSafeProperty(accentAttackSharpness, "Attack Sharpness");
                DrawSafeProperty(accentHoldZoom, "Hold Zoom");
                DrawSafeProperty(accentReturnDuration, "Return Duration");
                DrawSafeProperty(accentReturnEpsilon, "Return Epsilon");
                DrawSafeProperty(accentHoldBase, "Hold Base");
                DrawSafeProperty(accentStartOffset, "Start Offset");
                DrawSafeProperty(freezeAccentZoomWhenDirectorStopped, "Freeze When Stopped");
                DrawDualField(accentZoomTimeScalePrimary, accentZoomTimeScaleSecondary, "Time Scale");
                DrawSafeProperty(accentZoomFilterMode, "Filter Mode");

                if (accentZoomFilterMode != null)
                {
                    var mode = (VLiveCamera.AccentZoomFilterMode)accentZoomFilterMode.enumValueIndex;
                    if (mode == VLiveCamera.AccentZoomFilterMode.ExponentialLowPass)
                    {
                        DrawSafeProperty(accentLowPassTimeConstant, "LowPass Time");
                    }
                    else if (mode == VLiveCamera.AccentZoomFilterMode.DampedSpring)
                    {
                        DrawSafeProperty(accentSpringFrequency, "Spring Freq");
                        DrawSafeProperty(accentSpringDampingRatio, "Spring Damping");
                    }
                }

                DrawSafeProperty(resetAccentOnLargeJump, "Reset On Large Jump");
                if (resetAccentOnLargeJump != null && resetAccentOnLargeJump.boolValue)
                {
                    DrawSafeProperty(accentJumpThreshold, "Jump Threshold");
                }

                DrawSafeStateLine("Director Override", accentZoomDirector, null, "Use Shared/TimeTable");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDollyOffsetSectionBody()
        {
            EditorGUILayout.BeginVertical(boxStyle);

            using (new EditorGUI.DisabledScope(enableDollyBodyOffsetModule == null || !enableDollyBodyOffsetModule.boolValue))
            {
                DrawSafeProperty(dollyBodyOffsetDirector, "Director Override");
                DrawDualField(dollyBodyOffsetTimeScalePrimary, dollyBodyOffsetTimeScaleSecondary, "Time Scale");
                DrawSafeProperty(dollyBodyOffsetBase, "Base Offset");
                DrawSafeProperty(dollyBodyOffsetAmplitude, "Amplitude");
                DrawSafeProperty(dollyBodyOffsetFrequency, "Frequency");
                DrawSafeProperty(dollyBodyOffsetMotionMode, "Motion Mode");
                DrawSafeProperty(dollyBodyOffsetPhaseDeg, "Phase Deg");
                if (IsMotionModePerlin(dollyBodyOffsetMotionMode))
                    DrawSafeProperty(dollyBodyOffsetPerlinOffset, "Perlin Offset");
                DrawSafeProperty(previewDollyOffsetInEditMode, "Preview In Edit Mode");

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Record Current As Base"))
                    {
                        InvokeNonPublic(target, "RecordCurrentDollyOffsetAsBase");
                    }

                    if (GUILayout.Button("Restore Initial"))
                    {
                        InvokeNonPublic(target, "RestoreInitialDollyOffset");
                    }
                }

                DrawSafeStateLine("Director Override", dollyBodyOffsetDirector, null, "Use Shared/TimeTable");
            }

            EditorGUILayout.EndVertical();
        }

        private bool DrawModuleHeader(bool state, string title, SerializedProperty enabledProperty)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 22f);

            Rect foldRect = new Rect(rect.x, rect.y, rect.width - 78f, rect.height);
            Rect toggleRect = new Rect(rect.xMax - 70f, rect.y + 1f, 70f, rect.height);

            state = EditorGUI.Foldout(foldRect, state, title, true, foldoutStyle);

            if (enabledProperty != null)
            {
                EditorGUI.showMixedValue = enabledProperty.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                bool enabled = EditorGUI.ToggleLeft(toggleRect, "On", enabledProperty.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    enabledProperty.boolValue = enabled;
                }
                EditorGUI.showMixedValue = false;
            }

            return state;
        }

        private void DrawDualField(SerializedProperty a, SerializedProperty b, string label)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (a != null)
                    EditorGUILayout.PropertyField(a, new GUIContent("Primary"));
                else
                    EditorGUILayout.HelpBox($"Property not found: {label} / Primary", MessageType.Error);

                if (b != null)
                    EditorGUILayout.PropertyField(b, new GUIContent("Secondary"));
                else
                    EditorGUILayout.HelpBox($"Property not found: {label} / Secondary", MessageType.Error);
            }
        }

        private void DrawStateLine(string label, bool ok, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(110f));
                Color prev = GUI.color;
                GUI.color = ok ? StatusOk : StatusWarn;
                GUILayout.Label("●", GUILayout.Width(16f));
                GUI.color = prev;
                EditorGUILayout.LabelField(value);
            }
        }

        private void DrawSafeProperty(SerializedProperty prop, string label)
        {
            if (prop == null)
            {
                EditorGUILayout.HelpBox($"Property not found: {label}", MessageType.Error);
                return;
            }

            EditorGUILayout.PropertyField(prop, new GUIContent(label));
        }

        private bool IsTargetModeDirect(SerializedProperty modeProperty)
        {
            return modeProperty != null
                && modeProperty.enumValueIndex == (int)VLiveCamera.TargetReferenceMode.DirectTransform;
        }

        private bool IsMotionModePerlin(SerializedProperty modeProperty)
        {
            return modeProperty != null
                && modeProperty.enumValueIndex == (int)VLiveCamera.CameraMotionSignalMode.PerlinNoise;
        }

        private void DrawSafeStateLine(string label, SerializedProperty prop, string presentText = null, string missingText = "None")
        {
            if (prop == null)
            {
                DrawStateLine(label, false, "Property Missing");
                return;
            }

            bool hasValue = prop.objectReferenceValue != null;
            string valueText = hasValue
                ? (string.IsNullOrEmpty(presentText) ? prop.objectReferenceValue.name : presentText)
                : missingText;

            DrawStateLine(label, hasValue, valueText);
        }
    }
}
#endif
