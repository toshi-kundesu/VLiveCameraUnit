using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace toshi.VLiveKit.Photography
{
    [DisallowMultipleComponent]
    public class VLiveMulticamSwitcher : MonoBehaviour
    {
        private const double TimeMergeTolerance = 0.0001d;

        [Header("References")]
        [SerializeField] private VLiveMulticamCameraSet cameraSet;
        [SerializeField] private PlayableDirector playableDirector;

        [Header("Recording")]
        [SerializeField] private bool recordSwitches = true;
        [SerializeField] private bool enableRuntimeHotkeys = true;
        [SerializeField] private bool playbackRecordedCutsInPlayMode = false;
        [SerializeField] private int activeAngleIndex = -1;
        [SerializeField] private List<VLiveMulticamCut> cuts = new List<VLiveMulticamCut>();

        public VLiveMulticamCameraSet CameraSet
        {
            get => cameraSet;
            set => cameraSet = value;
        }

        public PlayableDirector Director
        {
            get => playableDirector;
            set => playableDirector = value;
        }

        public bool RecordSwitches
        {
            get => recordSwitches;
            set => recordSwitches = value;
        }

        public bool EnableRuntimeHotkeys
        {
            get => enableRuntimeHotkeys;
            set => enableRuntimeHotkeys = value;
        }

        public bool PlaybackRecordedCutsInPlayMode
        {
            get => playbackRecordedCutsInPlayMode;
            set => playbackRecordedCutsInPlayMode = value;
        }

        public int ActiveAngleIndex => activeAngleIndex;
        public IReadOnlyList<VLiveMulticamCut> Cuts => cuts;
        public int CutCount => cuts.Count;

        private void Reset()
        {
            ResolveReferencesIfMissing();
        }

        private void OnValidate()
        {
            ResolveReferencesIfMissing();
            SortCuts();

            if (cameraSet != null && activeAngleIndex >= cameraSet.AngleCount)
            {
                activeAngleIndex = cameraSet.AngleCount - 1;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (enableRuntimeHotkeys)
            {
                HandleRuntimeHotkeys();
            }

            if (playbackRecordedCutsInPlayMode && !recordSwitches)
            {
                ApplyRecordedCutAtTime(GetCurrentTime(), false);
            }
        }

        public double GetCurrentTime()
        {
            if (playableDirector != null)
            {
                return playableDirector.time;
            }

            return Application.isPlaying ? Time.timeAsDouble : 0d;
        }

        public bool SwitchToAngle(int angleIndex)
        {
            return SwitchToAngle(angleIndex, GetCurrentTime(), recordSwitches);
        }

        public bool SwitchToAngle(int angleIndex, double time, bool recordCut)
        {
            if (!SetActiveAngle(angleIndex))
            {
                return false;
            }

            if (recordCut)
            {
                RecordCut(time, angleIndex);
            }

            MarkDirty();
            return true;
        }

        public bool TrySwitchByKey(KeyCode keyCode, double time, bool recordCut)
        {
            if (cameraSet == null)
            {
                return false;
            }

            for (int i = 0; i < cameraSet.AngleCount; i++)
            {
                VLiveMulticamAngle angle = cameraSet.GetAngle(i);
                if (angle != null && angle.HotKey == keyCode)
                {
                    return SwitchToAngle(i, time, recordCut);
                }
            }

            return false;
        }

        public bool SetActiveAngle(int angleIndex)
        {
            if (cameraSet == null || !cameraSet.IsValidAngleIndex(angleIndex))
            {
                return false;
            }

            VLiveMulticamAngle activeAngle = cameraSet.GetAngle(angleIndex);
            if (activeAngle == null || activeAngle.VirtualCamera == null)
            {
                return false;
            }

            for (int i = 0; i < cameraSet.AngleCount; i++)
            {
                VLiveMulticamAngle angle = cameraSet.GetAngle(i);
                if (angle == null || angle.VirtualCamera == null)
                {
                    continue;
                }

                angle.VirtualCamera.Priority =
                    i == angleIndex ? cameraSet.ActivePriority : cameraSet.StandbyPriority;
            }

            activeAngleIndex = angleIndex;
            MarkDirty();
            return true;
        }

        public void RecordCut(double time, int angleIndex, float blendDuration = 0f)
        {
            if (cameraSet != null && !cameraSet.IsValidAngleIndex(angleIndex))
            {
                return;
            }

            time = Math.Max(0d, time);

            for (int i = 0; i < cuts.Count; i++)
            {
                if (Math.Abs(cuts[i].Time - time) > TimeMergeTolerance)
                {
                    continue;
                }

                cuts[i].Time = time;
                cuts[i].AngleIndex = angleIndex;
                cuts[i].BlendDuration = blendDuration;
                SortCuts();
                CompactRedundantCuts();
                MarkDirty();
                return;
            }

            cuts.Add(new VLiveMulticamCut(time, angleIndex, blendDuration));
            SortCuts();
            CompactRedundantCuts();
            MarkDirty();
        }

        public bool ApplyRecordedCutAtTime(double time, bool force = false)
        {
            int cutIndex = GetRecordedCutIndexAtTime(time);
            if (cutIndex < 0)
            {
                return false;
            }

            int angleIndex = cuts[cutIndex].AngleIndex;
            if (!force && angleIndex == activeAngleIndex)
            {
                return true;
            }

            return SetActiveAngle(angleIndex);
        }

        public int GetRecordedCutIndexAtTime(double time)
        {
            SortCuts();

            int result = -1;
            for (int i = 0; i < cuts.Count; i++)
            {
                if (cuts[i].Time > time + TimeMergeTolerance)
                {
                    break;
                }

                result = i;
            }

            return result;
        }

        public VLiveMulticamCut GetCut(int index)
        {
            return index >= 0 && index < cuts.Count ? cuts[index] : null;
        }

        public void RemoveCutAt(int index)
        {
            if (index < 0 || index >= cuts.Count)
            {
                return;
            }

            cuts.RemoveAt(index);
            MarkDirty();
        }

        public void ClearCuts()
        {
            cuts.Clear();
            MarkDirty();
        }

        public List<VLiveMulticamCut> GetSortedCutCopies()
        {
            SortCuts();

            List<VLiveMulticamCut> result = new List<VLiveMulticamCut>(cuts.Count);
            for (int i = 0; i < cuts.Count; i++)
            {
                result.Add(cuts[i].Clone());
            }

            return result;
        }

        public void SortCuts()
        {
            cuts.Sort((a, b) => a.Time.CompareTo(b.Time));
        }

        private void ResolveReferencesIfMissing()
        {
            if (cameraSet == null)
            {
                cameraSet = GetComponent<VLiveMulticamCameraSet>();
            }

            if (playableDirector == null)
            {
                playableDirector = GetComponentInParent<PlayableDirector>();
            }
        }

        private void HandleRuntimeHotkeys()
        {
            if (cameraSet == null)
            {
                return;
            }

            for (int i = 0; i < cameraSet.AngleCount; i++)
            {
                VLiveMulticamAngle angle = cameraSet.GetAngle(i);
                if (angle == null || angle.HotKey == KeyCode.None)
                {
                    continue;
                }

                if (Input.GetKeyDown(angle.HotKey))
                {
                    SwitchToAngle(i, GetCurrentTime(), recordSwitches);
                    break;
                }
            }
        }

        private void CompactRedundantCuts()
        {
            SortCuts();

            for (int i = cuts.Count - 1; i > 0; i--)
            {
                if (cuts[i].AngleIndex == cuts[i - 1].AngleIndex)
                {
                    cuts.RemoveAt(i);
                }
            }
        }

        private void MarkDirty()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }
    }
}
