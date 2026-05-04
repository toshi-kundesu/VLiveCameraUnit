// Assets/toshi.VLiveKit/camera/Runtime/Actor/Switching/VLiveCameraSwitcher.cs
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

public class VLiveCameraSwitcher : MonoBehaviour
{
    public enum LiveCameraInputMode
    {
        NumberRow,
        Numpad,
        CustomHotkeys
    }

    [Serializable]
    private class LiveCameraShot
    {
        [Header("Live Camera")]
        public Camera liveCamera;

        [Header("Program Switch")]
        public bool includeInProgram = true;

        public string liveDisplayName = "CAM_01";

        public string liveLayerNameOverride = "CAM_01";

        public KeyCode liveCutKey = KeyCode.None;
    }

    [Header("Live Program Output")]
    [SerializeField] private Camera programOutputCamera;

    [Header("Live Camera Input")]
    [SerializeField] private LiveCameraInputMode liveCameraInputMode = LiveCameraInputMode.NumberRow;

    [Header("Live Camera Lineup")]
    [SerializeField] private List<LiveCameraShot> liveCameraShots = new List<LiveCameraShot>();

    [Header("Live Monitor UI")]
    [SerializeField] private TextMeshProUGUI programCameraInfoText;

    [SerializeField] private List<TextMeshProUGUI> liveCameraStatusTexts = new List<TextMeshProUGUI>();

    [Header("Live Auto Cut")]
    [SerializeField] private bool useAutoLiveCut = true;

    [SerializeField] private float minLiveCutInterval = 1.0f;

    [SerializeField] private float maxLiveCutInterval = 3.0f;

    [Header("Live UI Colors")]
    [SerializeField] private Color onAirColor = Color.red;

    [SerializeField] private Color standbyColor = Color.white;

    [Header("Live Volume Layer")]
    [SerializeField] private string sharedVolumeLayerName = "CAM_ALL_VOLUME";

    private float liveCutInterval;
    private float liveCutTimer;
    private int currentLiveShotIndex;

    private struct LiveSensorPreset
    {
        public string name;
        public Vector2 size;

        public LiveSensorPreset(string name, float width, float height)
        {
            this.name = name;
            size = new Vector2(width, height);
        }
    }

    private static readonly LiveSensorPreset[] LiveSensorPresets =
    {
        new LiveSensorPreset("Full Frame", 36.0f, 24.0f),
        new LiveSensorPreset("Super 35", 24.9f, 18.7f),
        new LiveSensorPreset("APS-C (Canon)", 22.3f, 14.9f),
        new LiveSensorPreset("APS-C (Nikon/Sony)", 23.5f, 15.6f),
        new LiveSensorPreset("Micro Four Thirds", 17.3f, 13.0f),
        new LiveSensorPreset("Super 16", 12.5f, 7.4f),
        new LiveSensorPreset("1 inch", 13.2f, 8.8f),
        new LiveSensorPreset("2/3 inch", 9.6f, 5.4f),
    };

    private const float SensorMatchTolerance = 0.2f;

    private void Start()
    {
        if (liveCameraShots == null || liveCameraShots.Count == 0)
            return;

        currentLiveShotIndex = Mathf.Clamp(currentLiveShotIndex, 0, liveCameraShots.Count - 1);
        int validIndex = FindNextAvailableLiveShotIndex(currentLiveShotIndex);
        if (validIndex >= 0)
        {
            currentLiveShotIndex = validIndex;
        }

        if (useAutoLiveCut)
        {
            liveCutInterval = UnityEngine.Random.Range(minLiveCutInterval, maxLiveCutInterval);
        }
    }

    private void Update()
    {
        if (liveCameraShots == null || liveCameraShots.Count == 0)
            return;

        if (programOutputCamera == null)
            return;

        if (HandleLiveCameraInput())
        {
            liveCutTimer = 0f;
        }

        if (useAutoLiveCut)
        {
            liveCutTimer += Time.deltaTime;
            if (liveCutTimer >= liveCutInterval)
            {
                liveCutTimer = 0f;

                int nextIndex = FindRandomAvailableLiveShotIndex();
                if (nextIndex >= 0)
                {
                    CutToLiveCameraIndexInternal(nextIndex);
                    liveCutInterval = UnityEngine.Random.Range(minLiveCutInterval, maxLiveCutInterval);
                }
            }
        }

        LiveCameraShot onAirShot = GetCurrentLiveShot();
        Camera onAirCamera = onAirShot != null ? onAirShot.liveCamera : null;
        if (onAirCamera != null)
        {
            ApplyLiveCameraToProgramOutput(onAirShot);
            UpdateProgramInfoText(onAirShot);
            UpdateLiveCameraStatusTexts(onAirCamera);
        }
    }

    private bool HandleLiveCameraInput()
    {
        int maxKeys = Mathf.Min(liveCameraShots.Count, 9);

        switch (liveCameraInputMode)
        {
            case LiveCameraInputMode.NumberRow:
            {
                for (int keyNumber = 1; keyNumber <= maxKeys; keyNumber++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha0 + keyNumber))
                    {
                        int shotIndex = ResolveLiveShotIndexFromNumberKey(keyNumber);
                        if (shotIndex >= 0)
                        {
                            CutToLiveCameraIndexInternal(shotIndex);
                            return true;
                        }
                    }
                }

                return false;
            }

            case LiveCameraInputMode.Numpad:
            {
                for (int keyNumber = 1; keyNumber <= maxKeys; keyNumber++)
                {
                    if (Input.GetKeyDown(KeyCode.Keypad0 + keyNumber))
                    {
                        int shotIndex = ResolveLiveShotIndexFromNumberKey(keyNumber);
                        if (shotIndex >= 0)
                        {
                            CutToLiveCameraIndexInternal(shotIndex);
                            return true;
                        }
                    }
                }

                return false;
            }

            case LiveCameraInputMode.CustomHotkeys:
            {
                for (int i = 0; i < liveCameraShots.Count; i++)
                {
                    LiveCameraShot shot = liveCameraShots[i];
                    if (shot == null || !shot.includeInProgram || shot.liveCamera == null)
                        continue;

                    if (shot.liveCutKey == KeyCode.None)
                        continue;

                    if (Input.GetKeyDown(shot.liveCutKey))
                    {
                        CutToLiveCameraIndexInternal(i);
                        return true;
                    }
                }

                return false;
            }

            default:
                return false;
        }
    }

    private int ResolveLiveShotIndexFromNumberKey(int keyNumber)
    {
        if (liveCameraShots == null || liveCameraShots.Count == 0)
            return -1;

        if (keyNumber < 1 || keyNumber > 9)
            return -1;

        int shotCount = Mathf.Min(liveCameraShots.Count, 9);
        int shotIndex = keyNumber - 1;
        return shotIndex < shotCount ? shotIndex : -1;
    }

    private LiveCameraShot GetCurrentLiveShot()
    {
        if (liveCameraShots == null || liveCameraShots.Count == 0)
            return null;

        if (currentLiveShotIndex < 0 || currentLiveShotIndex >= liveCameraShots.Count)
            return null;

        LiveCameraShot shot = liveCameraShots[currentLiveShotIndex];
        return shot != null && shot.includeInProgram ? shot : null;
    }

    private Camera GetCurrentLiveCamera()
    {
        LiveCameraShot shot = GetCurrentLiveShot();
        return shot != null ? shot.liveCamera : null;
    }

    private int FindNextAvailableLiveShotIndex(int startIndex)
    {
        if (liveCameraShots == null || liveCameraShots.Count == 0)
            return -1;

        int count = liveCameraShots.Count;
        if (startIndex < 0)
            startIndex = 0;
        if (startIndex >= count)
            startIndex = count - 1;

        for (int i = 0; i < count; i++)
        {
            int shotIndex = (startIndex + i) % count;
            LiveCameraShot shot = liveCameraShots[shotIndex];
            if (shot != null && shot.includeInProgram && shot.liveCamera != null)
            {
                return shotIndex;
            }
        }

        return -1;
    }

    private int FindRandomAvailableLiveShotIndex()
    {
        if (liveCameraShots == null || liveCameraShots.Count == 0)
            return -1;

        List<int> availableShotIndexes = new List<int>();
        for (int i = 0; i < liveCameraShots.Count; i++)
        {
            LiveCameraShot shot = liveCameraShots[i];
            if (shot != null && shot.includeInProgram && shot.liveCamera != null)
            {
                availableShotIndexes.Add(i);
            }
        }

        if (availableShotIndexes.Count == 0)
            return -1;

        int randomIndex = UnityEngine.Random.Range(0, availableShotIndexes.Count);
        return availableShotIndexes[randomIndex];
    }

    private void CutToLiveCameraIndexInternal(int index)
    {
        if (liveCameraShots == null || liveCameraShots.Count == 0)
            return;

        int validIndex = FindNextAvailableLiveShotIndex(index);
        if (validIndex < 0)
            return;

        currentLiveShotIndex = validIndex;

        LiveCameraShot onAirShot = GetCurrentLiveShot();
        Camera onAirCamera = onAirShot != null ? onAirShot.liveCamera : null;
        if (onAirCamera != null && programOutputCamera != null)
        {
            ApplyLiveCameraToProgramOutput(onAirShot);
            UpdateProgramInfoText(onAirShot);
            UpdateLiveCameraStatusTexts(onAirCamera);
        }
    }

    private int ResolveLiveCameraLayer(LiveCameraShot shot)
    {
        if (shot == null)
            return -1;

        if (!string.IsNullOrEmpty(shot.liveLayerNameOverride))
        {
            int layer = LayerMask.NameToLayer(shot.liveLayerNameOverride);
            if (layer >= 0 && layer < 32)
            {
                return layer;
            }

            Debug.LogWarning($"[VLiveCameraSwitcher] liveLayerNameOverride '{shot.liveLayerNameOverride}' is missing. Check Project Settings > Tags and Layers.", this);
        }

        return shot.liveCamera != null ? shot.liveCamera.gameObject.layer : -1;
    }

    private static int ResolveLiveLayerByName(string layerName, string context)
    {
        if (string.IsNullOrEmpty(layerName))
            return -1;

        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0 || layer >= 32)
        {
            Debug.LogWarning($"[VLiveCameraSwitcher] {context} layer '{layerName}' is missing. Check Project Settings > Tags and Layers.");
            return -1;
        }

        return layer;
    }

    private void ApplyLiveCameraToProgramOutput(LiveCameraShot sourceShot)
    {
        if (sourceShot == null || sourceShot.liveCamera == null || programOutputCamera == null)
            return;

        Camera sourceCamera = sourceShot.liveCamera;

        programOutputCamera.transform.SetPositionAndRotation(
            sourceCamera.transform.position,
            sourceCamera.transform.rotation);

        programOutputCamera.fieldOfView = sourceCamera.fieldOfView;
        programOutputCamera.usePhysicalProperties = sourceCamera.usePhysicalProperties;
        programOutputCamera.sensorSize = sourceCamera.sensorSize;
        programOutputCamera.focalLength = sourceCamera.focalLength;
        programOutputCamera.aperture = sourceCamera.aperture;

        int liveCameraLayer = ResolveLiveCameraLayer(sourceShot);
        if (liveCameraLayer < 0)
        {
            liveCameraLayer = sourceCamera.gameObject.layer;
        }

        programOutputCamera.gameObject.layer = liveCameraLayer;
        SetLiveVolumeLayerMask(programOutputCamera, liveCameraLayer, sharedVolumeLayerName);
    }

    private static void SetLiveVolumeLayerMask(Camera camera, int liveCameraLayer, string sharedLayerName)
    {
        int mask = 1 << liveCameraLayer;

        if (!string.IsNullOrEmpty(sharedLayerName))
        {
            int sharedLayer = LayerMask.NameToLayer(sharedLayerName);
            if (sharedLayer >= 0 && sharedLayer < 32)
            {
                mask |= 1 << sharedLayer;
            }
            else
            {
                Debug.LogWarning($"[VLiveCameraSwitcher] sharedVolumeLayerName '{sharedLayerName}' is missing. Check Project Settings > Tags and Layers.");
            }
        }

        LayerMask layerMask = mask;

        Type urpType = Type.GetType(
            "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (urpType != null)
        {
            Component urp = camera.GetComponent(urpType);
            if (urp != null)
            {
                PropertyInfo property = urpType.GetProperty("volumeLayerMask");
                if (property != null)
                {
                    property.SetValue(urp, layerMask);
                }
            }
        }

        Type hdrpType = Type.GetType(
            "UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData, Unity.RenderPipelines.HighDefinition.Runtime");
        if (hdrpType != null)
        {
            Component hdrp = camera.GetComponent(hdrpType);
            if (hdrp != null)
            {
                PropertyInfo property = hdrpType.GetProperty("volumeLayerMask");
                if (property != null)
                {
                    property.SetValue(hdrp, layerMask);
                }
            }
        }

        Type postProcessingType = Type.GetType(
            "UnityEngine.Rendering.PostProcessing.PostProcessLayer, Unity.Postprocessing.Runtime");
        if (postProcessingType != null)
        {
            Component postProcessing = camera.GetComponent(postProcessingType);
            if (postProcessing != null)
            {
                FieldInfo field = postProcessingType.GetField(
                    "volumeLayer",
                    BindingFlags.Instance | BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(postProcessing, layerMask);
                }
            }
        }
    }

    private void UpdateProgramInfoText(LiveCameraShot shot)
    {
        if (programCameraInfoText == null)
            return;

        if (shot == null || shot.liveCamera == null)
            return;

        Camera camera = shot.liveCamera;
        Vector2 sensor = camera.sensorSize;
        float focalLength = camera.focalLength;
        float fStop = camera.aperture;

        string sensorText = GetSensorPresetName(sensor) ?? $"{sensor.x:0.#}x{sensor.y:0.#} mm";
        string cameraName = !string.IsNullOrEmpty(shot.liveDisplayName)
            ? shot.liveDisplayName
            : camera.name;

        int liveCameraLayer = ResolveLiveCameraLayer(shot);
        string liveCameraLayerName = liveCameraLayer >= 0 ? LayerMask.LayerToName(liveCameraLayer) : "(layer missing)";

        int sharedLayer = ResolveLiveLayerByName(sharedVolumeLayerName, "sharedVolumeLayerName");
        string sharedLayerName = sharedLayer >= 0 ? LayerMask.LayerToName(sharedLayer) : "(missing)";

        programCameraInfoText.text =
            $"{cameraName}  filter: [{liveCameraLayerName} + {sharedLayerName}]\n" +
            $"{sensorText} / {focalLength:0.0} mm / F{fStop:0.0}";
    }

    private void UpdateLiveCameraStatusTexts(Camera onAirCamera)
    {
        if (liveCameraStatusTexts == null || liveCameraStatusTexts.Count != liveCameraShots.Count)
            return;

        for (int i = 0; i < liveCameraShots.Count; i++)
        {
            LiveCameraShot shot = liveCameraShots[i];
            TextMeshProUGUI statusText = liveCameraStatusTexts[i];
            if (statusText == null || shot == null)
                continue;

            Camera liveCamera = shot.liveCamera;
            bool isOnAir = liveCamera == onAirCamera;

            statusText.color = isOnAir ? onAirColor : standbyColor;

            string prefix = shot.includeInProgram ? string.Empty : "[x] ";
            string name = !string.IsNullOrEmpty(shot.liveDisplayName)
                ? shot.liveDisplayName
                : (liveCamera != null ? liveCamera.name : "(null)");

            statusText.text = prefix + name;
        }
    }

    private static bool IsSensorPresetMatch(Vector2 a, Vector2 b)
    {
        return Mathf.Abs(a.x - b.x) < SensorMatchTolerance &&
               Mathf.Abs(a.y - b.y) < SensorMatchTolerance;
    }

    private static string GetSensorPresetName(Vector2 sensorSize)
    {
        foreach (LiveSensorPreset preset in LiveSensorPresets)
        {
            if (IsSensorPresetMatch(sensorSize, preset.size))
            {
                return preset.name;
            }
        }

        return null;
    }

    public void CutToLiveCameraIndex(int index) => CutToLiveCameraIndexInternal(index);
    public void CutToLiveCameraNumber(int cameraNumber) => CutToLiveCameraIndexInternal(cameraNumber - 1);
    public void CutToLiveCam1() => CutToLiveCameraIndexInternal(0);
    public void CutToLiveCam2() => CutToLiveCameraIndexInternal(1);
    public void CutToLiveCam3() => CutToLiveCameraIndexInternal(2);
    public void CutToLiveCam4() => CutToLiveCameraIndexInternal(3);
    public void CutToLiveCam5() => CutToLiveCameraIndexInternal(4);
    public void CutToLiveCam6() => CutToLiveCameraIndexInternal(5);
    public void CutToLiveCam7() => CutToLiveCameraIndexInternal(6);
    public void CutToLiveCam8() => CutToLiveCameraIndexInternal(7);
    public void CutToLiveCam9() => CutToLiveCameraIndexInternal(8);

    public void SwitchToCameraIndex(int index) => CutToLiveCameraIndex(index);
    public void SwitchToCameraNumber(int cameraNumber) => CutToLiveCameraNumber(cameraNumber);
    public void SwitchToCam1() => CutToLiveCam1();
    public void SwitchToCam2() => CutToLiveCam2();
    public void SwitchToCam3() => CutToLiveCam3();
    public void SwitchToCam4() => CutToLiveCam4();
    public void SwitchToCam5() => CutToLiveCam5();
    public void SwitchToCam6() => CutToLiveCam6();
    public void SwitchToCam7() => CutToLiveCam7();
    public void SwitchToCam8() => CutToLiveCam8();
    public void SwitchToCam9() => CutToLiveCam9();
}
