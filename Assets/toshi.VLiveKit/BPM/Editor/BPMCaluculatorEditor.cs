using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BPMCalculatorEditor : EditorWindow
{
    private List<float> clickTimes = new List<float>();
    private float _bpm = 120; // デフォルトのBPM

    [MenuItem("Tools/BPM Calculator")]
    public static void ShowWindow()
    {
        GetWindow<BPMCalculatorEditor>("BPM Calculator");
    }

    void OnGUI()
    {
        GUILayout.Label("BPM Calculator", EditorStyles.boldLabel);

        if (GUILayout.Button("Click to the Beat!"))
        {
            RecordClick();
        }

        if (clickTimes.Count > 1)
        {
            CalculateBPM();
            GUILayout.Label($"Calculated BPM: {_bpm}");
        }

        if (GUILayout.Button("Apply BPM"))
        {
            ApplyBPM();
        }
    }

    void RecordClick()
    {
        clickTimes.Add((float)EditorApplication.timeSinceStartup);
        if (clickTimes.Count > 4) // 過去4回のクリックのみを使用
        {
            clickTimes.RemoveAt(0);
        }
    }

    void CalculateBPM()
    {
        float averageInterval = 0;
        for (int i = 1; i < clickTimes.Count; i++)
        {
            averageInterval += clickTimes[i] - clickTimes[i - 1];
        }
        averageInterval /= (clickTimes.Count - 1);
        _bpm = 60 / averageInterval;
    }

    void ApplyBPM()
    {
        var cameraCrossFade = FindObjectOfType<CameraCrossFade>();
        if (cameraCrossFade != null)
        {
            Undo.RecordObject(cameraCrossFade, "Change BPM");
            cameraCrossFade._bpm = Mathf.RoundToInt(_bpm);
            EditorUtility.SetDirty(cameraCrossFade);
        }
    }
}