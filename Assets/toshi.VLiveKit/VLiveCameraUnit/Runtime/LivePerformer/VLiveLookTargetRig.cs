using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;
using System;
using System.Collections.Generic;

[DefaultExecutionOrder(110)]
[MovedFrom(false, sourceNamespace: null, sourceAssembly: null, sourceClassName: "BoneCollector")]
public class VLiveLookTargetRig : MonoBehaviour
{
    [Header("Performer Reference")]
    [SerializeField] private VLivePerformer vLivePerformer;

    [Header("Animator Fallback")]
    [FormerlySerializedAs("targetAnimator")]
    [SerializeField] private Animator performerAnimator;

    [Header("Humanoid Support")]
    [FormerlySerializedAs("primaryHumanoidAvatar")]
    [SerializeField] private Avatar fallbackHumanoidAvatar;

    [Header("Look Target Root")]
    [FormerlySerializedAs("parentTransform")]
    [SerializeField] private Transform lookTargetRoot;

    [Header("Performer Naming")]
    [FormerlySerializedAs("characterName")]
    [SerializeField] private string performerName = "Performer";

    [Header("Live Update")]
    [FormerlySerializedAs("updateWeightsEveryFrame")]
    [SerializeField] private bool syncActiveStateEveryFrame = true;

    [Serializable]
    public struct LookTargetChannel
    {
        public HumanBodyBones targetBone;
        public Transform performerBone;
        public GameObject lookTargetObject;
    }

    [Header("Debug View")]
    [FormerlySerializedAs("boneDataList")]
    [SerializeField] private List<LookTargetChannel> lookTargetChannels = new();

    private readonly Dictionary<HumanBodyBones, Transform> _boneMap = new();
    private readonly Dictionary<HumanBodyBones, GameObject> _targetMap = new();
    private readonly Dictionary<HumanBodyBones, PositionConstraint> _constraintMap = new();

    public Transform LookTargetRoot => lookTargetRoot;
    public IReadOnlyList<LookTargetChannel> LookTargetChannels => lookTargetChannels;

    public bool IsPerformerLive { get; private set; }

    private void Start()
    {
        AutoResolvePerformer();
        BuildTargets();
        RefreshLiveState();
    }

    private void Update()
    {
        if (syncActiveStateEveryFrame)
        {
            RefreshLiveState();
        }
    }

    // ----------------------------
    // Performer Resolve
    // ----------------------------

    private void AutoResolvePerformer()
    {
        if (vLivePerformer == null)
        {
            vLivePerformer = GetComponentInParent<VLivePerformer>();

            if (vLivePerformer == null)
            {
                vLivePerformer = FindObjectOfType<VLivePerformer>();
            }
        }

        if (vLivePerformer != null)
        {
            performerAnimator = vLivePerformer.PerformerAnimator;

            if (string.IsNullOrWhiteSpace(performerName))
            {
                performerName = vLivePerformer.PerformerName;
            }
        }
    }

    // ----------------------------
    // Build
    // ----------------------------

    [ContextMenu("Build Targets")]
    public void BuildTargets()
    {
        ClearTargets();

        _boneMap.Clear();
        _targetMap.Clear();
        _constraintMap.Clear();
        lookTargetChannels.Clear();

        CollectBones(performerAnimator, fallbackHumanoidAvatar, _boneMap);

        if (_boneMap.Count == 0)
        {
            Debug.LogWarning("[VLiveLookTargetRig] Bone collect failed");
            return;
        }

        EnsureRoot();

        foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (bone == HumanBodyBones.LastBone) continue;

            if (!_boneMap.TryGetValue(bone, out var t)) continue;

            var go = new GameObject($"VLiveTG_{performerName}_{bone}");
            go.transform.SetParent(lookTargetRoot, false);
            go.transform.position = t.position;
            go.transform.rotation = t.rotation;

            var c = go.AddComponent<PositionConstraint>();
            c.translationAtRest = Vector3.zero;
            c.locked = true;
            c.AddSource(new ConstraintSource { sourceTransform = t, weight = 1f });
            c.constraintActive = true;

            _targetMap[bone] = go;
            _constraintMap[bone] = c;

            lookTargetChannels.Add(new LookTargetChannel
            {
                targetBone = bone,
                performerBone = t,
                lookTargetObject = go
            });
        }
    }

    // ----------------------------
    // Live State
    // ----------------------------

    public void RefreshLiveState()
    {
        bool active = performerAnimator != null && performerAnimator.gameObject.activeInHierarchy;
        IsPerformerLive = active;

        foreach (var c in _constraintMap.Values)
        {
            for (int i = 0; i < c.sourceCount; i++)
            {
                var s = c.GetSource(i);
                s.weight = active ? 1f : 0f;
                c.SetSource(i, s);
            }
        }
    }

    // ----------------------------
    // Helpers
    // ----------------------------

    private void CollectBones(
        Animator anim,
        Avatar fallback,
        Dictionary<HumanBodyBones, Transform> dict)
    {
        if (!anim) return;

        var original = anim.avatar;
        bool swapped = false;

        if ((original == null || !original.isHuman) && fallback && fallback.isHuman)
        {
            anim.avatar = fallback;
            swapped = true;
        }

        foreach (HumanBodyBones b in Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (b == HumanBodyBones.LastBone) continue;

            var t = anim.GetBoneTransform(b);
            if (t) dict[b] = t;
        }

        if (swapped) anim.avatar = original;
    }

    private void EnsureRoot()
    {
        if (lookTargetRoot != null) return;

        var go = new GameObject($"VLiveTargets_{performerName}");
        lookTargetRoot = go.transform;

        if (performerAnimator)
            lookTargetRoot.SetParent(performerAnimator.transform, false);
    }

    private void ClearTargets()
    {
        if (!lookTargetRoot) return;

        for (int i = lookTargetRoot.childCount - 1; i >= 0; i--)
        {
            var c = lookTargetRoot.GetChild(i).gameObject;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                GameObject.DestroyImmediate(c);
            else
#endif
                GameObject.Destroy(c);
        }
    }

    // ----------------------------
    // Legacy API
    // ----------------------------

    public GameObject GetBoneTG(HumanBodyBones bone)
    {
        if (_targetMap.TryGetValue(bone, out var g)) return g;
        return null;
    }
}