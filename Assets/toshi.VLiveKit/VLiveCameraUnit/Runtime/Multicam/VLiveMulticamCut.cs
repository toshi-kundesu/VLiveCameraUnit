using System;
using UnityEngine;

namespace toshi.VLiveKit.Photography
{
    [Serializable]
    public class VLiveMulticamCut
    {
        [SerializeField] private double time;
        [SerializeField] private int angleIndex;
        [Min(0f)]
        [SerializeField] private float blendDuration;

        public VLiveMulticamCut()
        {
        }

        public VLiveMulticamCut(double time, int angleIndex, float blendDuration = 0f)
        {
            Time = time;
            AngleIndex = angleIndex;
            BlendDuration = blendDuration;
        }

        public double Time
        {
            get => time;
            set => time = Math.Max(0d, value);
        }

        public int AngleIndex
        {
            get => angleIndex;
            set => angleIndex = Math.Max(0, value);
        }

        public float BlendDuration
        {
            get => blendDuration;
            set => blendDuration = Mathf.Max(0f, value);
        }

        public VLiveMulticamCut Clone()
        {
            return new VLiveMulticamCut(time, angleIndex, blendDuration);
        }
    }
}
