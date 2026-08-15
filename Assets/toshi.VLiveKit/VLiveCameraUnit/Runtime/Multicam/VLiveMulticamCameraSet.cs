using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

namespace toshi.VLiveKit.Photography
{
    [DisallowMultipleComponent]
    public class VLiveMulticamCameraSet : MonoBehaviour
    {
        private static readonly Color[] DefaultPalette =
        {
            new Color(0.95f, 0.28f, 0.24f),
            new Color(0.2f, 0.55f, 0.95f),
            new Color(0.22f, 0.72f, 0.36f),
            new Color(0.96f, 0.72f, 0.2f),
            new Color(0.78f, 0.42f, 0.92f),
            new Color(0.1f, 0.72f, 0.72f),
            new Color(0.98f, 0.48f, 0.18f),
            new Color(0.65f, 0.65f, 0.65f),
        };

        [Header("Angles")]
        [SerializeField] private List<VLiveMulticamAngle> angles = new List<VLiveMulticamAngle>();

        [Header("Priority Switching")]
        [SerializeField] private int activePriority = 100;
        [SerializeField] private int standbyPriority = 0;

        public IReadOnlyList<VLiveMulticamAngle> Angles => angles;
        public int AngleCount => angles.Count;
        public int ActivePriority => activePriority;
        public int StandbyPriority => standbyPriority;

        public VLiveMulticamAngle GetAngle(int index)
        {
            return IsValidAngleIndex(index) ? angles[index] : null;
        }

        public bool IsValidAngleIndex(int index)
        {
            return index >= 0 && index < angles.Count;
        }

        public void AutoCollectChildCameras(bool includeInactive = true)
        {
            angles.Clear();

            CinemachineVirtualCameraBase[] cameras =
                GetComponentsInChildren<CinemachineVirtualCameraBase>(includeInactive);

            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] == null)
                {
                    continue;
                }

                AddAngle(cameras[i]);
            }
        }

        public int AddAngle(CinemachineVirtualCameraBase camera)
        {
            int index = angles.Count;
            Color color = DefaultPalette[index % DefaultPalette.Length];
            KeyCode hotKey = GetDefaultHotKey(index);
            string displayName = camera != null ? camera.gameObject.name : $"Angle {index + 1}";

            angles.Add(new VLiveMulticamAngle(displayName, camera, color, hotKey));
            return index;
        }

        public static KeyCode GetDefaultHotKey(int index)
        {
            switch (index)
            {
                case 0: return KeyCode.Alpha1;
                case 1: return KeyCode.Alpha2;
                case 2: return KeyCode.Alpha3;
                case 3: return KeyCode.Alpha4;
                case 4: return KeyCode.Alpha5;
                case 5: return KeyCode.Alpha6;
                case 6: return KeyCode.Alpha7;
                case 7: return KeyCode.Alpha8;
                case 8: return KeyCode.Alpha9;
                case 9: return KeyCode.Alpha0;
                case 10: return KeyCode.Q;
                case 11: return KeyCode.W;
                case 12: return KeyCode.E;
                case 13: return KeyCode.R;
                case 14: return KeyCode.T;
                case 15: return KeyCode.Y;
                default: return KeyCode.None;
            }
        }
    }
}
