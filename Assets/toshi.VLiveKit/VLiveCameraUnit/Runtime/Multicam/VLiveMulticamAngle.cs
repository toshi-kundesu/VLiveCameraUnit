using System;
using Cinemachine;
using UnityEngine;

namespace toshi.VLiveKit.Photography
{
    [Serializable]
    public class VLiveMulticamAngle
    {
        [SerializeField] private string displayName;
        [SerializeField] private CinemachineVirtualCameraBase virtualCamera;
        [SerializeField] private Color color = Color.gray;
        [SerializeField] private KeyCode hotKey = KeyCode.None;

        public VLiveMulticamAngle()
        {
        }

        public VLiveMulticamAngle(
            string displayName,
            CinemachineVirtualCameraBase virtualCamera,
            Color color,
            KeyCode hotKey)
        {
            this.displayName = displayName;
            this.virtualCamera = virtualCamera;
            this.color = color;
            this.hotKey = hotKey;
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(displayName))
                {
                    return displayName;
                }

                return virtualCamera != null ? virtualCamera.gameObject.name : "Angle";
            }
            set => displayName = value;
        }

        public CinemachineVirtualCameraBase VirtualCamera
        {
            get => virtualCamera;
            set => virtualCamera = value;
        }

        public Color Color
        {
            get => color;
            set => color = value;
        }

        public KeyCode HotKey
        {
            get => hotKey;
            set => hotKey = value;
        }

        public bool IsValid => virtualCamera != null;
    }
}
