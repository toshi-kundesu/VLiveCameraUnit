// VLivePerlinNoiseDutchRollController.cs
using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineVirtualCamera))]
public sealed class VLivePerlinNoiseDutchRollController : MonoBehaviour
{
    [Header("Live Timeline")]
    public PlayableDirector cueDirector;

    [Header("Live Time")]
    public float liveTimeScale = 1f;

    public float liveSubTimeScale = 1f;

    [Header("Live Intensity")]
    public float liveIntensityScale = 1f;

    public float liveSubIntensityScale = 1f;

    [Header("Dutch Roll")]
    public float baseRollDegrees = 0f;

    [Min(0f)] public float rollAmplitudeDegrees = 5f;

    [Header("Perlin Noise Control")]
    public float rollPerlinNoiseSpeed = 0.2f;

    public float rollPerlinNoiseScale = 1.0f;

    public float rollPerlinNoiseOffset = 0f;

#if UNITY_EDITOR
    [Header("Debug (Read-Only)")]
    [SerializeField] private float debugLiveTime;

    [SerializeField] private float debugPerlinNoiseRaw;

    [SerializeField] private float debugPerlinNoiseSigned;

    [SerializeField] private float debugRollDegrees;
#endif

    private CinemachineVirtualCamera liveVirtualCamera;

    private void Awake()
    {
        liveVirtualCamera = GetComponent<CinemachineVirtualCamera>();
        if (!cueDirector)
        {
            cueDirector = GetComponentInParent<PlayableDirector>();
        }
    }

    private void LateUpdate()
    {
        if (liveVirtualCamera == null)
            return;

        float baseTime = cueDirector ? (float)cueDirector.time : Time.time;
        float liveTime = baseTime * liveTimeScale * liveSubTimeScale;

        float perlinNoiseRaw = Mathf.PerlinNoise(liveTime * rollPerlinNoiseSpeed * rollPerlinNoiseScale + rollPerlinNoiseOffset, 0f);
        float perlinNoiseSigned = perlinNoiseRaw * 2f - 1f;

        float rollDegrees = baseRollDegrees +
                            rollAmplitudeDegrees *
                            perlinNoiseSigned *
                            liveIntensityScale *
                            liveSubIntensityScale;

        LensSettings lens = liveVirtualCamera.m_Lens;
        lens.Dutch = rollDegrees;
        liveVirtualCamera.m_Lens = lens;

#if UNITY_EDITOR
        debugLiveTime = liveTime;
        debugPerlinNoiseRaw = perlinNoiseRaw;
        debugPerlinNoiseSigned = perlinNoiseSigned;
        debugRollDegrees = rollDegrees;
#endif
    }
}
