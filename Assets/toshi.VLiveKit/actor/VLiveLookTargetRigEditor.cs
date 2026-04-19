#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VLiveLookTargetRig))]
public class VLiveLookTargetRigEditor : Editor
{
    private SerializedProperty _vLivePerformerProp;
    private SerializedProperty _performerAnimatorProp;
    private SerializedProperty _fallbackHumanoidAvatarProp;
    private SerializedProperty _lookTargetRootProp;
    private SerializedProperty _performerNameProp;
    private SerializedProperty _syncActiveStateEveryFrameProp;
    private SerializedProperty _lookTargetChannelsProp;

    private bool _showChannelList = true;

    private static readonly HumanBodyBones[] QuickAccessBones =
    {
        HumanBodyBones.Head,
        HumanBodyBones.Neck,
        HumanBodyBones.Chest,
        HumanBodyBones.Spine,
        HumanBodyBones.Hips,
        HumanBodyBones.LeftHand,
        HumanBodyBones.RightHand
    };

    private void OnEnable()
    {
        _vLivePerformerProp = serializedObject.FindProperty("vLivePerformer");
        _performerAnimatorProp = serializedObject.FindProperty("performerAnimator");
        _fallbackHumanoidAvatarProp = serializedObject.FindProperty("fallbackHumanoidAvatar");
        _lookTargetRootProp = serializedObject.FindProperty("lookTargetRoot");
        _performerNameProp = serializedObject.FindProperty("performerName");
        _syncActiveStateEveryFrameProp = serializedObject.FindProperty("syncActiveStateEveryFrame");
        _lookTargetChannelsProp = serializedObject.FindProperty("lookTargetChannels");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var rig = (VLiveLookTargetRig)target;

        DrawHeader();
        EditorGUILayout.Space(6);

        DrawPerformerSection();
        EditorGUILayout.Space(8);

        DrawOperationSection(rig);
        EditorGUILayout.Space(8);

        DrawQuickAccessSection(rig);
        EditorGUILayout.Space(8);

        DrawDebugSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeader()
    {
        var rect = GUILayoutUtility.GetRect(0, 68, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.07f, 0.07f, 0.10f));

        var titleRect = new Rect(rect.x + 12, rect.y + 8, rect.width - 24, 24);
        var subRect = new Rect(rect.x + 12, rect.y + 34, rect.width - 24, 18);
        var brandRect = new Rect(rect.x + 12, rect.y + 50, rect.width - 24, 16);

        EditorGUI.LabelField(titleRect, "VLIVE LOOK TARGET RIG", new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 15,
            normal = { textColor = Color.white }
        });

        EditorGUI.LabelField(subRect, "演者ボーンからカメラ用ターゲットを生成", new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(0.70f, 0.90f, 1f) }
        });

        EditorGUI.LabelField(brandRect, "VLive Performer / Camera Utility", new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(0.45f, 0.80f, 1f) }
        });
    }

    private void DrawPerformerSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("VLive Performer Link", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_vLivePerformerProp, new GUIContent("VLive Performer"));

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Fallback / Override", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_performerAnimatorProp, new GUIContent("Performer Animator"));
        EditorGUILayout.PropertyField(_fallbackHumanoidAvatarProp, new GUIContent("Fallback Humanoid Avatar"));
        EditorGUILayout.PropertyField(_lookTargetRootProp, new GUIContent("Look Target Root"));
        EditorGUILayout.PropertyField(_performerNameProp, new GUIContent("Performer Name"));
        EditorGUILayout.PropertyField(_syncActiveStateEveryFrameProp, new GUIContent("Sync Active State Every Frame"));
        EditorGUILayout.EndVertical();
    }

    private void DrawOperationSection(VLiveLookTargetRig rig)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("VLive Operation", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Resolve Performer", GUILayout.Height(28)))
            {
                Undo.RecordObject(rig, "Resolve Performer");
                InvokeMethod(rig, "AutoResolvePerformer");
                EditorUtility.SetDirty(rig);
            }

            if (GUILayout.Button("Build Targets", GUILayout.Height(28)))
            {
                Undo.RecordObject(rig, "Build Targets");
                rig.BuildTargets();
                EditorUtility.SetDirty(rig);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh State", GUILayout.Height(24)))
            {
                rig.RefreshLiveState();
                EditorUtility.SetDirty(rig);
            }

            GUI.enabled = rig.LookTargetRoot != null;
            if (GUILayout.Button("Ping Root", GUILayout.Height(24)))
            {
                EditorGUIUtility.PingObject(rig.LookTargetRoot);
                Selection.activeObject = rig.LookTargetRoot.gameObject;
            }
            GUI.enabled = true;
        }

        EditorGUILayout.Space(4);

        DrawStatusLamp("Performer Live", rig.IsPerformerLive);
        DrawStatusLamp("Targets Ready", rig.LookTargetChannels != null && rig.LookTargetChannels.Count > 0);

        EditorGUILayout.EndVertical();
    }

    private void DrawQuickAccessSection(VLiveLookTargetRig rig)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Quick Bone Access", EditorStyles.boldLabel);

        foreach (var bone in QuickAccessBones)
        {
            var target = rig.GetBoneTG(bone);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(bone.ToString(), GUILayout.Width(100));

                GUI.enabled = target != null;
                EditorGUILayout.ObjectField(target, typeof(GameObject), true);

                if (GUILayout.Button("Select", GUILayout.Width(60)) && target != null)
                {
                    Selection.activeObject = target;
                    EditorGUIUtility.PingObject(target);
                }

                GUI.enabled = true;
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawDebugSection()
    {
        EditorGUILayout.BeginVertical("box");
        _showChannelList = EditorGUILayout.Foldout(_showChannelList, "Target Channels", true);

        if (_showChannelList)
        {
            EditorGUI.indentLevel++;

            for (int i = 0; i < _lookTargetChannelsProp.arraySize; i++)
            {
                var element = _lookTargetChannelsProp.GetArrayElementAtIndex(i);
                var targetBoneProp = element.FindPropertyRelative("targetBone");
                var performerBoneProp = element.FindPropertyRelative("performerBone");
                var lookTargetObjectProp = element.FindPropertyRelative("lookTargetObject");

                EditorGUILayout.BeginVertical("helpbox");
                EditorGUILayout.PropertyField(targetBoneProp, new GUIContent("Target Bone"));
                EditorGUILayout.PropertyField(performerBoneProp, new GUIContent("Performer Bone"));
                EditorGUILayout.PropertyField(lookTargetObjectProp, new GUIContent("Look Target"));
                EditorGUILayout.EndVertical();
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawStatusLamp(string label, bool active)
    {
        var rect = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(true));
        var lampRect = new Rect(rect.x + 4, rect.y + 2, 12, 12);
        var labelRect = new Rect(rect.x + 22, rect.y, rect.width - 22, 18);

        EditorGUI.DrawRect(lampRect, active ? new Color(0.20f, 1f, 0.45f) : new Color(0.35f, 0.35f, 0.35f));
        EditorGUI.LabelField(labelRect, label);
    }

    private void InvokeMethod(object targetObject, string methodName)
    {
        var method = targetObject.GetType().GetMethod(
            methodName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public);

        if (method != null)
        {
            method.Invoke(targetObject, null);
        }
    }
}
#endif