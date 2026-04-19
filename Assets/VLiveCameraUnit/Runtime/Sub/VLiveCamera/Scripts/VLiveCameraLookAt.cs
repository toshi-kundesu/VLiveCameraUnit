// VLiveKit is all Unlicense.
// unlicense: https://unlicense.org/
// this comment & namespace can be removed. you can use this code freely.

using UnityEngine;
using toshi.VLiveKit.Photography;

namespace toshi.VLiveKit.Photography
{
public class VLiveCameraLookAt : MonoBehaviour
{
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

    void Start()
    {
        cam = GetComponent<Camera>();
        time = Random.Range(0f, 100f); // ランダムな初期時間
        // カメラprefabがなかった場合、指定したパスにあるprefabを読み込む
        if (spawnCamera)
        {
            if (!cameraPrefab)
            {
                cameraPrefab = Resources.Load<GameObject>("Assets/VLiveKit/VLiveCamera/Prefabs/Rig_bg_camera01.prefab");
            }
            if (cameraPrefab)
            {
                // カメラ位置にprefabを出現させる
                GameObject instantiatedPrefab = Instantiate(cameraPrefab, transform.position, Quaternion.identity);
                // インスタンスを、カメラの子オブジェクトにする
                instantiatedPrefab.transform.parent = transform;
                // インスタンスのサイズを小さくする
                float cameraSize = 0.25f;
                instantiatedPrefab.transform.localScale = Vector3.one * cameraSize;
                // 角度を変更する
                instantiatedPrefab.transform.localEulerAngles = new Vector3(0, 90, 0);
                // 位置を変更する
                instantiatedPrefab.transform.localPosition = new Vector3(0, 0, -0.5f);
                // マテリアルカラーを変更する
                // Material material = instantiatedPrefab.GetComponent<Renderer>().material;
                // material.color = Color.black;
            }
        }
    }

    void Update()
    {
        if (target)
        {
            // ターゲットの位置にオフセットを追加
            Vector3 targetPositionWithOffset = target.position + offset;
            // ターゲットを見つめる
            Quaternion targetRotation = Quaternion.LookRotation(targetPositionWithOffset - transform.position);
            // X軸とY軸のダンピングを分けて適用
            float xRotation = Mathf.LerpAngle(transform.rotation.eulerAngles.x, targetRotation.eulerAngles.x, Time.deltaTime * xDamping);
            float yRotation = Mathf.LerpAngle(transform.rotation.eulerAngles.y, targetRotation.eulerAngles.y, Time.deltaTime * yDamping);
            transform.rotation = Quaternion.Euler(xRotation, yRotation, targetRotation.eulerAngles.z);

            // パーリンノイズに基づいてズーム
            time += Time.deltaTime * zoomSpeed;
            float noise = Mathf.PerlinNoise(time, 0f);
            float curveValue = fovCurve.Evaluate(noise); // カーブから値を取得
            cam.fieldOfView = Mathf.Lerp(minFov, maxFov, curveValue); // カーブの値を使用して視野角を設定

            // フォーカスディスタンスを自動設定
            if (autoFocus)
            {
                focusDistance = Vector3.Distance(transform.position, target.position);
                // フォーカスディスタンスをカメラに設定
                // ここでカメラのフォーカスディスタンスを設定するコードを追加
                // 例えば、カメラのDepth of Fieldエフェクトを使用している場合
                cam.focusDistance = focusDistance;
            }
        }
        cameraWorldPos = transform.position;
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