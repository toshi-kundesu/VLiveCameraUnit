// VLivePerlinNoiseCameraDriftController.cs
using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(100)]
public sealed class VLivePerlinNoiseCameraDriftController : MonoBehaviour
{
    public enum LiveDriftTarget
    {
        VirtualCamera,
        Transform
    }

    public enum LiveDriftSpace
    {
        Global,
        Local
    }

    [Header("Live Drift Target")]
    [SerializeField] private LiveDriftTarget liveDriftTarget = LiveDriftTarget.VirtualCamera;

    [SerializeField] private Transform manualLiveTarget;

    [Header("Live Drift Space")]
    [SerializeField] private LiveDriftSpace liveDriftSpace = LiveDriftSpace.Global;

    [Header("Live Timeline")]
    [SerializeField] private bool syncWithLiveTimeline = false;

    public PlayableDirector liveTimelineDirector;

    [Header("Live Perlin Noise Position Drift")]
    public float liveTimeScale = 1f;

    public float liveSubTimeScale = 1f;

    public Vector3 liveDriftAxis = Vector3.one;

    public Vector3 liveDriftAmplitude = new Vector3(0.1f, 0.1f, 0.1f);

    public Vector3 liveDriftPerlinNoiseFrequency = new Vector3(1f, 1f, 1f);

    public Vector3 liveDriftRangeMin = new Vector3(-0.5f, -0.5f, -0.5f);

    public Vector3 liveDriftRangeMax = new Vector3(0.5f, 0.5f, 0.5f);

    public Vector3 liveDriftPositionOffset;

    [SerializeField] private bool useInitialLivePosition = true;

    [Header("Live Perlin Noise Rotation Drift")]
    [SerializeField] private bool enableLiveRotationDrift = false;

    public Vector3 liveRotationDriftAmplitude = new Vector3(5f, 5f, 5f);

    public Vector3 liveRotationDriftPerlinNoiseFrequency = new Vector3(1f, 1f, 1f);

    [Header("Live Seed")]
    public bool randomizeLiveSeedAtStart = true;

    public int liveSeed = 12345;

    [Header("Disable Behavior")]
    [SerializeField] private bool resetTransformOnDisable = false;

    [SerializeField] private float liveDriftTime = 0f;

    [Header("Live Gizmos")]
    [SerializeField] private bool drawLiveDriftGizmos = true;

    [SerializeField] private Color liveDriftGizmoColor = new Color(1f, 1f, 0f, 0.5f);

    private CinemachineVirtualCamera liveVirtualCamera;
    private Transform liveTargetTransform;
    private Vector3 initialWorldPosition;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Vector3 positionPerlinNoiseOffset;
    private Vector3 rotationPerlinNoiseOffset;

    private void Start()
    {
        switch (liveDriftTarget)
        {
            case LiveDriftTarget.VirtualCamera:
                liveVirtualCamera = GetComponent<CinemachineVirtualCamera>();
                liveTargetTransform = liveVirtualCamera != null ? liveVirtualCamera.transform : null;
                break;

            case LiveDriftTarget.Transform:
                liveTargetTransform = manualLiveTarget != null ? manualLiveTarget : transform;
                break;
        }

        if (liveTargetTransform == null)
        {
            Debug.LogWarning($"{nameof(VLivePerlinNoiseCameraDriftController)}: target Transform was not found.", this);
            enabled = false;
            return;
        }

        initialWorldPosition = liveTargetTransform.position;
        initialLocalPosition = liveTargetTransform.localPosition;
        initialLocalRotation = liveTargetTransform.localRotation;

        if (randomizeLiveSeedAtStart)
        {
            liveSeed = Random.Range(int.MinValue, int.MaxValue);
        }

        System.Random rng = new System.Random(liveSeed);
        positionPerlinNoiseOffset = new Vector3(
            (float)rng.NextDouble() * 1000f,
            (float)rng.NextDouble() * 1000f,
            (float)rng.NextDouble() * 1000f
        );
        rotationPerlinNoiseOffset = new Vector3(
            (float)rng.NextDouble() * 1000f + 2000f,
            (float)rng.NextDouble() * 1000f + 2000f,
            (float)rng.NextDouble() * 1000f + 2000f
        );
    }

    private void Update()
    {
        if (liveTargetTransform == null)
            return;

        if (syncWithLiveTimeline && liveTimelineDirector != null)
        {
            liveDriftTime = (float)liveTimelineDirector.time * liveTimeScale * liveSubTimeScale;
        }
        else
        {
            liveDriftTime += Time.deltaTime * liveTimeScale * liveSubTimeScale;
        }

        float perlinNoiseX = EvaluateCenteredPerlinNoise(liveDriftTime * liveDriftPerlinNoiseFrequency.x + positionPerlinNoiseOffset.x, positionPerlinNoiseOffset.x);
        float perlinNoiseY = EvaluateCenteredPerlinNoise(liveDriftTime * liveDriftPerlinNoiseFrequency.y + positionPerlinNoiseOffset.y, positionPerlinNoiseOffset.y);
        float perlinNoiseZ = EvaluateCenteredPerlinNoise(liveDriftTime * liveDriftPerlinNoiseFrequency.z + positionPerlinNoiseOffset.z, positionPerlinNoiseOffset.z);

        float driftX = perlinNoiseX * liveDriftAmplitude.x * liveDriftAxis.x;
        float driftY = perlinNoiseY * liveDriftAmplitude.y * liveDriftAxis.y;
        float driftZ = perlinNoiseZ * liveDriftAmplitude.z * liveDriftAxis.z;

        float moveX = RemapRange(driftX, -liveDriftAmplitude.x, liveDriftAmplitude.x, liveDriftRangeMin.x, liveDriftRangeMax.x);
        float moveY = RemapRange(driftY, -liveDriftAmplitude.y, liveDriftAmplitude.y, liveDriftRangeMin.y, liveDriftRangeMax.y);
        float moveZ = RemapRange(driftZ, -liveDriftAmplitude.z, liveDriftAmplitude.z, liveDriftRangeMin.z, liveDriftRangeMax.z);

        Vector3 basePosition =
            useInitialLivePosition
                ? (liveDriftSpace == LiveDriftSpace.Global ? initialWorldPosition : initialLocalPosition)
                : Vector3.zero;

        Vector3 calculatedPosition = basePosition + new Vector3(moveX, moveY, moveZ) + liveDriftPositionOffset;

        if (liveDriftSpace == LiveDriftSpace.Global)
        {
            liveTargetTransform.position = calculatedPosition;
        }
        else
        {
            liveTargetTransform.localPosition = calculatedPosition;
        }

        if (enableLiveRotationDrift)
        {
            float rotationX = EvaluateCenteredPerlinNoise(liveDriftTime * liveRotationDriftPerlinNoiseFrequency.x + rotationPerlinNoiseOffset.x, rotationPerlinNoiseOffset.x) * liveRotationDriftAmplitude.x;
            float rotationY = EvaluateCenteredPerlinNoise(liveDriftTime * liveRotationDriftPerlinNoiseFrequency.y + rotationPerlinNoiseOffset.y, rotationPerlinNoiseOffset.y) * liveRotationDriftAmplitude.y;
            float rotationZ = EvaluateCenteredPerlinNoise(liveDriftTime * liveRotationDriftPerlinNoiseFrequency.z + rotationPerlinNoiseOffset.z, rotationPerlinNoiseOffset.z) * liveRotationDriftAmplitude.z;

            liveTargetTransform.localRotation = Quaternion.Euler(rotationX, rotationY, rotationZ) * initialLocalRotation;
        }
    }

    private static float EvaluateCenteredPerlinNoise(float x, float y)
    {
        return Mathf.Lerp(-1f, 1f, Mathf.PerlinNoise(x, y));
    }

    private static float RemapRange(float value, float sourceMin, float sourceMax, float targetMin, float targetMax)
    {
        return (value - sourceMin) / (sourceMax - sourceMin) * (targetMax - targetMin) + targetMin;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (liveDriftTarget == LiveDriftTarget.Transform && manualLiveTarget == null)
        {
            manualLiveTarget = transform;
        }
    }

    private void OnDisable()
    {
        Debug.Log("OnDisable START");

        if (!resetTransformOnDisable)
        {
            Debug.Log("OnDisable: resetTransformOnDisable = false -> skip");
            return;
        }

        Transform target = liveTargetTransform;

        if (target == null)
        {
            Debug.Log("OnDisable: liveTargetTransform is null -> resolving...");

            switch (liveDriftTarget)
            {
                case LiveDriftTarget.VirtualCamera:
                    if (liveVirtualCamera == null)
                    {
                        liveVirtualCamera = GetComponent<CinemachineVirtualCamera>();
                    }
                    target = liveVirtualCamera != null ? liveVirtualCamera.transform : null;
                    break;

                case LiveDriftTarget.Transform:
                    target = manualLiveTarget != null ? manualLiveTarget : transform;
                    break;
            }
        }

        if (target == null)
        {
            Debug.LogWarning("OnDisable: liveTargetTransform still null -> abort");
            return;
        }

        Debug.Log($"OnDisable BEFORE -> pos:{target.position} local:{target.localPosition}");

        target.localPosition = Vector3.zero;
        target.localRotation = Quaternion.identity;

        Debug.Log($"OnDisable AFTER -> pos:{target.position} local:{target.localPosition}");
    }
#endif

    [ContextMenu("Live Perlin Noise Drift/Randomize Seed")]
    public void RandomizeLiveSeed()
    {
        liveSeed = Random.Range(int.MinValue, int.MaxValue);
        Start();
    }

    [ContextMenu("Live Perlin Noise Drift/Capture Current As Base")]
    public void CaptureCurrentAsLiveBase()
    {
        if (liveTargetTransform == null)
            return;

        initialWorldPosition = liveTargetTransform.position;
        initialLocalPosition = liveTargetTransform.localPosition;
        initialLocalRotation = liveTargetTransform.localRotation;
    }

    [ContextMenu("Live Perlin Noise Drift/Restore Base")]
    public void RestoreLiveBase()
    {
        if (liveTargetTransform == null)
            return;

        if (liveDriftSpace == LiveDriftSpace.Global)
        {
            liveTargetTransform.position = initialWorldPosition;
        }
        else
        {
            liveTargetTransform.localPosition = initialLocalPosition;
        }

        liveTargetTransform.localRotation = initialLocalRotation;
    }

    public void RandomizeSeed() => RandomizeLiveSeed();
    public void RebaseInitialTransform() => CaptureCurrentAsLiveBase();
    public void ResetToInitial() => RestoreLiveBase();

    private void OnDrawGizmos()
    {
        if (!drawLiveDriftGizmos)
            return;

        Vector3 center;
        if (Application.isPlaying)
        {
            center = useInitialLivePosition
                ? (liveDriftSpace == LiveDriftSpace.Global ? initialWorldPosition : transform.TransformPoint(initialLocalPosition))
                : transform.position;
        }
        else
        {
            Vector3 baseLocal = useInitialLivePosition ? transform.localPosition : Vector3.zero;
            center = transform.TransformPoint(baseLocal);
        }

        Vector3 size = new Vector3(
            Mathf.Abs(liveDriftRangeMax.x - liveDriftRangeMin.x),
            Mathf.Abs(liveDriftRangeMax.y - liveDriftRangeMin.y),
            Mathf.Abs(liveDriftRangeMax.z - liveDriftRangeMin.z)
        );

        Color previousColor = Gizmos.color;
        Gizmos.color = liveDriftGizmoColor;
        Gizmos.DrawWireCube(center + liveDriftPositionOffset, size);
        Gizmos.color = previousColor;
    }
}
