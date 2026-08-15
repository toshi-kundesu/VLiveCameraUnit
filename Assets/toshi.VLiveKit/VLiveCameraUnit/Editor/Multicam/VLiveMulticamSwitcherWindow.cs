#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

namespace toshi.VLiveKit.Photography.Editor
{
    public class VLiveMulticamSwitcherWindow : EditorWindow
    {
        private VLiveMulticamSwitcher switcher;
        private VLiveMulticamCameraSet cameraSet;
        private PlayableDirector director;
        private Vector2 scroll;
        private string exportTrackName = "VLive Multicam";
        private double exportEndTime;

        [MenuItem("Tools/VLiveKit/Camera/Multicam Switcher")]
        public static void Open()
        {
            VLiveMulticamSwitcherWindow window =
                GetWindow<VLiveMulticamSwitcherWindow>("VLive Multicam");
            window.TryUseSelection();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            TryUseSelection();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnSelectionChange()
        {
            TryUseSelection();
            Repaint();
        }

        private void OnGUI()
        {
            HandleKeyEvents();

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawSetupSection();
            EditorGUILayout.Space(8f);
            DrawTransportSection();
            EditorGUILayout.Space(8f);
            DrawAngleSection();
            EditorGUILayout.Space(8f);
            DrawCutSection();
            EditorGUILayout.Space(8f);
            DrawExportSection();

            EditorGUILayout.EndScrollView();
        }

        private void OnEditorUpdate()
        {
            if (director != null && director.state == PlayState.Playing)
            {
                Repaint();
            }
        }

        private void DrawSetupSection()
        {
            EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            switcher = (VLiveMulticamSwitcher)EditorGUILayout.ObjectField(
                "Switcher",
                switcher,
                typeof(VLiveMulticamSwitcher),
                true);
            if (EditorGUI.EndChangeCheck())
            {
                SyncFromSwitcher();
            }

            EditorGUI.BeginChangeCheck();
            cameraSet = (VLiveMulticamCameraSet)EditorGUILayout.ObjectField(
                "Camera Set",
                cameraSet,
                typeof(VLiveMulticamCameraSet),
                true);
            director = (PlayableDirector)EditorGUILayout.ObjectField(
                "Director",
                director,
                typeof(PlayableDirector),
                true);
            if (EditorGUI.EndChangeCheck())
            {
                SyncToSwitcher();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Use Selection"))
            {
                TryUseSelection(true);
            }

            if (GUILayout.Button("Create On Selection"))
            {
                CreateSetupOnSelection();
            }

            using (new EditorGUI.DisabledScope(cameraSet == null))
            {
                if (GUILayout.Button("Collect Child Vcams"))
                {
                    CollectChildCameras();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTransportSection()
        {
            EditorGUILayout.LabelField("Transport", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(director == null))
            {
                EditorGUILayout.BeginHorizontal();
                double time = director != null ? director.time : 0d;
                EditorGUI.BeginChangeCheck();
                double newTime = EditorGUILayout.DoubleField("Time", time);
                if (EditorGUI.EndChangeCheck() && director != null)
                {
                    Undo.RecordObject(director, "Set Multicam Time");
                    director.time = Math.Max(0d, newTime);
                    director.Evaluate();
                }

                if (GUILayout.Button("Play", GUILayout.Width(56f)))
                {
                    director.Play();
                }

                if (GUILayout.Button("Pause", GUILayout.Width(56f)))
                {
                    director.Pause();
                }

                if (GUILayout.Button("Stop", GUILayout.Width(56f)))
                {
                    director.Stop();
                    director.time = 0d;
                    director.Evaluate();
                }
                EditorGUILayout.EndHorizontal();
            }

            using (new EditorGUI.DisabledScope(switcher == null))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                bool record = EditorGUILayout.ToggleLeft(
                    "Record switches",
                    switcher != null && switcher.RecordSwitches,
                    GUILayout.Width(140f));
                bool runtimeHotkeys = EditorGUILayout.ToggleLeft(
                    "Runtime hotkeys",
                    switcher != null && switcher.EnableRuntimeHotkeys,
                    GUILayout.Width(140f));
                bool playbackCuts = EditorGUILayout.ToggleLeft(
                    "Playback cuts in Play Mode",
                    switcher != null && switcher.PlaybackRecordedCutsInPlayMode);
                if (EditorGUI.EndChangeCheck() && switcher != null)
                {
                    Undo.RecordObject(switcher, "Edit Multicam Switcher");
                    switcher.RecordSwitches = record;
                    switcher.EnableRuntimeHotkeys = runtimeHotkeys;
                    switcher.PlaybackRecordedCutsInPlayMode = playbackCuts;
                    EditorUtility.SetDirty(switcher);
                }
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Apply Recorded Cut At Current Time"))
                {
                    RecordVirtualCameraUndo("Apply Multicam Cut");
                    switcher.ApplyRecordedCutAtTime(GetCurrentTime(), true);
                    MarkVirtualCamerasDirty();
                }
            }
        }

        private void DrawAngleSection()
        {
            EditorGUILayout.LabelField("Angles", EditorStyles.boldLabel);

            if (cameraSet == null)
            {
                EditorGUILayout.HelpBox("Assign or create a VLiveMulticamCameraSet.", MessageType.Info);
                return;
            }

            if (cameraSet.AngleCount == 0)
            {
                EditorGUILayout.HelpBox("No angles. Use Collect Child Vcams or add angles in the Camera Set inspector.", MessageType.Info);
                return;
            }

            int columns = Mathf.Clamp(Mathf.FloorToInt(position.width / 150f), 1, 6);
            int index = 0;
            while (index < cameraSet.AngleCount)
            {
                EditorGUILayout.BeginHorizontal();
                for (int column = 0; column < columns && index < cameraSet.AngleCount; column++, index++)
                {
                    DrawAngleButton(index);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawAngleButton(int index)
        {
            VLiveMulticamAngle angle = cameraSet.GetAngle(index);
            if (angle == null)
            {
                return;
            }

            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = angle.Color;

            string keyLabel = angle.HotKey == KeyCode.None
                ? "-"
                : angle.HotKey.ToString().Replace("Alpha", "");
            string activePrefix = switcher != null && switcher.ActiveAngleIndex == index ? "> " : "";
            string label = $"{activePrefix}{index + 1}. {angle.DisplayName}\n[{keyLabel}]";

            using (new EditorGUI.DisabledScope(angle.VirtualCamera == null || switcher == null))
            {
                if (GUILayout.Button(label, GUILayout.Height(50f)))
                {
                    TriggerSwitch(index);
                }
            }

            GUI.backgroundColor = previous;
        }

        private void DrawCutSection()
        {
            EditorGUILayout.LabelField("Cuts", EditorStyles.boldLabel);

            if (switcher == null)
            {
                EditorGUILayout.HelpBox("Assign or create a VLiveMulticamSwitcher.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Recorded: {switcher.CutCount}", GUILayout.Width(120f));
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(switcher.CutCount == 0))
            {
                if (GUILayout.Button("Clear", GUILayout.Width(72f)) &&
                    EditorUtility.DisplayDialog("Clear Multicam Cuts", "Clear all recorded multicam cuts?", "Clear", "Cancel"))
                {
                    Undo.RecordObject(switcher, "Clear Multicam Cuts");
                    switcher.ClearCuts();
                    EditorUtility.SetDirty(switcher);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (switcher.CutCount == 0)
            {
                EditorGUILayout.HelpBox("Switch angles while Record switches is enabled to create cuts.", MessageType.Info);
                return;
            }

            string[] angleNames = BuildAngleNames();
            for (int i = 0; i < switcher.CutCount; i++)
            {
                VLiveMulticamCut cut = switcher.GetCut(i);
                if (cut == null)
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(TimeSpan.FromSeconds(cut.Time).ToString(@"hh\:mm\:ss\.fff"), GUILayout.Width(96f));

                EditorGUI.BeginChangeCheck();
                double newTime = EditorGUILayout.DoubleField(cut.Time, GUILayout.Width(86f));
                int newAngle = EditorGUILayout.Popup(
                    Mathf.Clamp(cut.AngleIndex, 0, Mathf.Max(0, angleNames.Length - 1)),
                    angleNames,
                    GUILayout.MinWidth(120f));
                float newBlend = EditorGUILayout.FloatField(cut.BlendDuration, GUILayout.Width(56f));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(switcher, "Edit Multicam Cut");
                    cut.Time = newTime;
                    cut.AngleIndex = newAngle;
                    cut.BlendDuration = newBlend;
                    switcher.SortCuts();
                    EditorUtility.SetDirty(switcher);
                }

                if (GUILayout.Button("Go", GUILayout.Width(42f)))
                {
                    GoToCut(cut);
                }

                if (GUILayout.Button("X", GUILayout.Width(24f)))
                {
                    Undo.RecordObject(switcher, "Remove Multicam Cut");
                    switcher.RemoveCutAt(i);
                    EditorUtility.SetDirty(switcher);
                    EditorGUILayout.EndHorizontal();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawExportSection()
        {
            EditorGUILayout.LabelField("Timeline Export", EditorStyles.boldLabel);

            exportTrackName = EditorGUILayout.TextField("Track Name", exportTrackName);
            exportEndTime = Math.Max(0d, EditorGUILayout.DoubleField("End Time Override", exportEndTime));

            using (new EditorGUI.DisabledScope(switcher == null || director == null || switcher.CutCount == 0))
            {
                if (GUILayout.Button("Export To New Cinemachine Track"))
                {
                    try
                    {
                        CinemachineTrack track = VLiveMulticamTimelineExporter.ExportToNewTrack(
                            switcher,
                            exportTrackName,
                            exportEndTime);
                        EditorUtility.DisplayDialog(
                            "Multicam Export",
                            $"Created Timeline track: {track.name}",
                            "OK");
                    }
                    catch (Exception ex)
                    {
                        EditorUtility.DisplayDialog("Multicam Export Failed", ex.Message, "OK");
                    }
                }
            }
        }

        private void HandleKeyEvents()
        {
            Event current = Event.current;
            if (current == null ||
                current.type != EventType.KeyDown ||
                EditorGUIUtility.editingTextField ||
                switcher == null)
            {
                return;
            }

            int angleIndex = FindAngleIndexForKey(current.keyCode);
            if (angleIndex < 0)
            {
                return;
            }

            Undo.RecordObject(switcher, "Record Multicam Cut");
            RecordVirtualCameraUndo("Switch Multicam Angle");

            switcher.SwitchToAngle(angleIndex, GetCurrentTime(), switcher.RecordSwitches);
            current.Use();
            EditorUtility.SetDirty(switcher);
            MarkVirtualCamerasDirty();
            Repaint();
        }

        private void TriggerSwitch(int index)
        {
            if (switcher == null)
            {
                return;
            }

            Undo.RecordObject(switcher, "Record Multicam Cut");
            RecordVirtualCameraUndo("Switch Multicam Angle");

            switcher.SwitchToAngle(index, GetCurrentTime(), switcher.RecordSwitches);

            if (director != null)
            {
                director.Evaluate();
            }

            EditorUtility.SetDirty(switcher);
            MarkVirtualCamerasDirty();
        }

        private void GoToCut(VLiveMulticamCut cut)
        {
            if (director != null)
            {
                Undo.RecordObject(director, "Go To Multicam Cut");
                director.time = cut.Time;
                director.Evaluate();
            }

            RecordVirtualCameraUndo("Preview Multicam Cut");
            switcher.SwitchToAngle(cut.AngleIndex, cut.Time, false);
            MarkVirtualCamerasDirty();
        }

        private double GetCurrentTime()
        {
            if (director != null)
            {
                return director.time;
            }

            return switcher != null ? switcher.GetCurrentTime() : 0d;
        }

        private void TryUseSelection(bool force = false)
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                return;
            }

            VLiveMulticamSwitcher selectedSwitcher =
                selected.GetComponent<VLiveMulticamSwitcher>() ??
                selected.GetComponentInParent<VLiveMulticamSwitcher>() ??
                selected.GetComponentInChildren<VLiveMulticamSwitcher>();

            if (selectedSwitcher != null && (force || switcher == null || selectedSwitcher != switcher))
            {
                switcher = selectedSwitcher;
                SyncFromSwitcher();
                return;
            }

            VLiveMulticamCameraSet selectedSet =
                selected.GetComponent<VLiveMulticamCameraSet>() ??
                selected.GetComponentInParent<VLiveMulticamCameraSet>() ??
                selected.GetComponentInChildren<VLiveMulticamCameraSet>();

            if (selectedSet != null && (force || cameraSet == null || selectedSet != cameraSet))
            {
                cameraSet = selectedSet;
                switcher = selectedSet.GetComponent<VLiveMulticamSwitcher>();
                SyncFromSwitcher();
            }
        }

        private void CreateSetupOnSelection()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("VLive Multicam", "Select a GameObject first.", "OK");
                return;
            }

            Undo.SetCurrentGroupName("Create Multicam Switcher");
            int undoGroup = Undo.GetCurrentGroup();

            cameraSet = selected.GetComponent<VLiveMulticamCameraSet>();
            if (cameraSet == null)
            {
                cameraSet = Undo.AddComponent<VLiveMulticamCameraSet>(selected);
            }

            switcher = selected.GetComponent<VLiveMulticamSwitcher>();
            if (switcher == null)
            {
                switcher = Undo.AddComponent<VLiveMulticamSwitcher>(selected);
            }

            director =
                selected.GetComponent<PlayableDirector>() ??
                selected.GetComponentInParent<PlayableDirector>() ??
                selected.GetComponentInChildren<PlayableDirector>();

            SyncToSwitcher();
            CollectChildCameras();

            Undo.CollapseUndoOperations(undoGroup);
        }

        private void CollectChildCameras()
        {
            if (cameraSet == null)
            {
                return;
            }

            Undo.RecordObject(cameraSet, "Collect Multicam Cameras");
            cameraSet.AutoCollectChildCameras(true);
            EditorUtility.SetDirty(cameraSet);
        }

        private void SyncFromSwitcher()
        {
            if (switcher == null)
            {
                return;
            }

            cameraSet = switcher.CameraSet;
            director = switcher.Director;
        }

        private void SyncToSwitcher()
        {
            if (switcher == null)
            {
                return;
            }

            Undo.RecordObject(switcher, "Edit Multicam References");
            switcher.CameraSet = cameraSet;
            switcher.Director = director;
            EditorUtility.SetDirty(switcher);
        }

        private string[] BuildAngleNames()
        {
            if (cameraSet == null || cameraSet.AngleCount == 0)
            {
                return new[] { "No Angle" };
            }

            string[] names = new string[cameraSet.AngleCount];
            for (int i = 0; i < cameraSet.AngleCount; i++)
            {
                VLiveMulticamAngle angle = cameraSet.GetAngle(i);
                names[i] = angle != null ? $"{i + 1}. {angle.DisplayName}" : $"{i + 1}. Missing";
            }

            return names;
        }

        private int FindAngleIndexForKey(KeyCode keyCode)
        {
            if (cameraSet == null)
            {
                return -1;
            }

            for (int i = 0; i < cameraSet.AngleCount; i++)
            {
                VLiveMulticamAngle angle = cameraSet.GetAngle(i);
                if (angle != null && angle.HotKey == keyCode)
                {
                    return i;
                }
            }

            return -1;
        }

        private UnityEngine.Object[] CollectVirtualCameraObjects()
        {
            if (cameraSet == null)
            {
                return Array.Empty<UnityEngine.Object>();
            }

            List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
            for (int i = 0; i < cameraSet.AngleCount; i++)
            {
                VLiveMulticamAngle angle = cameraSet.GetAngle(i);
                if (angle != null && angle.VirtualCamera != null)
                {
                    objects.Add(angle.VirtualCamera);
                }
            }

            return objects.ToArray();
        }

        private void RecordVirtualCameraUndo(string undoName)
        {
            UnityEngine.Object[] objects = CollectVirtualCameraObjects();
            if (objects.Length > 0)
            {
                Undo.RecordObjects(objects, undoName);
            }
        }

        private void MarkVirtualCamerasDirty()
        {
            if (cameraSet == null)
            {
                return;
            }

            for (int i = 0; i < cameraSet.AngleCount; i++)
            {
                VLiveMulticamAngle angle = cameraSet.GetAngle(i);
                if (angle != null && angle.VirtualCamera != null)
                {
                    EditorUtility.SetDirty(angle.VirtualCamera);
                }
            }
        }
    }
}
#endif
