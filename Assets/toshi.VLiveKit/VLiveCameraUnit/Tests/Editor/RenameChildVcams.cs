#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class RenameChildVcams
{
    private const string MenuPath = "Tools/Cinemachine/選択オブジェクト配下のVCamを連番リネーム";

    [MenuItem(MenuPath)]
    private static void RenameSelectedChildrenVcams()
    {
        var root = Selection.activeGameObject;
        if (root == null)
        {
            EditorUtility.DisplayDialog("VCam Rename", "Hierarchyで親オブジェクトを1つ選択してください。", "OK");
            return;
        }

        var targets = CollectVirtualCameraObjects(root);

        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("VCam Rename", "選択オブジェクト配下に CinemachineVirtualCamera が見つかりませんでした。", "OK");
            return;
        }

        Undo.RecordObjects(targets.ToArray(), "Rename Cinemachine Virtual Cameras");

        int digits = Mathf.Max(2, targets.Count.ToString().Length);

        for (int i = 0; i < targets.Count; i++)
        {
            string newName = $"vcam{(i + 1).ToString($"D{digits}")}";
            targets[i].name = newName;
            EditorUtility.SetDirty(targets[i]);
        }

        Debug.Log($"[RenameChildVcams] {targets.Count} 個のVCamを連番リネームしました。", root);
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateRenameSelectedChildrenVcams()
    {
        return Selection.activeGameObject != null;
    }

    private static List<GameObject> CollectVirtualCameraObjects(GameObject root)
    {
        var results = new List<GameObject>();

        // Hierarchy順で拾いたいので Transform の順で走査
        var transforms = root.GetComponentsInChildren<Transform>(true);

        foreach (var t in transforms)
        {
            if (HasCinemachineVirtualCamera(t.gameObject))
            {
                results.Add(t.gameObject);
            }
        }

        return results;
    }

    private static bool HasCinemachineVirtualCamera(GameObject go)
    {
        // Cinemachine 2 系
        if (go.GetComponent("CinemachineVirtualCamera") != null)
            return true;

        // 念のため FreeLook も対象にしたいなら有効化
        // if (go.GetComponent("CinemachineFreeLook") != null)
        //     return true;

        return false;
    }
}
#endif