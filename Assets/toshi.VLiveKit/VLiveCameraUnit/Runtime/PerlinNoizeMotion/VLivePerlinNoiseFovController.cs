// VLivePerlinNoiseFovController.cs
using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

[DefaultExecutionOrder(100)]
public sealed class VLivePerlinNoiseFovController : MonoBehaviour
{
    [Header("Live Camera")]
    [SerializeField] private CinemachineVirtualCamera liveVirtualCamera;

    [SerializeField] private PlayableDirector liveTimeline;

    [Header("Live Time")]
    public bool syncWithLiveTimeline = true;

    [Header("Live FOV Range")]
    [Min(1f)] public float minLiveFov = 30f;

    [Min(1f)] public float maxLiveFov = 60f;

    [Header("Live Perlin Noise Control")]
    public float liveTimeScale = 1f;

    public float fovPerlinNoiseScale = 1f;

    public float fovPerlinNoiseOffset = 0f;

#if UNITY_EDITOR
    [Header("Debug (Read-Only)")]
    [SerializeField] private float debugLiveTimelineTime;

    [SerializeField] private float debugSignedPerlinNoise;
#endif

    private void Awake()
    {
        if (liveVirtualCamera == null)
        {
            liveVirtualCamera = GetComponent<CinemachineVirtualCamera>();
        }

        if (liveVirtualCamera == null)
        {
            Debug.LogError("[VLivePerlinNoiseFovController] CinemachineVirtualCamera was not found.", this);
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (liveVirtualCamera == null)
            return;

        if (maxLiveFov < minLiveFov)
        {
            (minLiveFov, maxLiveFov) = (maxLiveFov, minLiveFov);
        }

        double baseTime;
        if (syncWithLiveTimeline && liveTimeline != null)
        {
            baseTime = liveTimeline.time * liveTimeScale;
#if UNITY_EDITOR
            debugLiveTimelineTime = (float)baseTime;
#endif
        }
        else
        {
            baseTime = Time.timeSinceLevelLoad * liveTimeScale;
        }

        float perlinNoiseValue = Mathf.PerlinNoise((float)baseTime * fovPerlinNoiseScale + fovPerlinNoiseOffset, 0f);
        float signedPerlinNoise = perlinNoiseValue * 2f - 1f;

#if UNITY_EDITOR
        debugSignedPerlinNoise = signedPerlinNoise;
#endif

        float amplitude = (maxLiveFov - minLiveFov) * 0.5f;
        float center = (maxLiveFov + minLiveFov) * 0.5f;
        float fovOffset = amplitude * signedPerlinNoise;

        liveVirtualCamera.m_Lens.FieldOfView = center + fovOffset;
    }

    [ContextMenu("Live Perlin Noise FOV/Find Virtual Camera")]
    private void FindLivePerlinNoiseVirtualCamera()
    {
        liveVirtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
    }

    [ContextMenu("GetVirtualCamera")]
    private void GetVirtualCamera()
    {
        FindLivePerlinNoiseVirtualCamera();
    }
}
