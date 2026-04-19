using UnityEngine;

public class SimpleCameraSwitcher : MonoBehaviour
{
    public Camera[] cameras = new Camera[8];
    public KeyCode[] keys = new KeyCode[8];
    public bool isAutoSwitching = false; // 自動スイッチングのフラグ
    public float minSwitchTime = 1.0f; // 最小スイッチ時間
    public float maxSwitchTime = 5.0f; // 最大スイッチ時間

    private float nextSwitchTime;

    private void Start()
    {
        // 全てのカメラコンポーネントを無効に設定
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null)
            {
                cameras[i].enabled = false;
            }
        }
        // 最初のカメラコンポーネントを有効にする
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null)
            {
                cameras[i].enabled = true;
                break;
            }
        }

        if (isAutoSwitching)
        {
            ScheduleNextSwitch();
        }
    }

    private void Update()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            if (Input.GetKeyDown(keys[i]) && cameras[i] != null)
            {
                SwitchCamera(i);
            }
        }

        if (isAutoSwitching && Time.time >= nextSwitchTime)
        {
            SwitchCamera(GetRandomNonNullCameraIndex());
            ScheduleNextSwitch();
        }
    }

    private void SwitchCamera(int index)
    {
        // 全てのカメラコンポーネントを無効にする
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null)
            {
                cameras[i].enabled = false;
            }
        }
        // 指定したカメラコンポーネントを有効にする
        if (cameras[index] != null)
        {
            cameras[index].enabled = true;
        }
    }

    private void ScheduleNextSwitch()
    {
        nextSwitchTime = Time.time + Random.Range(minSwitchTime, maxSwitchTime);
    }

    private int GetRandomNonNullCameraIndex()
    {
        int index;
        do
        {
            index = Random.Range(0, cameras.Length);
        } while (cameras[index] == null);
        return index;
    }
}