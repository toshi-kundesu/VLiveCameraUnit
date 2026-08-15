#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace toshi.VLiveKit.Photography.Editor
{
    public static class VLiveMulticamTimelineExporter
    {
        private const double MinimumClipDuration = 1d / 60d;

        public static CinemachineTrack ExportToNewTrack(
            VLiveMulticamSwitcher switcher,
            string trackName,
            double explicitEndTime = 0d)
        {
            if (switcher == null)
            {
                throw new ArgumentNullException(nameof(switcher));
            }

            PlayableDirector director = switcher.Director;
            if (director == null)
            {
                throw new InvalidOperationException("PlayableDirector is not assigned.");
            }

            TimelineAsset timeline = director.playableAsset as TimelineAsset;
            if (timeline == null)
            {
                throw new InvalidOperationException("The assigned PlayableDirector does not use a TimelineAsset.");
            }

            VLiveMulticamCameraSet cameraSet = switcher.CameraSet;
            if (cameraSet == null)
            {
                throw new InvalidOperationException("Camera set is not assigned.");
            }

            List<VLiveMulticamCut> cuts = BuildExportCuts(switcher, cameraSet);
            if (cuts.Count == 0)
            {
                throw new InvalidOperationException("There are no valid multicam cuts to export.");
            }

            string resolvedTrackName = MakeUniqueTrackName(
                timeline,
                string.IsNullOrEmpty(trackName) ? "VLive Multicam" : trackName);

            Undo.RegisterCompleteObjectUndo(timeline, "Export Multicam Timeline");
            CinemachineTrack track = timeline.CreateTrack<CinemachineTrack>(null, resolvedTrackName);

            double endTime = ResolveEndTime(timeline, director, cuts, explicitEndTime);

            for (int i = 0; i < cuts.Count; i++)
            {
                VLiveMulticamCut cut = cuts[i];
                VLiveMulticamAngle angle = cameraSet.GetAngle(cut.AngleIndex);
                if (angle == null || angle.VirtualCamera == null)
                {
                    continue;
                }

                double clipStart = Math.Max(0d, cut.Time);
                double nextStart = i + 1 < cuts.Count ? Math.Max(clipStart, cuts[i + 1].Time) : endTime;
                double clipDuration = Math.Max(MinimumClipDuration, nextStart - clipStart);

                TimelineClip clip = track.CreateClip<CinemachineShot>();
                clip.start = clipStart;
                clip.duration = clipDuration;
                clip.displayName = angle.DisplayName;

                CinemachineShot shot = clip.asset as CinemachineShot;
                if (shot != null)
                {
                    shot.VirtualCamera.defaultValue = angle.VirtualCamera;
                }
            }

            CinemachineBrain brain = ResolveBrain(director);
            if (brain != null)
            {
                Undo.RecordObject(director, "Bind Cinemachine Track");
                director.SetGenericBinding(track, brain);
            }

            EditorUtility.SetDirty(timeline);
            EditorUtility.SetDirty(director);

            if (director.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
            }

            return track;
        }

        private static List<VLiveMulticamCut> BuildExportCuts(
            VLiveMulticamSwitcher switcher,
            VLiveMulticamCameraSet cameraSet)
        {
            List<VLiveMulticamCut> sourceCuts = switcher.GetSortedCutCopies();
            List<VLiveMulticamCut> result = new List<VLiveMulticamCut>();

            for (int i = 0; i < sourceCuts.Count; i++)
            {
                VLiveMulticamCut cut = sourceCuts[i];
                VLiveMulticamAngle angle = cameraSet.GetAngle(cut.AngleIndex);
                if (angle == null || angle.VirtualCamera == null)
                {
                    continue;
                }

                if (result.Count > 0 && result[result.Count - 1].AngleIndex == cut.AngleIndex)
                {
                    continue;
                }

                result.Add(cut.Clone());
            }

            if (result.Count == 0 && cameraSet.IsValidAngleIndex(switcher.ActiveAngleIndex))
            {
                result.Add(new VLiveMulticamCut(0d, switcher.ActiveAngleIndex));
            }

            if (result.Count > 0 && result[0].Time > 0d)
            {
                result.Insert(0, new VLiveMulticamCut(0d, result[0].AngleIndex));
            }

            return result;
        }

        private static double ResolveEndTime(
            TimelineAsset timeline,
            PlayableDirector director,
            List<VLiveMulticamCut> cuts,
            double explicitEndTime)
        {
            double lastCutTime = cuts[cuts.Count - 1].Time;

            if (explicitEndTime > lastCutTime)
            {
                return explicitEndTime;
            }

            if (director.duration > lastCutTime && !double.IsInfinity(director.duration))
            {
                return director.duration;
            }

            if (timeline.duration > lastCutTime && !double.IsInfinity(timeline.duration))
            {
                return timeline.duration;
            }

            return lastCutTime + 1d;
        }

        private static CinemachineBrain ResolveBrain(PlayableDirector director)
        {
            CinemachineBrain brain = director.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                return brain;
            }

            if (Camera.main != null)
            {
                brain = Camera.main.GetComponent<CinemachineBrain>();
                if (brain != null)
                {
                    return brain;
                }
            }

#if UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindFirstObjectByType<CinemachineBrain>();
#else
            return UnityEngine.Object.FindObjectOfType<CinemachineBrain>();
#endif
        }

        private static string MakeUniqueTrackName(TimelineAsset timeline, string baseName)
        {
            HashSet<string> existingNames = new HashSet<string>();
            foreach (TrackAsset track in timeline.GetOutputTracks())
            {
                if (track != null)
                {
                    existingNames.Add(track.name);
                }
            }

            if (!existingNames.Contains(baseName))
            {
                return baseName;
            }

            for (int i = 1; i < 1000; i++)
            {
                string candidate = $"{baseName} ({i})";
                if (!existingNames.Contains(candidate))
                {
                    return candidate;
                }
            }

            return $"{baseName} ({DateTime.Now:HHmmss})";
        }
    }
}
#endif
