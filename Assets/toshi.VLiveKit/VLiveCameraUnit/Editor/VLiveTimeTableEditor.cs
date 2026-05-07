#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using toshi.VLiveKit;

namespace toshi.VLiveKit.Editor
{
    [CustomEditor(typeof(VLiveTimeTable))]
    public class VLiveTimeTableEditor : UnityEditor.Editor
    {
        private enum TimelineDisplayMode
        {
            Frames = 0,
            Timecode = 1,
            Seconds = 2
        }

        private readonly List<PlayableDirector> assignedDirectors = new List<PlayableDirector>();
        private double bulkFrameRate = 30.0;
        private TimelineDisplayMode displayMode = TimelineDisplayMode.Timecode;

        private void OnEnable()
        {
            RefreshAssignedDirectors();

            if (TryGetFirstTimelineFrameRate(out double frameRate))
            {
                bulkFrameRate = frameRate;
            }

            displayMode = GetTimelineDisplayMode(displayMode);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            DrawCollectionTools();

            EditorGUILayout.Space();
            DrawBulkTimelineSettings();
        }

        private void DrawCollectionTools()
        {
            EditorGUILayout.LabelField("Playable Registration", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Assign Attached Director"))
                {
                    ApplyToTargets("Assign Attached Director As Master", timeTable =>
                    {
                        timeTable.AssignAttachedDirectorAsMasterIfNeeded();
                        timeTable.RebuildMap();
                    });
                }

                if (GUILayout.Button("Auto Collect Directors"))
                {
                    ApplyToTargets("Auto Collect VLive Timelines", timeTable => timeTable.AutoCollectChildDirectors());
                }
            }

            RefreshAssignedDirectors();
            EditorGUILayout.LabelField("Assigned Directors", assignedDirectors.Count.ToString());
        }

        private void DrawBulkTimelineSettings()
        {
            EditorGUILayout.LabelField("Bulk Timeline Settings", EditorStyles.boldLabel);

            bulkFrameRate = Math.Max(0.000001, EditorGUILayout.DoubleField("Frame Rate", bulkFrameRate));
            displayMode = (TimelineDisplayMode)EditorGUILayout.EnumPopup("Timeline Display", displayMode);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Frame Rate"))
                {
                    ApplyFrameRateToAssignedTimelines();
                }

                if (GUILayout.Button("Apply Display"))
                {
                    SetTimelineDisplayMode(displayMode);
                }
            }

            if (GUILayout.Button("Apply Frame Rate + Display"))
            {
                ApplyFrameRateToAssignedTimelines();
                SetTimelineDisplayMode(displayMode);
            }
        }

        private void ApplyFrameRateToAssignedTimelines()
        {
            RefreshAssignedDirectors();

            int changedCount = 0;
            HashSet<UnityEngine.Object> changedAssets = new HashSet<UnityEngine.Object>();

            for (int i = 0; i < assignedDirectors.Count; i++)
            {
                UnityEngine.Object timelineAsset = GetTimelineAsset(assignedDirectors[i]);
                if (timelineAsset == null || changedAssets.Contains(timelineAsset))
                {
                    continue;
                }

                Undo.RecordObject(timelineAsset, "Apply Timeline Frame Rate");
                if (SetTimelineFrameRate(timelineAsset, bulkFrameRate))
                {
                    changedAssets.Add(timelineAsset);
                    EditorUtility.SetDirty(timelineAsset);
                    changedCount++;
                }
            }

            if (changedCount > 0)
            {
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[VLiveTimeTable] Applied frame rate {bulkFrameRate:0.###} to {changedCount} timeline asset(s).", target);
        }

        private void RefreshAssignedDirectors()
        {
            assignedDirectors.Clear();

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] is VLiveTimeTable timeTable)
                {
                    timeTable.CollectAssignedDirectors(assignedDirectors);
                }
            }
        }

        private bool TryGetFirstTimelineFrameRate(out double frameRate)
        {
            frameRate = 0.0;

            for (int i = 0; i < assignedDirectors.Count; i++)
            {
                UnityEngine.Object timelineAsset = GetTimelineAsset(assignedDirectors[i]);
                if (timelineAsset != null && TryGetTimelineFrameRate(timelineAsset, out frameRate))
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyToTargets(string undoName, Action<VLiveTimeTable> action)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (!(targets[i] is VLiveTimeTable timeTable))
                {
                    continue;
                }

                Undo.RecordObject(timeTable, undoName);
                action(timeTable);
                EditorUtility.SetDirty(timeTable);
            }

            serializedObject.Update();
            RefreshAssignedDirectors();
        }

        private static UnityEngine.Object GetTimelineAsset(PlayableDirector director)
        {
            if (director == null)
            {
                return null;
            }

            PlayableAsset playableAsset = director.playableAsset;
            if (playableAsset == null || playableAsset.GetType().FullName != "UnityEngine.Timeline.TimelineAsset")
            {
                return null;
            }

            return playableAsset as UnityEngine.Object;
        }

        private static bool TryGetTimelineFrameRate(UnityEngine.Object timelineAsset, out double frameRate)
        {
            frameRate = 0.0;

            object editorSettings = GetTimelineEditorSettings(timelineAsset);
            PropertyInfo frameRateProperty = editorSettings?.GetType().GetProperty("frameRate", BindingFlags.Instance | BindingFlags.Public);
            if (frameRateProperty == null)
            {
                return false;
            }

            frameRate = Convert.ToDouble(frameRateProperty.GetValue(editorSettings));
            return true;
        }

        private static bool SetTimelineFrameRate(UnityEngine.Object timelineAsset, double frameRate)
        {
            object editorSettings = GetTimelineEditorSettings(timelineAsset);
            PropertyInfo frameRateProperty = editorSettings?.GetType().GetProperty("frameRate", BindingFlags.Instance | BindingFlags.Public);
            if (frameRateProperty == null || !frameRateProperty.CanWrite)
            {
                return false;
            }

            frameRateProperty.SetValue(editorSettings, frameRate);
            return true;
        }

        private static object GetTimelineEditorSettings(UnityEngine.Object timelineAsset)
        {
            return timelineAsset != null
                ? timelineAsset.GetType().GetProperty("editorSettings", BindingFlags.Instance | BindingFlags.Public)?.GetValue(timelineAsset)
                : null;
        }

        private static TimelineDisplayMode GetTimelineDisplayMode(TimelineDisplayMode fallback)
        {
            SerializedProperty timeFormatProperty = GetTimelineTimeFormatProperty(out SerializedObject preferences);
            if (timeFormatProperty == null)
            {
                return fallback;
            }

            return (TimelineDisplayMode)Mathf.Clamp(timeFormatProperty.enumValueIndex, 0, 2);
        }

        private static void SetTimelineDisplayMode(TimelineDisplayMode mode)
        {
            SerializedProperty timeFormatProperty = GetTimelineTimeFormatProperty(out SerializedObject preferences);
            if (timeFormatProperty == null)
            {
                Debug.LogWarning("[VLiveTimeTable] Timeline display preferences were not found.");
                return;
            }

            timeFormatProperty.enumValueIndex = (int)mode;
            preferences.ApplyModifiedProperties();

            object timelinePreferences = preferences.targetObject;
            timelinePreferences.GetType().GetMethod("Save", BindingFlags.Instance | BindingFlags.Public)?.Invoke(timelinePreferences, null);
            RefreshTimelineEditor();
        }

        private static SerializedProperty GetTimelineTimeFormatProperty(out SerializedObject preferences)
        {
            preferences = null;

            Type timelinePreferencesType = FindType("TimelinePreferences");
            PropertyInfo instanceProperty = timelinePreferencesType?.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            UnityEngine.Object instance = instanceProperty?.GetValue(null) as UnityEngine.Object;
            if (instance == null)
            {
                return null;
            }

            preferences = new SerializedObject(instance);
            preferences.Update();
            return preferences.FindProperty("timeFormat");
        }

        private static void RefreshTimelineEditor()
        {
            Type timelineEditorType = FindType("UnityEditor.Timeline.TimelineEditor");
            Type refreshReasonType = FindType("UnityEditor.Timeline.RefreshReason");
            MethodInfo refreshMethod = timelineEditorType?.GetMethod("Refresh", BindingFlags.Static | BindingFlags.Public);
            if (refreshMethod == null || refreshReasonType == null)
            {
                return;
            }

            object reason = Enum.Parse(refreshReasonType, "WindowNeedsRedraw");
            refreshMethod.Invoke(null, new[] { reason });
        }

        private static Type FindType(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
#endif
