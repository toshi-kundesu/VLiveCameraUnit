using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// カメラのクロスフェード演出
/// </summary>
public class CameraCrossFade : MonoBehaviour
{
    // 切り替え方法を表すenum
    public enum TransitionType
    {
        Cut,
        CrossFade,
        Random
    }

    // 新しいenumで切り替えモードを定義
    public enum TransitionMode
    {
        Manual,
        AutoSequential,
        AutoRandom // ランダムモードを追加
    }

    // 切り替え対象カメラ
    [SerializeField] private Camera[] _cameras;
    // 切り替えたい拍のリスト
    [SerializeField] private int[] _changeBeats;
    [SerializeField] private int _nextChangeBeat = 8;

    // クロスフェード演出を表示するためのRawImage
    [SerializeField] private RawImage _crossFadeImage;

    // フェード時間
    [SerializeField] private float _fadeDuration = 1;

    // 切り替え方法
    [SerializeField] private TransitionType _transitionType = TransitionType.CrossFade;

    // 切り替えモード
    [SerializeField] private TransitionMode _transitionMode = TransitionMode.Manual;

    // 自動切り替えの間隔
    [SerializeField] private float _autoChangeInterval = 5f;

    private RenderTexture _renderTexture;
    [SerializeField]
    private int _currentIndex;
    private Coroutine _fadeCoroutine;
    [SerializeField]
    private float _timeSinceLastChange; // 最後の切り替えからの経過時間
    public int _bpm = 120; // BPMを追加

    // 指定されたパラメータは有効かどうか
    private bool IsValid => _cameras.Length >= 2 && _crossFadeImage != null;

    // フェード中かどうか
    private bool IsChanging => _fadeCoroutine != null;

    // 初期化
    private void Awake()
    {
        if (!IsValid) return;

        // クロスフェード用のRenderTexture作成
        _renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
        // _renderTexture = new RenderTexture(1920, 1080, 0);
        // _renderTexture = new RenderTexture(1080, 1920, 0);
        // BPMから、8拍分の秒数を計算し、それをAutoChangeIntervalとする
        // _changeBeatを事前に指定したリストの中からランダムに選択

        

        _autoChangeInterval = 60f / _bpm * _nextChangeBeat;

        // RawImage初期化
        _crossFadeImage.texture = _renderTexture;
        _crossFadeImage.gameObject.SetActive(false);

        // カメラ初期化
        for (var i = 0; i < _cameras.Length; ++i)
        {
            // 最初のカメラだけ有効にする
            _cameras[i].enabled = i == _currentIndex;
        }
    }

    // キー入力チェック・カメラ変更
    private void Update()
    {
        if (!IsValid) return;

        // フェード中でない場合、またはAutoモードまたはRandomモードの場合に_timeSinceLastChangeを更新
        _timeSinceLastChange += Time.deltaTime;

        if (!IsChanging)
        {
            if (_transitionMode == TransitionMode.Manual)
            {
                if (!Input.anyKeyDown) return;

                if (!int.TryParse(Input.inputString, out var cameraNo))
                    return;

                _timeSinceLastChange = 0; // カメラを手動で切り替えた場合、タイマーをリセット
                ChangeCamera(cameraNo - 1);
            }
            else if (_transitionMode == TransitionMode.AutoSequential)
            {
                // ボタンが押されていたらManualと同じように切り替わるようにする
                // もしボタンが押されていたら、
                if (Input.anyKeyDown)
                {
                    if (!int.TryParse(Input.inputString, out var cameraNo))
                        return;

                    _timeSinceLastChange = 0; // カメラを手動で切り替えた場合、タイマーをリセット
                    ChangeCamera(cameraNo - 1);
                }
                if (_timeSinceLastChange >= _autoChangeInterval)
                {
                    _timeSinceLastChange = 0;
                    ChangeCamera((_currentIndex + 1) % _cameras.Length);
                }
            }
            else if (_transitionMode == TransitionMode.AutoRandom)
            {

                if (Input.anyKeyDown)
                {
                    if (!int.TryParse(Input.inputString, out var cameraNo))
                        return;

                    _timeSinceLastChange = 0; // カメラを手動で切り替えた場合、タイマーをリセット
                    ChangeCamera(cameraNo - 1);
                }
                if (_timeSinceLastChange >= _autoChangeInterval)
                {
                    _timeSinceLastChange = 0;
                    int randomIndex = Random.Range(0, _cameras.Length);
                    // 現在のカメラと同じインデックスが選ばれた場合は再選択
                    while (randomIndex == _currentIndex)
                    {
                        randomIndex = Random.Range(0, _cameras.Length);
                    }
                    ChangeCamera(randomIndex);
                }
            }
        }
    }

    // 指定されたインデックスのカメラに切り替える（フェードまたはカット）
    public void ChangeCamera(int index)
    {
        if (!IsValid || IsChanging || index < 0 || index >= _cameras.Length || index == _currentIndex)
            return;

         // Autoモードで手動でカメラを切り替えた場合、タイマーをリセット
        // if (_transitionMode == TransitionMode.Auto)
        // {
        //     _timeSinceLastChange = 0;
        // }

        switch (_transitionType)
        {
            case TransitionType.CrossFade:
                _fadeCoroutine = StartCoroutine(CrossFadeCoroutine(index)); // ここを修正
                break;
            case TransitionType.Cut:
                CutToCamera(index);
                break;
            case TransitionType.Random:
                // CutかCrossFadeかランダムに指定
                int random = Random.Range(0, 2);
                Debug.Log(random);
                if (random == 1)
                {
                    _fadeCoroutine = StartCoroutine(CrossFadeCoroutine(index));
                }
                else
                {
                    CutToCamera(index);
                }
                break;
        }

        // nextChangeBeatを更新
        _nextChangeBeat = _changeBeats[Random.Range(0, _changeBeats.Length)];
        _autoChangeInterval = 60f / _bpm * _nextChangeBeat;
   
    }

    // カメラを即座に切り替える（カット）
    private void CutToCamera(int index)
    {
        _cameras[_currentIndex].enabled = false;
        _cameras[index].enabled = true;
        _currentIndex = index;
    }

    // クロスフェード演出コルーチン
    private IEnumerator CrossFadeCoroutine(int index)
    {
        // フェード用のRawImage表示
        _crossFadeImage.gameObject.SetActive(true);

        // フェード中のみ、切り替え後カメラの描画先をRenderTextureに設定
        var nextCamera = _cameras[index];
        nextCamera.enabled = true;
        nextCamera.targetTexture = _renderTexture;

        // RawImageのα値を徐々に変更（フェード）
        var startTime = Time.time;
        while (true)
        {
            var time = Time.time - startTime;
            if (time > _fadeDuration)
                break;

            var alpha = time / _fadeDuration;
            _crossFadeImage.color = new Color(1, 1, 1, alpha);

            yield return null;
        }

        // 切り替え後カメラを有効化
        nextCamera.targetTexture = null;
        _cameras[_currentIndex].enabled = false;

        // フェード用のRawImage非表示
        _crossFadeImage.gameObject.SetActive(false);

        _currentIndex = index;
        _fadeCoroutine = null;
    }
    // ContextMenuを使用してエディタから直接このメソッドを呼び出せるようにする
    [ContextMenu("Set Random Seed")]
    void SetRandomSeed()
    {
        // 現在の時刻からハッシュコードを取得してシード値として使用
        int seed = System.DateTime.Now.GetHashCode();
        Random.InitState(seed);
        

        // ここでRandom.Rangeを使用することも可能
        Debug.Log("Random Seed Set: " + seed);
    }
}