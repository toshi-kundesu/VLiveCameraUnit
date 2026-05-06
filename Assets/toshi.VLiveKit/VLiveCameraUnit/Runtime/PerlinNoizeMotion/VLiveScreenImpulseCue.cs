// VLiveScreenImpulseCue.cs
using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineVirtualCamera))]
public sealed class VLiveScreenImpulseCue : MonoBehaviour
{
    public enum LiveImpulseFilterMode
    {
        None,
        ExponentialLowPass,
        DampedSpring
    }

    [Header("Live Timeline")]
    public PlayableDirector liveTimelineDirector;

    [Header("Live Screen Base")]
    [Range(0f, 1f)] public float liveBaseScreenX = 0.5f;

    [Range(0f, 1f)] public float liveBaseScreenY = 0.5f;

    [Header("Live Impulse Range")]
    [Min(0f)] public float liveImpulseXMin = 0.05f;

    [Min(0f)] public float liveImpulseXMax = 0.15f;

    [Min(0f)] public float liveImpulseYMin = 0.02f;

    [Min(0f)] public float liveImpulseYMax = 0.10f;

    [Header("Live Impulse Envelope")]
    [Min(0.01f)] public float liveAttack = 0.20f;

    [Min(0.00f)] public float liveHold = 0.10f;

    [Min(0.01f)] public float liveReturn = 0.40f;

    [Min(0.00f)] public float liveBaseHold = 0.20f;

    [Header("Live Impulse Shape")]
    [Range(2f, 16f)] public float liveAttackSharpness = 8f;

    [Range(1e-4f, 0.1f)] public float liveReturnEpsilon = 0.02f;

    [Header("Live Random")]
    public int liveSeed = 12345;

    [Header("Live Output Filter")]
    public LiveImpulseFilterMode liveFilterMode = LiveImpulseFilterMode.DampedSpring;

    [Min(0.001f)] public float lowPassTimeConstant = 0.12f;

    [Min(0.01f)] public float springFrequency = 2.0f;

    [Range(0.1f, 2.0f)] public float springDampingRatio = 1.0f;

    public bool resetOnLargeTimelineJump = true;

    [Min(0.01f)] public float timelineJumpThreshold = 0.5f;

#if UNITY_EDITOR
    [Header("Debug (Read-Only)")]
    [SerializeField] private int debugLiveCycle;

    [SerializeField] private float debugLivePhase;

    [SerializeField] private float debugLiveEnvelope;

    [SerializeField] private float debugRawScreenX;

    [SerializeField] private float debugRawScreenY;

    [SerializeField] private float debugFilteredScreenX;

    [SerializeField] private float debugFilteredScreenY;
#endif

    private CinemachineComposer composer;
    private float liveCycleLength;
    private double previousLiveTimelineTime = double.NaN;
    private float outputScreenX;
    private float outputScreenY;
    private float velocityScreenX;
    private float velocityScreenY;

    private void Awake()
    {
        CinemachineVirtualCamera liveVirtualCamera = GetComponent<CinemachineVirtualCamera>();
        composer = liveVirtualCamera.GetCinemachineComponent<CinemachineComposer>();
        if (!liveTimelineDirector)
        {
            liveTimelineDirector = GetComponentInParent<PlayableDirector>();
        }

        liveCycleLength = Mathf.Max(liveAttack + liveHold + liveReturn + liveBaseHold, 1e-4f);

        outputScreenX = liveBaseScreenX;
        outputScreenY = liveBaseScreenY;
        velocityScreenX = velocityScreenY = 0f;
        previousLiveTimelineTime = double.NaN;
    }

    private void OnValidate()
    {
        liveCycleLength = Mathf.Max(liveAttack + liveHold + liveReturn + liveBaseHold, 1e-4f);
    }

    private void LateUpdate()
    {
        if (composer == null)
            return;

        double liveTimelineTime = liveTimelineDirector ? liveTimelineDirector.time : Time.timeAsDouble;

        float deltaTime;
        if (double.IsNaN(previousLiveTimelineTime))
        {
            deltaTime = 0f;
        }
        else
        {
            deltaTime = Mathf.Clamp((float)(liveTimelineTime - previousLiveTimelineTime), -1f, 1f);
        }

        if (resetOnLargeTimelineJump && Mathf.Abs(deltaTime) > timelineJumpThreshold)
        {
            outputScreenX = liveBaseScreenX;
            outputScreenY = liveBaseScreenY;
            velocityScreenX = velocityScreenY = 0f;
            deltaTime = 0f;
        }

        previousLiveTimelineTime = liveTimelineTime;

        int liveCycle = GetLiveCycleIndex(liveTimelineTime, liveCycleLength);
        float livePhase = GetLivePhaseWithinCycle(liveTimelineTime, liveCycleLength);

        float directionX = ((liveCycle & 1) == 0) ? +1f : -1f;
        float directionY = ((liveCycle & 1) == 0) ? -1f : +1f;

        float impulseX = Mathf.Lerp(liveImpulseXMin, liveImpulseXMax, Hash01(liveCycle ^ liveSeed));
        float impulseY = Mathf.Lerp(liveImpulseYMin, liveImpulseYMax, Hash01((liveCycle + 1000) ^ liveSeed));

        float envelope = EvaluateLiveEnvelope(
            livePhase,
            liveAttack,
            liveHold,
            liveReturn,
            liveBaseHold,
            liveAttackSharpness,
            liveReturnEpsilon);

        float rawScreenX = Mathf.Clamp01(liveBaseScreenX + directionX * impulseX * envelope);
        float rawScreenY = Mathf.Clamp01(liveBaseScreenY + directionY * impulseY * envelope);

        switch (liveFilterMode)
        {
            case LiveImpulseFilterMode.None:
            {
                outputScreenX = rawScreenX;
                outputScreenY = rawScreenY;
                break;
            }

            case LiveImpulseFilterMode.ExponentialLowPass:
            {
                if (deltaTime > 0f)
                {
                    float alpha = 1f - Mathf.Exp(-deltaTime / Mathf.Max(1e-4f, lowPassTimeConstant));
                    outputScreenX += (rawScreenX - outputScreenX) * alpha;
                    outputScreenY += (rawScreenY - outputScreenY) * alpha;
                }
                else
                {
                    outputScreenX = rawScreenX;
                    outputScreenY = rawScreenY;
                    velocityScreenX = velocityScreenY = 0f;
                }

                break;
            }

            case LiveImpulseFilterMode.DampedSpring:
            {
                if (deltaTime > 0f)
                {
                    float angularFrequency = 2f * Mathf.PI * Mathf.Max(0.01f, springFrequency);
                    float dampingRatio = Mathf.Clamp(springDampingRatio, 0.1f, 2.0f);
                    float angularFrequencySquared = angularFrequency * angularFrequency;
                    float damping = 2f * dampingRatio * angularFrequency;

                    velocityScreenX += (-damping * velocityScreenX - angularFrequencySquared * (outputScreenX - rawScreenX)) * deltaTime;
                    outputScreenX += velocityScreenX * deltaTime;

                    velocityScreenY += (-damping * velocityScreenY - angularFrequencySquared * (outputScreenY - rawScreenY)) * deltaTime;
                    outputScreenY += velocityScreenY * deltaTime;
                }
                else
                {
                    outputScreenX = rawScreenX;
                    outputScreenY = rawScreenY;
                    velocityScreenX = velocityScreenY = 0f;
                }

                break;
            }
        }

        composer.m_ScreenX = Mathf.Clamp01(outputScreenX);
        composer.m_ScreenY = Mathf.Clamp01(outputScreenY);

#if UNITY_EDITOR
        debugLiveCycle = liveCycle;
        debugLivePhase = livePhase;
        debugLiveEnvelope = envelope;
        debugRawScreenX = rawScreenX;
        debugRawScreenY = rawScreenY;
        debugFilteredScreenX = outputScreenX;
        debugFilteredScreenY = outputScreenY;
#endif
    }

    private static float EvaluateLiveEnvelope(float phase, float attack, float hold, float ret, float baseHold, float sharpness, float epsilon)
    {
        float start = 0f;
        float attackEnd = start + attack;
        float holdEnd = attackEnd + hold;
        float returnEnd = holdEnd + ret;

        if (phase < attackEnd)
        {
            float u = phase / Mathf.Max(attack, 1e-6f);
            return 1f - Mathf.Exp(-sharpness * u);
        }

        if (phase < holdEnd)
        {
            return 1f;
        }

        if (phase < returnEnd)
        {
            float dt = phase - holdEnd;
            float lambda = -Mathf.Log(Mathf.Clamp(epsilon, 1e-6f, 1f)) / Mathf.Max(ret, 1e-6f);
            return Mathf.Exp(-lambda * dt);
        }

        return 0f;
    }

    private static int GetLiveCycleIndex(double timeNow, float cycleLength)
    {
        return (int)Mathf.Floor((float)timeNow / Mathf.Max(cycleLength, 1e-6f));
    }

    private static float GetLivePhaseWithinCycle(double timeNow, float cycleLength)
    {
        float length = Mathf.Max(cycleLength, 1e-6f);
        float remainder = (float)(timeNow % length);
        if (remainder < 0f)
        {
            remainder += length;
        }

        return remainder;
    }

    private static float Hash01(int n)
    {
        uint x = (uint)n;
        x ^= x >> 16;
        x *= 0x7feb352dU;
        x ^= x >> 15;
        x *= 0x846ca68bU;
        x ^= x >> 16;
        return (x & 0xFFFFFF) / (float)0x1000000;
    }
}
