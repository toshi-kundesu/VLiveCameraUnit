using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;
using System;
using System.Collections.Generic;

[DefaultExecutionOrder(110)]
[DisallowMultipleComponent]
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

    [Header("Multi Performer Targets")]
    [SerializeField] private bool activeSourcesOnly = true;
    [SerializeField] private List<LookTargetPerformerSource> additionalPerformerSources = new();

    [Serializable]
    public struct LookTargetChannel
    {
        public HumanBodyBones targetBone;
        public Transform performerBone;
        public GameObject lookTargetObject;
        public int sourceCount;
    }

    [Serializable]
    public class LookTargetPerformerSource
    {
        [SerializeField] private bool sourceEnabled = true;
        [SerializeField] private VLivePerformer vLivePerformer;
        [SerializeField] private Animator performerAnimator;
        [SerializeField] private Avatar fallbackHumanoidAvatar;
        [SerializeField] private string performerName;

        public bool SourceEnabled => sourceEnabled;
        public VLivePerformer Performer => vLivePerformer;
        public Animator Animator => performerAnimator;
        public Avatar FallbackAvatar => fallbackHumanoidAvatar;
        public string PerformerName => performerName;

        public Animator ResolveAnimator()
        {
            if (performerAnimator == null && vLivePerformer != null)
            {
                performerAnimator = vLivePerformer.PerformerAnimator;
            }

            if (performerAnimator == null && vLivePerformer != null)
            {
                performerAnimator = vLivePerformer.GetComponentInChildren<Animator>(true);
            }

            return performerAnimator;
        }

        public string ResolveName()
        {
            if (!string.IsNullOrWhiteSpace(performerName))
                return performerName;

            if (vLivePerformer != null)
                return vLivePerformer.PerformerName;

            if (performerAnimator != null)
                return performerAnimator.name;

            return "Performer";
        }
    }

    [Header("Debug View")]
    [FormerlySerializedAs("boneDataList")]
    [SerializeField] private List<LookTargetChannel> lookTargetChannels = new();

    private readonly Dictionary<HumanBodyBones, Transform> _boneMap = new();
    private readonly Dictionary<HumanBodyBones, GameObject> _targetMap = new();
    private readonly Dictionary<HumanBodyBones, PositionConstraint> _constraintMap = new();
    private readonly Dictionary<PositionConstraint, List<ConstraintSourceState>> _constraintSourceMap = new();
    private readonly List<ResolvedPerformerSource> _resolvedPerformerSources = new();

    public Transform LookTargetRoot => lookTargetRoot;
    public IReadOnlyList<LookTargetChannel> LookTargetChannels => lookTargetChannels;

    public static VLiveLookTargetRig Instance => Get();
    public bool IsPerformerLive { get; private set; }
    public int PerformerSourceCount { get; private set; }
    public int ActivePerformerSourceCount { get; private set; }
    public bool ActiveSourcesOnly => activeSourcesOnly;

    private static VLiveLookTargetRig cachedInstance;

    private sealed class ResolvedPerformerSource
    {
        public Animator Animator;
        public Avatar FallbackAvatar;
        public string Name;
        public readonly Dictionary<HumanBodyBones, Transform> BoneMap = new();
    }

    private sealed class ConstraintSourceState
    {
        public readonly Animator Animator;

        public ConstraintSourceState(Animator animator)
        {
            Animator = animator;
        }
    }

    private void Awake()
    {
        TryRegisterSceneSingleton();
    }

    private void OnEnable()
    {
        TryRegisterSceneSingleton();
    }

    private void OnDestroy()
    {
        if (cachedInstance == this)
        {
            cachedInstance = null;
        }
    }

    private void Start()
    {
        if (!enabled)
            return;

        AutoResolvePerformer();
        BuildTargets();
        RefreshLiveState();
    }

    private void Update()
    {
        if (!enabled)
            return;

        if (syncActiveStateEveryFrame)
        {
            RefreshLiveState();
        }
    }

    public static VLiveLookTargetRig Get(Component caller = null)
    {
        if (caller != null)
        {
            VLiveLookTargetRig parentRig = caller.GetComponentInParent<VLiveLookTargetRig>(true);
            if (parentRig != null && parentRig.enabled)
            {
                cachedInstance = parentRig;
                return cachedInstance;
            }
        }

        if (cachedInstance != null && cachedInstance.enabled)
        {
            return cachedInstance;
        }

        cachedInstance = FindEnabledSceneInstance();
        return cachedInstance;
    }

    private bool TryRegisterSceneSingleton()
    {
        if (cachedInstance == null || cachedInstance == this)
        {
            cachedInstance = this;
            return true;
        }

        if (cachedInstance != null && !cachedInstance.enabled)
        {
            cachedInstance = this;
            return true;
        }

        Debug.LogError(
            $"[VLiveLookTargetRig] Only one VLiveLookTargetRig can exist in a scene. Existing: {cachedInstance.name}, duplicate: {name}",
            this);
        enabled = false;
        return false;
    }

    private static VLiveLookTargetRig FindEnabledSceneInstance()
    {
        VLiveLookTargetRig[] instances = FindObjectsOfType<VLiveLookTargetRig>(true);
        for (int i = 0; i < instances.Length; i++)
        {
            if (instances[i] != null && instances[i].enabled)
            {
                return instances[i];
            }
        }

        return instances.Length > 0 ? instances[0] : null;
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
            if (performerAnimator == null)
            {
                performerAnimator = vLivePerformer.PerformerAnimator;
            }

            if (performerAnimator == null)
            {
                performerAnimator = vLivePerformer.GetComponentInChildren<Animator>(true);
            }

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
        AutoResolvePerformer();

        _boneMap.Clear();
        _resolvedPerformerSources.Clear();
        _constraintSourceMap.Clear();

        ResolvePerformerSources(_resolvedPerformerSources);

        if (_resolvedPerformerSources.Count == 0)
        {
            Debug.LogWarning("[VLiveLookTargetRig] Performer Animator is not assigned.", this);
            return;
        }

        bool foundAnyBone = false;
        for (int i = 0; i < _resolvedPerformerSources.Count; i++)
        {
            ResolvedPerformerSource source = _resolvedPerformerSources[i];
            source.BoneMap.Clear();
            CollectBones(source.Animator, source.FallbackAvatar, source.BoneMap);

            if (source.BoneMap.Count > 0)
            {
                foundAnyBone = true;

                if (_boneMap.Count == 0)
                {
                    foreach (var pair in source.BoneMap)
                    {
                        _boneMap[pair.Key] = pair.Value;
                    }
                }
            }
        }

        if (!foundAnyBone)
        {
            Debug.LogWarning("[VLiveLookTargetRig] Bone collect failed.", this);
            return;
        }

        ClearTargets();
        _targetMap.Clear();
        _constraintMap.Clear();
        lookTargetChannels.Clear();

        EnsureRoot();

        foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (bone == HumanBodyBones.LastBone) continue;

            int sourceCount = 0;
            Transform firstBone = null;
            Vector3 positionSum = Vector3.zero;
            Quaternion firstRotation = Quaternion.identity;
            List<ConstraintSourceState> sourceStates = new();

            for (int i = 0; i < _resolvedPerformerSources.Count; i++)
            {
                ResolvedPerformerSource source = _resolvedPerformerSources[i];
                if (!source.BoneMap.TryGetValue(bone, out var sourceBone) || sourceBone == null)
                    continue;

                if (firstBone == null)
                {
                    firstBone = sourceBone;
                    firstRotation = sourceBone.rotation;
                }

                positionSum += sourceBone.position;
                sourceCount++;
            }

            if (sourceCount == 0 || firstBone == null) continue;

            var go = new GameObject($"VLiveTG_{performerName}_{bone}");
            go.transform.SetParent(lookTargetRoot, false);
            go.transform.position = positionSum / sourceCount;
            go.transform.rotation = firstRotation;

            var c = go.AddComponent<PositionConstraint>();
            c.translationAtRest = Vector3.zero;
            c.locked = true;

            for (int i = 0; i < _resolvedPerformerSources.Count; i++)
            {
                ResolvedPerformerSource source = _resolvedPerformerSources[i];
                if (!source.BoneMap.TryGetValue(bone, out var sourceBone) || sourceBone == null)
                    continue;

                c.AddSource(new ConstraintSource
                {
                    sourceTransform = sourceBone,
                    weight = GetInitialSourceWeight(source.Animator)
                });
                sourceStates.Add(new ConstraintSourceState(source.Animator));
            }

            c.constraintActive = true;

            _targetMap[bone] = go;
            _constraintMap[bone] = c;
            _constraintSourceMap[c] = sourceStates;

            lookTargetChannels.Add(new LookTargetChannel
            {
                targetBone = bone,
                performerBone = firstBone,
                lookTargetObject = go,
                sourceCount = sourceCount
            });
        }

        PerformerSourceCount = _resolvedPerformerSources.Count;
        RefreshLiveState();
    }

    // ----------------------------
    // Live State
    // ----------------------------

    public void RefreshLiveState()
    {
        if (_resolvedPerformerSources.Count == 0)
        {
            ResolvePerformerSources(_resolvedPerformerSources);
        }

        int activeCount = 0;
        for (int i = 0; i < _resolvedPerformerSources.Count; i++)
        {
            if (IsAnimatorLive(_resolvedPerformerSources[i].Animator))
            {
                activeCount++;
            }
        }

        PerformerSourceCount = _resolvedPerformerSources.Count;
        ActivePerformerSourceCount = activeCount;
        IsPerformerLive = activeCount > 0;

        foreach (var pair in _constraintSourceMap)
        {
            PositionConstraint constraint = pair.Key;
            if (constraint == null)
                continue;

            List<ConstraintSourceState> sourceStates = pair.Value;
            int sourceCount = Mathf.Min(constraint.sourceCount, sourceStates.Count);
            int liveSourceCount = 0;

            if (activeSourcesOnly)
            {
                for (int i = 0; i < sourceCount; i++)
                {
                    if (IsAnimatorLive(sourceStates[i].Animator))
                    {
                        liveSourceCount++;
                    }
                }
            }

            for (int i = 0; i < sourceCount; i++)
            {
                var s = constraint.GetSource(i);
                s.weight = GetLiveSourceWeight(sourceStates[i].Animator, sourceCount, liveSourceCount);
                constraint.SetSource(i, s);
            }
        }
    }

    // ----------------------------
    // Helpers
    // ----------------------------

    private void ResolvePerformerSources(List<ResolvedPerformerSource> results)
    {
        results.Clear();

        HashSet<Animator> seenAnimators = new();
        AddResolvedPerformerSource(
            vLivePerformer,
            performerAnimator,
            fallbackHumanoidAvatar,
            performerName,
            seenAnimators,
            results);

        if (additionalPerformerSources == null)
            return;

        for (int i = 0; i < additionalPerformerSources.Count; i++)
        {
            LookTargetPerformerSource source = additionalPerformerSources[i];
            if (source == null || !source.SourceEnabled)
                continue;

            Animator sourceAnimator = source.ResolveAnimator();
            Avatar sourceFallback = source.FallbackAvatar != null ? source.FallbackAvatar : fallbackHumanoidAvatar;

            AddResolvedPerformerSource(
                source.Performer,
                sourceAnimator,
                sourceFallback,
                source.ResolveName(),
                seenAnimators,
                results);
        }
    }

    private static void AddResolvedPerformerSource(
        VLivePerformer performer,
        Animator animator,
        Avatar fallback,
        string displayName,
        HashSet<Animator> seenAnimators,
        List<ResolvedPerformerSource> results)
    {
        if (animator == null && performer != null)
        {
            animator = performer.PerformerAnimator;
        }

        if (animator == null && performer != null)
        {
            animator = performer.GetComponentInChildren<Animator>(true);
        }

        if (animator == null || !seenAnimators.Add(animator))
            return;

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = performer != null ? performer.PerformerName : animator.name;
        }

        results.Add(new ResolvedPerformerSource
        {
            Animator = animator,
            FallbackAvatar = fallback,
            Name = displayName
        });
    }

    private float GetInitialSourceWeight(Animator animator)
    {
        if (!syncActiveStateEveryFrame)
            return 1f;

        return activeSourcesOnly
            ? (IsAnimatorLive(animator) ? 1f : 0f)
            : 1f;
    }

    private float GetLiveSourceWeight(Animator animator, int sourceCount, int liveSourceCount)
    {
        if (activeSourcesOnly)
        {
            if (liveSourceCount == 0 || !IsAnimatorLive(animator))
                return 0f;

            return 1f / liveSourceCount;
        }

        return IsPerformerLive && sourceCount > 0 ? 1f / sourceCount : 0f;
    }

    private static bool IsAnimatorLive(Animator animator)
    {
        return animator != null && animator.gameObject.activeInHierarchy;
    }

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

        for (int i = 0; i < lookTargetChannels.Count; i++)
        {
            LookTargetChannel channel = lookTargetChannels[i];
            if (channel.targetBone == bone && channel.lookTargetObject != null)
            {
                _targetMap[bone] = channel.lookTargetObject;
                return channel.lookTargetObject;
            }
        }

        return null;
    }
}
