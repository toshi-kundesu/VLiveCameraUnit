// VLivePerlinNoiseCameraShakeController.cs
using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

[DefaultExecutionOrder(100)]
public sealed class VLivePerlinNoiseCameraShakeController : MonoBehaviour
{
    public enum LiveShakeTarget
    {
        CinemachineVirtualCamera,
        Transform
    }

    [Header("Live Shake Target")]
    [SerializeField] private LiveShakeTarget liveShakeTarget = LiveShakeTarget.CinemachineVirtualCamera;

    [SerializeField] private Transform manualLiveTarget;

    [Header("Live Timeline")]
    public bool syncLiveTimeline = true;

    public PlayableDirector liveTimelineDirector;

    [Header("Live Shake Control")]
    public bool liveShakeEnabled = false;

    public float liveShakeAmplitude = 0.15f;

    public float liveShakeFrequency = 2.5f;

    public float liveShakeDamping = 4f;

    public Vector3 liveShakeAxis = new Vector3(1f, 1f, 0f);

    [Header("Live Perlin Noise Control")]
    public float livePerlinNoiseStrength = 0.02f;

    public float livePerlinNoiseFrequency = 8f;

    private Transform liveTargetTransform;
    private Vector3 initialLocalPosition;
    private float liveShakeEnergy = 0f;
    private float liveShakeSeed;

    private void Start()
    {
        if (liveShakeTarget == LiveShakeTarget.CinemachineVirtualCamera)
        {
            CinemachineVirtualCamera liveVirtualCamera = GetComponent<CinemachineVirtualCamera>();
            liveTargetTransform = liveVirtualCamera != null ? liveVirtualCamera.transform : null;
        }
        else
        {
            liveTargetTransform = manualLiveTarget != null ? manualLiveTarget : transform;
        }

        if (liveTargetTransform == null)
        {
            enabled = false;
            return;
        }

        initialLocalPosition = liveTargetTransform.localPosition;
        liveShakeSeed = Random.value * 1000f;
    }

    private void Update()
    {
        if (liveTargetTransform == null)
            return;

        float liveTime = syncLiveTimeline && liveTimelineDirector != null
            ? (float)liveTimelineDirector.time
            : Time.time;

        if (liveShakeEnabled)
        {
            liveShakeEnergy = Mathf.MoveTowards(liveShakeEnergy, 1f, Time.deltaTime * 3f);
        }
        else
        {
            liveShakeEnergy = Mathf.Exp(-liveShakeDamping * Time.deltaTime) * liveShakeEnergy;
        }

        if (liveShakeEnergy < 0.0001f)
        {
            liveTargetTransform.localPosition = initialLocalPosition;
            return;
        }

        float sine = Mathf.Sin(liveTime * liveShakeFrequency * Mathf.PI * 2f + liveShakeSeed);

        Vector3 shake =
            new Vector3(
                sine * liveShakeAxis.x,
                Mathf.Sin(liveTime * liveShakeFrequency * 1.3f + liveShakeSeed) * liveShakeAxis.y,
                Mathf.Sin(liveTime * liveShakeFrequency * 0.7f + liveShakeSeed) * liveShakeAxis.z
            ) * liveShakeAmplitude * liveShakeEnergy;

        Vector3 perlinTextureNoise = new Vector3(
            Mathf.PerlinNoise(liveShakeSeed, liveTime * livePerlinNoiseFrequency) - 0.5f,
            Mathf.PerlinNoise(liveShakeSeed + 10f, liveTime * livePerlinNoiseFrequency) - 0.5f,
            Mathf.PerlinNoise(liveShakeSeed + 20f, liveTime * livePerlinNoiseFrequency) - 0.5f
        ) * livePerlinNoiseStrength * liveShakeEnergy;

        liveTargetTransform.localPosition = initialLocalPosition + shake + perlinTextureNoise;
    }

    [ContextMenu("Live Perlin Noise Shake/On Air")]
    public void StartLiveShake() => liveShakeEnabled = true;

    [ContextMenu("Live Perlin Noise Shake/Standby")]
    public void StopLiveShake() => liveShakeEnabled = false;

    public void ShakeOn() => StartLiveShake();
    public void ShakeOff() => StopLiveShake();
}
