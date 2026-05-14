// Assets/toshi.VLiveKit/camera/Runtime/Actor/Switching/VLiveCameraSwitcher.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Live-program camera switcher for VLiveCamera test scenes.
/// It maps operator inputs to camera shots, drives optional auto-cut timing, and updates monitor UI.
/// </summary>
public class VLiveCameraSwitcher : MonoBehaviour
{
    public enum LiveCameraInputMode
    {
        NumberRow,
        Numpad,
        CustomHotkeys
    }

    public enum LiveCameraOutputMode
    {
        ProgramOutputCamera,
        DirectCameraEnable
    }

    public enum LiveCameraTransitionType
    {
        Cut,
        CrossFade,
        Random
    }

    public enum LiveAutoCutMode
    {
        RandomInterval,
        SequentialBeat,
        RandomBeat
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

    [SerializeField] private LiveCameraOutputMode liveCameraOutputMode = LiveCameraOutputMode.ProgramOutputCamera;

    [SerializeField] private RawImage programOutputImage;

    [Header("Live Camera Input")]
    [SerializeField] private LiveCameraInputMode liveCameraInputMode = LiveCameraInputMode.NumberRow;

    [Header("Live Camera Lineup")]
    [SerializeField] private List<LiveCameraShot> liveCameraShots = new List<LiveCameraShot>();

    [Header("Live Monitor UI")]
    [SerializeField] private TextMeshProUGUI programCameraInfoText;

    [SerializeField] private List<TextMeshProUGUI> liveCameraStatusTexts = new List<TextMeshProUGUI>();

    [Header("Live Auto Cut")]
    [SerializeField] private bool useAutoLiveCut = true;

    [SerializeField] private LiveAutoCutMode autoLiveCutMode = LiveAutoCutMode.RandomInterval;

    [SerializeField] private float minLiveCutInterval = 1.0f;

    [SerializeField] private float maxLiveCutInterval = 3.0f;

    [SerializeField] private int liveBpm = 120;

    [SerializeField] private int[] liveChangeBeats = { 8 };

    [Header("Live Transition")]
    [SerializeField] private LiveCameraTransitionType liveTransitionType = LiveCameraTransitionType.Cut;

    [SerializeField] private Camera crossFadeCamera;

    [SerializeField] private RawImage crossFadeImage;

    [SerializeField] private float crossFadeDuration = 1.0f;

    [Header("Live UI Colors")]
    [SerializeField] private Color onAirColor = Color.red;

    [SerializeField] private Color standbyColor = Color.white;

    [Header("Live Volume Layer")]
    [SerializeField] private string sharedVolumeLayerName = string.Empty;

    private float liveCutInterval;
    private float liveCutTimer;
    private int currentLiveShotIndex;
    private int currentLiveChangeBeat = 8;
    private RenderTexture programOutputRenderTexture;
    private int programOutputRenderTextureWidth;
    private int programOutputRenderTextureHeight;
    private RenderTexture programOutputPreviousTargetTexture;
    private RenderTexture crossFadeRenderTexture;
    private int crossFadeRenderTextureWidth;
    private int crossFadeRenderTextureHeight;
    private Coroutine liveTransitionCoroutine;

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

        InitializeProgramOutputImage();
        InitializeCrossFadeImage();
        SetLiveCameraEnabledStates(currentLiveShotIndex);
        ApplyCurrentLiveShotNow();
        ScheduleNextLiveCut();
    }

    private void Update()
    {
        if (liveCameraShots == null || liveCameraShots.Count == 0)
            return;

        if (liveCameraOutputMode == LiveCameraOutputMode.ProgramOutputCamera && programOutputCamera == null)
            return;

        if (!IsLiveTransitioning() && HandleLiveCameraInput())
        {
            // Manual cuts reset the auto-cut timer so the operator's choice stays on air for a full interval.
            liveCutTimer = 0f;
            ScheduleNextLiveCut();
        }

        if (!IsLiveTransitioning() && useAutoLiveCut)
        {
            liveCutTimer += Time.deltaTime;
            if (liveCutTimer >= liveCutInterval)
            {
                liveCutTimer = 0f;

                int nextIndex = ResolveNextAutoLiveShotIndex();
                if (nextIndex >= 0)
                {
                    SwitchToLiveCameraIndexInternal(nextIndex);
                    ScheduleNextLiveCut();
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
                            SwitchToLiveCameraIndexInternal(shotIndex);
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
                            SwitchToLiveCameraIndexInternal(shotIndex);
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
                        SwitchToLiveCameraIndexInternal(i);
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
        return FindRandomAvailableLiveShotIndex(false);
    }

    private int FindRandomAvailableLiveShotIndex(bool excludeCurrent)
    {
        if (liveCameraShots == null || liveCameraShots.Count == 0)
            return -1;

        List<int> availableShotIndexes = new List<int>();
        for (int i = 0; i < liveCameraShots.Count; i++)
        {
            LiveCameraShot shot = liveCameraShots[i];
            if (shot != null && shot.includeInProgram && shot.liveCamera != null)
            {
                if (excludeCurrent && i == currentLiveShotIndex && liveCameraShots.Count > 1)
                    continue;

                availableShotIndexes.Add(i);
            }
        }

        if (availableShotIndexes.Count == 0)
            return -1;

        int randomIndex = UnityEngine.Random.Range(0, availableShotIndexes.Count);
        return availableShotIndexes[randomIndex];
    }

    private int ResolveNextAutoLiveShotIndex()
    {
        switch (autoLiveCutMode)
        {
            case LiveAutoCutMode.SequentialBeat:
                return FindNextAvailableLiveShotIndex(currentLiveShotIndex + 1);
            case LiveAutoCutMode.RandomBeat:
                return FindRandomAvailableLiveShotIndex(true);
            case LiveAutoCutMode.RandomInterval:
            default:
                return FindRandomAvailableLiveShotIndex(true);
        }
    }

    private void ScheduleNextLiveCut()
    {
        if (!useAutoLiveCut)
            return;

        switch (autoLiveCutMode)
        {
            case LiveAutoCutMode.SequentialBeat:
            case LiveAutoCutMode.RandomBeat:
                currentLiveChangeBeat = PickLiveChangeBeat();
                liveCutInterval = 60f / Mathf.Max(1, liveBpm) * currentLiveChangeBeat;
                break;
            case LiveAutoCutMode.RandomInterval:
            default:
                float min = Mathf.Max(0f, minLiveCutInterval);
                float max = Mathf.Max(min, maxLiveCutInterval);
                liveCutInterval = UnityEngine.Random.Range(min, max);
                break;
        }
    }

    private int PickLiveChangeBeat()
    {
        if (liveChangeBeats == null || liveChangeBeats.Length == 0)
            return Mathf.Max(1, currentLiveChangeBeat);

        int beat = liveChangeBeats[UnityEngine.Random.Range(0, liveChangeBeats.Length)];
        return Mathf.Max(1, beat);
    }

    private void SwitchToLiveCameraIndexInternal(int index)
    {
        if (liveCameraShots == null || liveCameraShots.Count == 0)
            return;

        if (IsLiveTransitioning())
            return;

        int validIndex = FindNextAvailableLiveShotIndex(index);
        if (validIndex < 0)
            return;

        if (validIndex == currentLiveShotIndex)
            return;

        LiveCameraTransitionType transition = liveTransitionType;
        if (transition == LiveCameraTransitionType.Random)
        {
            transition = UnityEngine.Random.Range(0, 2) == 0
                ? LiveCameraTransitionType.Cut
                : LiveCameraTransitionType.CrossFade;
        }

        if (transition == LiveCameraTransitionType.CrossFade && CanCrossFadeToLiveShot(validIndex))
        {
            liveTransitionCoroutine = StartCoroutine(CrossFadeToLiveCameraCoroutine(validIndex));
            return;
        }

        CutToLiveCameraIndexInternal(validIndex);
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
        SetLiveCameraEnabledStates(currentLiveShotIndex);
        if (onAirCamera != null)
        {
            ApplyLiveCameraToProgramOutput(onAirShot);
            UpdateProgramInfoText(onAirShot);
            UpdateLiveCameraStatusTexts(onAirCamera);
        }
    }

    private bool IsLiveTransitioning()
    {
        return liveTransitionCoroutine != null;
    }

    private bool CanCrossFadeToLiveShot(int index)
    {
        if (crossFadeCamera == null || crossFadeImage == null || crossFadeDuration <= 0f)
            return false;

        LiveCameraShot shot = GetLiveShot(index);
        return shot != null && shot.liveCamera != null;
    }

    private IEnumerator CrossFadeToLiveCameraCoroutine(int index)
    {
        LiveCameraShot nextShot = GetLiveShot(index);
        Camera sourceCamera = nextShot != null ? nextShot.liveCamera : null;
        if (sourceCamera == null || crossFadeCamera == null)
        {
            liveTransitionCoroutine = null;
            yield break;
        }

        InitializeCrossFadeImage();
        if (crossFadeRenderTexture == null)
        {
            CutToLiveCameraIndexInternal(index);
            liveTransitionCoroutine = null;
            yield break;
        }

        bool wasEnabled = crossFadeCamera.enabled;
        Rect previousRect = crossFadeCamera.rect;
        RenderTexture previousTargetTexture = crossFadeCamera.targetTexture;
        CopyLiveCameraToOutputCamera(nextShot, crossFadeCamera);
        crossFadeCamera.rect = new Rect(0f, 0f, 1f, 1f);
        crossFadeCamera.targetTexture = crossFadeRenderTexture;
        crossFadeCamera.enabled = true;

        crossFadeImage.texture = crossFadeRenderTexture;
        crossFadeImage.color = new Color(1f, 1f, 1f, 0f);
        crossFadeImage.gameObject.SetActive(true);

        float startTime = Time.time;
        while (Time.time - startTime < crossFadeDuration)
        {
            CopyLiveCameraToOutputCamera(nextShot, crossFadeCamera);
            crossFadeCamera.rect = new Rect(0f, 0f, 1f, 1f);
            crossFadeCamera.targetTexture = crossFadeRenderTexture;
            crossFadeCamera.enabled = true;

            float alpha = Mathf.Clamp01((Time.time - startTime) / crossFadeDuration);
            crossFadeImage.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        crossFadeCamera.targetTexture = previousTargetTexture;
        crossFadeCamera.rect = previousRect;
        crossFadeCamera.enabled = wasEnabled;
        crossFadeImage.gameObject.SetActive(false);
        CutToLiveCameraIndexInternal(index);
        liveTransitionCoroutine = null;
    }

    private LiveCameraShot GetLiveShot(int index)
    {
        if (liveCameraShots == null || index < 0 || index >= liveCameraShots.Count)
            return null;

        return liveCameraShots[index];
    }

    private void ApplyCurrentLiveShotNow()
    {
        LiveCameraShot onAirShot = GetCurrentLiveShot();
        Camera onAirCamera = onAirShot != null ? onAirShot.liveCamera : null;
        if (onAirCamera == null)
            return;

        ApplyLiveCameraToProgramOutput(onAirShot);
        UpdateProgramInfoText(onAirShot);
        UpdateLiveCameraStatusTexts(onAirCamera);
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
        if (liveCameraOutputMode == LiveCameraOutputMode.DirectCameraEnable)
            return;

        if (sourceShot == null || sourceShot.liveCamera == null || programOutputCamera == null)
            return;

        CopyLiveCameraToOutputCamera(sourceShot, programOutputCamera);
    }

    private void CopyLiveCameraToOutputCamera(LiveCameraShot sourceShot, Camera destinationCamera)
    {
        if (sourceShot == null || sourceShot.liveCamera == null || destinationCamera == null)
            return;

        Camera sourceCamera = sourceShot.liveCamera;
        destinationCamera.transform.SetPositionAndRotation(
            sourceCamera.transform.position,
            sourceCamera.transform.rotation);

        destinationCamera.fieldOfView = sourceCamera.fieldOfView;
        destinationCamera.orthographic = sourceCamera.orthographic;
        destinationCamera.orthographicSize = sourceCamera.orthographicSize;
        destinationCamera.nearClipPlane = sourceCamera.nearClipPlane;
        destinationCamera.farClipPlane = sourceCamera.farClipPlane;
        destinationCamera.clearFlags = sourceCamera.clearFlags;
        destinationCamera.backgroundColor = sourceCamera.backgroundColor;
        destinationCamera.cullingMask = sourceCamera.cullingMask;
        destinationCamera.usePhysicalProperties = sourceCamera.usePhysicalProperties;
        destinationCamera.sensorSize = sourceCamera.sensorSize;
        destinationCamera.focalLength = sourceCamera.focalLength;
        destinationCamera.aperture = sourceCamera.aperture;

        int liveCameraLayer = ResolveLiveCameraLayer(sourceShot);
        if (liveCameraLayer < 0)
        {
            liveCameraLayer = sourceCamera.gameObject.layer;
        }

        destinationCamera.gameObject.layer = liveCameraLayer;
        SetLiveVolumeLayerMask(destinationCamera, liveCameraLayer, sharedVolumeLayerName);
    }

    private void SetLiveCameraEnabledStates(int onAirIndex)
    {
        if (liveCameraOutputMode != LiveCameraOutputMode.DirectCameraEnable)
            return;

        if (liveCameraShots == null)
            return;

        for (int i = 0; i < liveCameraShots.Count; i++)
        {
            LiveCameraShot shot = liveCameraShots[i];
            if (shot == null || shot.liveCamera == null)
                continue;

            shot.liveCamera.enabled = i == onAirIndex;
        }
    }

    private void InitializeProgramOutputImage()
    {
        if (programOutputImage == null || programOutputCamera == null)
            return;

        int textureWidth;
        int textureHeight;
        ResolveOutputTextureSize(out textureWidth, out textureHeight);

        if (programOutputRenderTexture == null ||
            programOutputRenderTextureWidth != textureWidth ||
            programOutputRenderTextureHeight != textureHeight)
        {
            ReleaseProgramOutputRenderTexture();
            programOutputPreviousTargetTexture = programOutputCamera.targetTexture;
            programOutputRenderTexture = new RenderTexture(textureWidth, textureHeight, 0);
            programOutputRenderTextureWidth = textureWidth;
            programOutputRenderTextureHeight = textureHeight;
        }

        programOutputCamera.targetTexture = programOutputRenderTexture;
        programOutputCamera.rect = new Rect(0f, 0f, 1f, 1f);
        programOutputImage.texture = programOutputRenderTexture;
        programOutputImage.gameObject.SetActive(true);
    }

    private void InitializeCrossFadeImage()
    {
        if (crossFadeImage == null)
            return;

        int textureWidth;
        int textureHeight;
        ResolveOutputTextureSize(out textureWidth, out textureHeight);

        if (crossFadeRenderTexture == null ||
            crossFadeRenderTextureWidth != textureWidth ||
            crossFadeRenderTextureHeight != textureHeight)
        {
            ReleaseCrossFadeRenderTexture();
            crossFadeRenderTexture = new RenderTexture(textureWidth, textureHeight, 0);
            crossFadeRenderTextureWidth = textureWidth;
            crossFadeRenderTextureHeight = textureHeight;
        }

        crossFadeImage.texture = crossFadeRenderTexture;
        crossFadeImage.gameObject.SetActive(false);
    }

    private static void ResolveOutputTextureSize(out int width, out int height)
    {
        width = Screen.width;
        height = Screen.height;

        if (width <= 0 || height <= 0)
        {
            width = 1920;
            height = 1080;
        }
    }

    private void ReleaseProgramOutputRenderTexture()
    {
        if (programOutputRenderTexture == null)
            return;

        if (programOutputCamera != null && programOutputCamera.targetTexture == programOutputRenderTexture)
        {
            programOutputCamera.targetTexture = programOutputPreviousTargetTexture;
        }

        programOutputRenderTexture.Release();
        Destroy(programOutputRenderTexture);
        programOutputRenderTexture = null;
        programOutputRenderTextureWidth = 0;
        programOutputRenderTextureHeight = 0;
    }

    private void ReleaseCrossFadeRenderTexture()
    {
        if (crossFadeRenderTexture == null)
            return;

        crossFadeRenderTexture.Release();
        Destroy(crossFadeRenderTexture);
        crossFadeRenderTexture = null;
        crossFadeRenderTextureWidth = 0;
        crossFadeRenderTextureHeight = 0;
    }

    private void OnDisable()
    {
        if (crossFadeImage != null)
        {
            crossFadeImage.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        ReleaseProgramOutputRenderTexture();
        ReleaseCrossFadeRenderTexture();
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

        string sharedLayerName = "(default)";
        if (!string.IsNullOrEmpty(sharedVolumeLayerName))
        {
            int sharedLayer = ResolveLiveLayerByName(sharedVolumeLayerName, "sharedVolumeLayerName");
            sharedLayerName = sharedLayer >= 0 ? LayerMask.LayerToName(sharedLayer) : "(missing)";
        }

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

    [ContextMenu("Set Random Seed From Time")]
    public void SetRandomSeedFromTime()
    {
        UnityEngine.Random.InitState(DateTime.Now.GetHashCode());
    }

    public void CutToLiveCameraIndex(int index) => SwitchToLiveCameraIndexInternal(index);
    public void CutToLiveCameraNumber(int cameraNumber) => SwitchToLiveCameraIndexInternal(cameraNumber - 1);
    public void CutToLiveCam1() => SwitchToLiveCameraIndexInternal(0);
    public void CutToLiveCam2() => SwitchToLiveCameraIndexInternal(1);
    public void CutToLiveCam3() => SwitchToLiveCameraIndexInternal(2);
    public void CutToLiveCam4() => SwitchToLiveCameraIndexInternal(3);
    public void CutToLiveCam5() => SwitchToLiveCameraIndexInternal(4);
    public void CutToLiveCam6() => SwitchToLiveCameraIndexInternal(5);
    public void CutToLiveCam7() => SwitchToLiveCameraIndexInternal(6);
    public void CutToLiveCam8() => SwitchToLiveCameraIndexInternal(7);
    public void CutToLiveCam9() => SwitchToLiveCameraIndexInternal(8);

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
