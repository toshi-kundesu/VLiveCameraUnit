using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using toshi.VLiveKit;

namespace toshi.VLiveKit.Photography
{
    [DefaultExecutionOrder(115)]
    [DisallowMultipleComponent]
    [AddComponentMenu("toshi/VLiveKit/Photography/VLive Camera Preset Spawner")]
    public class VLiveCameraPresetSpawner : MonoBehaviour
    {
        public enum PresetControlMode
        {
            Manual,
            AutoShuffle
        }

        [System.Serializable]
        public class CameraPresetSlot
        {
            public string cameraName = "cam01";
            public VLiveCameraPreset preset;
            public VLiveCamera cameraMan;
            public bool usePresetTagFilter = false;
            public VLiveCameraPreset.ShotScaleTag shotScaleFilter = VLiveCameraPreset.ShotScaleTag.None;
            public VLiveCameraPreset.StageSideTag stageSideFilter = VLiveCameraPreset.StageSideTag.None;
            public VLiveCameraPreset.CameraRigTag cameraRigFilter = VLiveCameraPreset.CameraRigTag.None;
            public string customTagFilter;
        }

        private const string DefaultPresetFolderPath =
            "Assets/toshi.VLiveKit/VLiveCameraUnit/Presets/SceneCameras";

        private const int DefaultCameraSlotCount = 8;

        [Header("Preset Source")]
        [SerializeField] private string presetFolderPath = DefaultPresetFolderPath;
        [SerializeField] private List<VLiveCameraPreset> presets = new List<VLiveCameraPreset>();

        [Header("Camera Template")]
        [SerializeField] private GameObject cameraPrefab;
        [SerializeField] private bool applyPresetImmediately = true;
        [SerializeField] private bool renameFromPreset = true;
        [SerializeField] private bool assignLayerFromSlotName = true;
        [SerializeField] private bool assignLayerToChildren = true;
        [SerializeField] private string generatedNamePrefix = "VLiveCamera_";
        [SerializeField] private Vector3 firstLocalPosition = Vector3.zero;

        [FormerlySerializedAs("localPositionStep")]
        [SerializeField] private Vector3 perCameraLocalOffset = Vector3.zero;

        [Header("Runtime Rig")]
        [SerializeField] private VLiveTimeTable timeTable;
        [SerializeField] private string timelineSectionName;
        [SerializeField] private VLiveLookTargetRig targetRig;
        [SerializeField] private bool assignReferencesOnAwake = true;
        [SerializeField] private bool assignTargetsOnStart = true;

        [Header("Camera Preset Slots")]
        [SerializeField] private PresetControlMode presetControlMode = PresetControlMode.Manual;
        [SerializeField] private bool applySlotWhenManualPresetChanges = true;
        [SerializeField] private bool shuffleOnStart = false;
        [Min(0.1f)]
        [SerializeField] private float shuffleIntervalSeconds = 5f;
        [SerializeField] private List<CameraPresetSlot> cameraPresetSlots = new List<CameraPresetSlot>();

        [Header("Generated Cameras")]
        [SerializeField] private List<VLiveCamera> generatedCameraMen = new List<VLiveCamera>();

        public string PresetFolderPath
        {
            get => presetFolderPath;
            set => presetFolderPath = value;
        }

        public List<VLiveCameraPreset> Presets => presets;
        public List<CameraPresetSlot> CameraPresetSlots => cameraPresetSlots;
        public IReadOnlyList<VLiveCamera> GeneratedCameraMen => generatedCameraMen;
        public PresetControlMode ControlMode => presetControlMode;
        public bool ApplySlotWhenManualPresetChanges => applySlotWhenManualPresetChanges;

        private float shuffleTimer;

        public bool EnsureDefaultCameraPresetSlots()
        {
            return EnsureCameraPresetSlots();
        }

        private void Reset()
        {
            EnsureCameraPresetSlots();
        }

        private void Awake()
        {
            EnsureCameraPresetSlots();

            if (assignReferencesOnAwake)
            {
                AssignRuntimeReferences(false);
            }
        }

        private void Start()
        {
            if (assignTargetsOnStart)
            {
                AssignRuntimeReferences(true);
            }

            if (shuffleOnStart)
            {
                shuffleTimer = 0f;
                ShuffleAndApplyCameraPresetSlots();
            }
        }

        private void Update()
        {
            if (presetControlMode != PresetControlMode.AutoShuffle)
                return;

            if (presets.Count == 0)
                return;

            shuffleTimer += Time.unscaledDeltaTime;
            if (shuffleTimer < Mathf.Max(0.1f, shuffleIntervalSeconds))
                return;

            shuffleTimer = 0f;
            ShuffleAndApplyCameraPresetSlots();
        }

        [ContextMenu("Assign Runtime References")]
        public void AssignRuntimeReferences()
        {
            AssignRuntimeReferences(true);
        }

        public void AssignRuntimeReferences(bool assignTargetsImmediately)
        {
            ResolveRuntimeRigReferences();
            PruneMissingGeneratedCameras();

            List<VLiveCamera> runtimeReferenceTargets = new List<VLiveCamera>();
            AddGeneratedCameras(runtimeReferenceTargets);
            AddSlotCameras(runtimeReferenceTargets);
            AddChildCameras(runtimeReferenceTargets);

            for (int i = 0; i < runtimeReferenceTargets.Count; i++)
            {
                VLiveCamera cameraMan = runtimeReferenceTargets[i];
                if (cameraMan == null)
                    continue;

                cameraMan.ConfigureRuntimeReferences(
                    timeTable,
                    targetRig,
                    timelineSectionName,
                    false,
                    assignTargetsImmediately);
            }
        }

        [ContextMenu("Generate Cameras From Presets")]
        public void GenerateCamerasFromPresets()
        {
            EnsureCameraPresetSlots();
            PruneMissingGeneratedCameras();

            if (presets.Count == 0)
            {
                Debug.LogWarning("[VLiveCameraPresetSpawner] No presets are assigned.", this);
                return;
            }

            for (int i = 0; i < presets.Count; i++)
            {
                VLiveCameraPreset preset = presets[i];
                if (preset == null)
                    continue;

                VLiveCamera existing = FindGeneratedCameraForPreset(preset);
                if (existing != null)
                {
                    ConfigureGeneratedCamera(existing, preset, i);
                    continue;
                }

                VLiveCamera cameraMan = CreateCameraMan(preset, i);
                if (cameraMan != null)
                {
                    generatedCameraMen.Add(cameraMan);
                }
            }

            AssignRuntimeReferences(false);
        }

        [ContextMenu("Apply Camera Preset Slots")]
        public void ApplyCameraPresetSlots()
        {
            EnsureCameraPresetSlots();
            PruneMissingGeneratedCameras();
            ResolveRuntimeRigReferences();

            for (int i = 0; i < cameraPresetSlots.Count; i++)
            {
                CameraPresetSlot slot = cameraPresetSlots[i];
                if (slot == null || slot.preset == null)
                    continue;

                VLiveCamera cameraMan = ResolveSlotCamera(slot, i);
                if (cameraMan == null)
                    continue;

                ConfigureSlotCamera(cameraMan, slot, i);
            }

            AssignRuntimeReferences(false);
        }

        public void ApplyCameraPresetSlot(int slotIndex)
        {
            EnsureCameraPresetSlots();
            PruneMissingGeneratedCameras();
            ResolveRuntimeRigReferences();

            if (slotIndex < 0 || slotIndex >= cameraPresetSlots.Count)
                return;

            CameraPresetSlot slot = cameraPresetSlots[slotIndex];
            if (slot == null || slot.preset == null)
                return;

            VLiveCamera cameraMan = ResolveSlotCamera(slot, slotIndex);
            if (cameraMan == null)
                return;

            ConfigureSlotCamera(cameraMan, slot, slotIndex);
            AssignRuntimeReferences(false);
        }

        [ContextMenu("Shuffle And Apply Camera Preset Slots")]
        public void ShuffleAndApplyCameraPresetSlots()
        {
            EnsureCameraPresetSlots();

            if (presets.Count == 0)
            {
                Debug.LogWarning("[VLiveCameraPresetSpawner] No presets are assigned.", this);
                return;
            }

            for (int i = 0; i < cameraPresetSlots.Count; i++)
            {
                CameraPresetSlot slot = cameraPresetSlots[i];
                if (slot == null)
                    continue;

                slot.preset = PickRandomPreset(slot.preset, slot);
            }

            ApplyCameraPresetSlots();
        }

        [ContextMenu("Collect Generated Cameras In Children")]
        public void CollectGeneratedCamerasInChildren()
        {
            generatedCameraMen.Clear();
            GetComponentsInChildren(true, generatedCameraMen);
            generatedCameraMen.RemoveAll(cameraMan => cameraMan == null || cameraMan.transform == transform);
        }

        private VLiveCamera CreateCameraMan(VLiveCameraPreset preset, int index)
        {
            ResolveRuntimeRigReferences();

            GameObject instance = cameraPrefab != null
                ? Instantiate(cameraPrefab, transform)
                : new GameObject();

            instance.transform.SetParent(transform, false);
            instance.transform.localPosition = firstLocalPosition + (perCameraLocalOffset * index);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            VLiveCamera cameraMan = instance.GetComponentInChildren<VLiveCamera>(true);
            if (cameraMan == null)
            {
                cameraMan = instance.AddComponent<VLiveCamera>();
            }

            if (instance.GetComponent<CinemachineVirtualCamera>() == null &&
                instance.GetComponentInChildren<CinemachineVirtualCamera>(true) == null)
            {
                instance.AddComponent<CinemachineVirtualCamera>();
            }

            ConfigureGeneratedCamera(cameraMan, preset, index);
            return cameraMan;
        }

        private VLiveCamera ResolveSlotCamera(CameraPresetSlot slot, int index)
        {
            if (slot.cameraMan != null)
            {
                AddGeneratedCameraIfMissing(slot.cameraMan);
                return slot.cameraMan;
            }

            string slotName = GetSlotCameraName(slot, index);
            VLiveCamera existing = FindGeneratedCameraByName(slotName);
            if (existing != null)
            {
                slot.cameraMan = existing;
                return existing;
            }

            GameObject instance = cameraPrefab != null
                ? Instantiate(cameraPrefab, transform)
                : new GameObject();

            instance.transform.SetParent(transform, false);
            instance.transform.localPosition = firstLocalPosition + (perCameraLocalOffset * index);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            VLiveCamera cameraMan = instance.GetComponentInChildren<VLiveCamera>(true);
            if (cameraMan == null)
            {
                cameraMan = instance.AddComponent<VLiveCamera>();
            }

            if (instance.GetComponent<CinemachineVirtualCamera>() == null &&
                instance.GetComponentInChildren<CinemachineVirtualCamera>(true) == null)
            {
                instance.AddComponent<CinemachineVirtualCamera>();
            }

            slot.cameraMan = cameraMan;
            AddGeneratedCameraIfMissing(cameraMan);

            return cameraMan;
        }

        private void ConfigureGeneratedCamera(VLiveCamera cameraMan, VLiveCameraPreset preset, int index)
        {
            ResolveRuntimeRigReferences();

            if (renameFromPreset)
            {
                cameraMan.gameObject.name = BuildCameraName(preset, index);
            }

            cameraMan.SetPreset(preset, applyPresetImmediately);
            cameraMan.ConfigureRuntimeReferences(timeTable, targetRig, timelineSectionName, false, false);
        }

        private void ConfigureSlotCamera(VLiveCamera cameraMan, CameraPresetSlot slot, int index)
        {
            cameraMan.gameObject.name = GetSlotCameraName(slot, index);
            AssignSlotLayer(cameraMan, slot, index);
            cameraMan.SetPreset(slot.preset, applyPresetImmediately);
            cameraMan.ConfigureRuntimeReferences(timeTable, targetRig, timelineSectionName, false, false);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(cameraMan);
                UnityEditor.EditorUtility.SetDirty(cameraMan.gameObject);
            }
#endif
        }

        private void AssignSlotLayer(VLiveCamera cameraMan, CameraPresetSlot slot, int index)
        {
            if (!assignLayerFromSlotName || cameraMan == null)
                return;

            string layerName = slot != null && !string.IsNullOrWhiteSpace(slot.cameraName)
                ? slot.cameraName
                : GetDefaultSlotCameraName(index);

            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0 || layer >= 32)
            {
                Debug.LogWarning(
                    $"[VLiveCameraPresetSpawner] Layer '{layerName}' is missing. Add it in Project Settings > Tags and Layers.",
                    this);
                return;
            }

            if (assignLayerToChildren)
            {
                SetLayerRecursively(cameraMan.transform, layer);
            }
            else
            {
                cameraMan.gameObject.layer = layer;
            }
        }

        private void ResolveRuntimeRigReferences()
        {
            bool changed = false;

            if (timeTable == null)
            {
                timeTable = VLiveTimeTable.Get(this);
                changed |= timeTable != null;
            }

            if (targetRig == null)
            {
                targetRig = VLiveLookTargetRig.Get(this);
                changed |= targetRig != null;
            }

#if UNITY_EDITOR
            if (changed && !Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

        private VLiveCamera FindGeneratedCameraForPreset(VLiveCameraPreset preset)
        {
            for (int i = 0; i < generatedCameraMen.Count; i++)
            {
                VLiveCamera cameraMan = generatedCameraMen[i];
                if (cameraMan == null)
                    continue;

                if (cameraMan.Preset == preset)
                    return cameraMan;
            }

            return null;
        }

        private VLiveCameraPreset PickRandomPreset(VLiveCameraPreset currentPreset, CameraPresetSlot slot)
        {
            if (presets.Count == 0)
                return null;

            List<VLiveCameraPreset> candidates = GetPresetCandidates(slot);

            if (candidates.Count == 0)
            {
                Debug.LogWarning(
                    $"[VLiveCameraPresetSpawner] No presets match the tag filter for '{slot?.cameraName}'. Falling back to all presets.",
                    this);
                candidates = presets;
            }

            if (candidates.Count == 1)
                return candidates[0];

            VLiveCameraPreset preset = currentPreset;
            int guard = 0;
            while (preset == currentPreset && guard < 16)
            {
                preset = candidates[Random.Range(0, candidates.Count)];
                guard++;
            }

            return preset != null ? preset : candidates[Random.Range(0, candidates.Count)];
        }

        private List<VLiveCameraPreset> GetPresetCandidates(CameraPresetSlot slot)
        {
            List<VLiveCameraPreset> candidates = new List<VLiveCameraPreset>();
            for (int i = 0; i < presets.Count; i++)
            {
                VLiveCameraPreset preset = presets[i];
                if (preset == null)
                    continue;

                if (slot == null || !slot.usePresetTagFilter)
                {
                    candidates.Add(preset);
                    continue;
                }

                if (preset.MatchesTags(
                    slot.shotScaleFilter,
                    slot.stageSideFilter,
                    slot.cameraRigFilter,
                    slot.customTagFilter))
                {
                    candidates.Add(preset);
                }
            }

            return candidates;
        }

        private VLiveCamera FindGeneratedCameraByName(string cameraName)
        {
            for (int i = 0; i < generatedCameraMen.Count; i++)
            {
                VLiveCamera cameraMan = generatedCameraMen[i];
                if (cameraMan == null)
                    continue;

                if (cameraMan.gameObject.name == cameraName)
                    return cameraMan;
            }

            VLiveCamera[] childCameras = GetComponentsInChildren<VLiveCamera>(true);
            for (int i = 0; i < childCameras.Length; i++)
            {
                VLiveCamera cameraMan = childCameras[i];
                if (cameraMan == null || cameraMan.transform == transform)
                    continue;

                if (cameraMan.gameObject.name == cameraName)
                    return cameraMan;
            }

            return null;
        }

        private string BuildCameraName(VLiveCameraPreset preset, int index)
        {
            string presetName = preset != null && !string.IsNullOrWhiteSpace(preset.name)
                ? preset.name
                : preset != null && !string.IsNullOrWhiteSpace(preset.presetDisplayName)
                    ? preset.presetDisplayName
                    : index.ToString("00");

            return generatedNamePrefix + SanitizeObjectName(presetName);
        }

        private string GetSlotCameraName(CameraPresetSlot slot, int index)
        {
            string cameraName = slot != null && !string.IsNullOrWhiteSpace(slot.cameraName)
                ? slot.cameraName
                : GetDefaultSlotCameraName(index);

            return generatedNamePrefix + SanitizeObjectName(cameraName);
        }

        private bool EnsureCameraPresetSlots()
        {
            bool changed = false;

            if (cameraPresetSlots == null)
            {
                cameraPresetSlots = new List<CameraPresetSlot>();
                changed = true;
            }

            while (cameraPresetSlots.Count < DefaultCameraSlotCount)
            {
                int slotIndex = cameraPresetSlots.Count;
                cameraPresetSlots.Add(new CameraPresetSlot
                {
                    cameraName = GetDefaultSlotCameraName(slotIndex)
                });
                changed = true;
            }

            for (int i = 0; i < cameraPresetSlots.Count; i++)
            {
                if (cameraPresetSlots[i] == null)
                {
                    cameraPresetSlots[i] = new CameraPresetSlot();
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(cameraPresetSlots[i].cameraName))
                {
                    cameraPresetSlots[i].cameraName = GetDefaultSlotCameraName(i);
                    changed = true;
                }
                else if (IsLegacySlotCameraName(cameraPresetSlots[i].cameraName, i))
                {
                    cameraPresetSlots[i].cameraName = GetDefaultSlotCameraName(i);
                    changed = true;
                }
            }

            return changed;
        }

        private static string GetDefaultSlotCameraName(int slotIndex)
        {
            return $"cam{slotIndex + 1:00}";
        }

        private static bool IsLegacySlotCameraName(string cameraName, int slotIndex)
        {
            return string.Equals(cameraName, $"CAM_{slotIndex + 1:00}", System.StringComparison.OrdinalIgnoreCase);
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            if (root == null)
                return;

            root.gameObject.layer = layer;

            for (int i = 0; i < root.childCount; i++)
            {
                SetLayerRecursively(root.GetChild(i), layer);
            }
        }

        private void PruneMissingGeneratedCameras()
        {
            generatedCameraMen.RemoveAll(cameraMan => cameraMan == null);
        }

        private void AddGeneratedCameras(List<VLiveCamera> cameras)
        {
            for (int i = 0; i < generatedCameraMen.Count; i++)
            {
                AddCameraIfMissing(cameras, generatedCameraMen[i]);
            }
        }

        private void AddSlotCameras(List<VLiveCamera> cameras)
        {
            EnsureCameraPresetSlots();

            for (int i = 0; i < cameraPresetSlots.Count; i++)
            {
                CameraPresetSlot slot = cameraPresetSlots[i];
                if (slot == null || slot.cameraMan == null)
                    continue;

                AddGeneratedCameraIfMissing(slot.cameraMan);
                AddCameraIfMissing(cameras, slot.cameraMan);
            }
        }

        private void AddChildCameras(List<VLiveCamera> cameras)
        {
            VLiveCamera[] childCameras = GetComponentsInChildren<VLiveCamera>(true);
            for (int i = 0; i < childCameras.Length; i++)
            {
                VLiveCamera cameraMan = childCameras[i];
                if (cameraMan == null || cameraMan.transform == transform)
                    continue;

                AddGeneratedCameraIfMissing(cameraMan);
                AddCameraIfMissing(cameras, cameraMan);
            }
        }

        private void AddGeneratedCameraIfMissing(VLiveCamera cameraMan)
        {
            AddCameraIfMissing(generatedCameraMen, cameraMan);
        }

        private static void AddCameraIfMissing(List<VLiveCamera> cameras, VLiveCamera cameraMan)
        {
            if (cameraMan == null)
                return;

            for (int i = 0; i < cameras.Count; i++)
            {
                if (cameras[i] == cameraMan)
                    return;
            }

            cameras.Add(cameraMan);
        }

        private static string SanitizeObjectName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Camera";

            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalidChars.Length; i++)
            {
                value = value.Replace(invalidChars[i], '_');
            }

            return value.Trim();
        }
    }
}
