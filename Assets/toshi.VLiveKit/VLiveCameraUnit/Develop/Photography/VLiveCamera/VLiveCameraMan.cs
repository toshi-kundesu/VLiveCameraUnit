// VLiveKit is all Unlicense.
// unlicense: https://unlicense.org/
// this comment & namespace can be removed. you can use this code freely.
// last update: 2024/11/04

using UnityEngine;
using UnityEngine.Playables;
using toshi.VLiveKit.Photography;

namespace toshi.VLiveKit.Photography
{
public class VLiveCameraMan : MonoBehaviour
{
    public bool useRandomTimeOffset = true;
    public Transform target; // 追跡するオブジェクト
    public Vector3 offset; // オフセット
    [Range(0, 10)]
    public float xDamping = 5.0f; // X軸のダンピングの値
    [Range(0, 10)]
    public float yDamping = 5.0f; // Y軸のダンピングの値
    [Range(0.1f, 89)]
    public float minFov = 15.0f; // 最小視野角
    [Range(0.1f, 89)]
    public float maxFov = 89.0f; // 最大視野角
    public float zoomSpeed = 0.1f; // ズームの速度
    private Camera cam; // カメラコンポーネント
    private float time; // パーリンノイズの時間パラメータ
    // time for zoom
    private float zoomTime;
    // time for perlin noise
    private float perlinPosTime;

    private Vector3 cameraWorldPos;
    [SerializeField]
    private Color activeColor = Color.green;
    [SerializeField]
    private Color inactiveColor = Color.red;
    // カメラ位置に出現させるprefab
    [SerializeField]
    private GameObject cameraPrefab;
    // カメラを出現させるかどうか
    [SerializeField]
    private bool spawnCamera = false;

    public bool autoFocus = false; // フォーカスを自動化するスイッチ
    private float focusDistance; // フォーカスディスタンス

    public AnimationCurve fovCurve = AnimationCurve.Linear(0, 0, 1, 1); // 視野角のカーブ

    // PerlinNoizeMotionのプロパティ
    public bool useRotation = false;
    public Vector3 positionAmplitude = Vector3.one;
    public Vector3 rotationAmplitude = Vector3.one * 10.0f;
    [SerializeField] [Range(0.01f, 1.0f)]
    public float frequency = 1.0f;
    public bool useRandomMotion = true;
    public bool syncTimeline = false;
    public bool manualSeed = false;
    public int manualSeedValue = 0;
    [SerializeField]
    private PlayableDirector timelineDirector;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float randomOffsetX;
    private float randomOffsetY;

    void Start()
    {
        // get camera component
        cam = GetComponent<Camera>();
        // set random time offset or not
        if (useRandomTimeOffset)
        {
            time = Random.Range(0f, 100f); // ランダムな初期時間
            zoomTime = Random.Range(0f, 100f); // ランダムな初期時間
            perlinPosTime = Random.Range(0f, 100f); // ランダムな初期時間
        }
        else
        {
            time = 0f;
            zoomTime = 0f;
            perlinPosTime = 0f;
        }

        // set initial position and rotation
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;

        // spawn camera prefab
        if (spawnCamera)
        {
            if (!cameraPrefab)
            {
                cameraPrefab = Resources.Load<GameObject>("Assets/VLiveKit/VLiveCamera/Prefabs/Rig_bg_camera01.prefab");
            }
            if (cameraPrefab)
            {
                GameObject instantiatedPrefab = Instantiate(cameraPrefab, transform.position, Quaternion.identity);
                instantiatedPrefab.transform.parent = transform;
                float cameraSize = 0.5f;
                instantiatedPrefab.transform.localScale = Vector3.one * cameraSize;
                instantiatedPrefab.transform.localEulerAngles = new Vector3(0, 90, 0);
                instantiatedPrefab.transform.localPosition = new Vector3(0, 0, -0.5f);
            }
        }

        // set random offset for perlin noise
        if (useRandomMotion)
        {
            int randomSeed = manualSeed ? manualSeedValue : Random.Range(0, int.MaxValue);
            Random.InitState(randomSeed);
            randomOffsetX = Random.value * 1000.0f;
            randomOffsetY = Random.value * 1000.0f;
        }
    }

    void Update()
    {
        

        
    }

    void LateUpdate()
    {
        // sync timeline or not
        if (syncTimeline && timelineDirector != null)
        {
            // get time from timeline
            time = (float)timelineDirector.time * frequency;
            zoomTime = (float)timelineDirector.time * zoomSpeed;
            perlinPosTime = (float)timelineDirector.time * frequency;
        }
        else
        {
            // update time
            if (Application.isPlaying)
            {
                time += Time.deltaTime * frequency;
                zoomTime += Time.deltaTime * zoomSpeed;
                perlinPosTime += Time.deltaTime * frequency;
            }
        }
        float offsetX = useRandomMotion ? randomOffsetX : 0.0f;
        float offsetY = useRandomMotion ? randomOffsetY : 0.0f;

        float x = initialPosition.x + Remap(Mathf.PerlinNoise(time + offsetX, 0), 0, 1, -positionAmplitude.x, positionAmplitude.x);
        float y = initialPosition.y + Remap(Mathf.PerlinNoise(0, time + offsetY), 0, 1, -positionAmplitude.y, positionAmplitude.y);
        float z = initialPosition.z + Remap(Mathf.PerlinNoise(time + offsetX, time + offsetY), 0, 1, -positionAmplitude.z, positionAmplitude.z);

        transform.localPosition = new Vector3(x, y, z);

        if (useRotation)
        {
            float rx = Remap(Mathf.PerlinNoise(time + offsetX, 0), 0, 1, -rotationAmplitude.x, rotationAmplitude.x);
            float ry = Remap(Mathf.PerlinNoise(0, time + offsetY), 0, 1, -rotationAmplitude.y, rotationAmplitude.y);
            float rz = Remap(Mathf.PerlinNoise(time + offsetX, time + offsetY), 0, 1, -rotationAmplitude.z, rotationAmplitude.z);

            Quaternion targetRotation = Quaternion.Euler(rx, ry, rz) * initialRotation;
            transform.localRotation = targetRotation;
        }

        cameraWorldPos = transform.position;
        if (target)
        {
            Vector3 targetPositionWithOffset = target.position + offset;
            Quaternion targetRotation = Quaternion.LookRotation(targetPositionWithOffset - transform.position);
            float xRotation = Mathf.LerpAngle(transform.rotation.eulerAngles.x, targetRotation.eulerAngles.x, Time.deltaTime * xDamping);
            float yRotation = Mathf.LerpAngle(transform.rotation.eulerAngles.y, targetRotation.eulerAngles.y, Time.deltaTime * yDamping);
            transform.rotation = Quaternion.Euler(xRotation, yRotation, targetRotation.eulerAngles.z);

            // time += Time.deltaTime * zoomSpeed;
            float noise = Mathf.PerlinNoise(zoomTime, 0f);
            float curveValue = fovCurve.Evaluate(noise);
            cam.fieldOfView = Mathf.Lerp(minFov, maxFov, curveValue);

            if (autoFocus)
            {
                focusDistance = Vector3.Distance(transform.position, target.position);
                cam.focusDistance = focusDistance;
            }
        }

        

        
    }

    float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    void OnDrawGizmos()
    {
        if (cam == null)
        {
            return;
        }
        // カメラがアクティブなら緑色、非アクティブなら赤色で表示
        // playmode中のみ表示
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(cameraWorldPos, 0.25f);
        }
        else
        {
            Gizmos.color = cam.isActiveAndEnabled ? activeColor : inactiveColor;
            Gizmos.DrawSphere(cameraWorldPos, 0.25f);
        }
        
        // 表示
    }
    }
}// namespace toshi.VLiveKit.Photography