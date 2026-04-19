#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace toshi.VLiveKit.Photography.Editor
{
    public partial class VLiveCameraEditor
    {
        private const float BoneMapHeight = 280f;
        private const float BoneNodeSize = 24f;

        private struct BoneMapNode
        {
            public HumanBodyBones bone;
            public string shortLabel;
            public Vector2 normalizedPosition;

            public BoneMapNode(HumanBodyBones bone, string shortLabel, Vector2 normalizedPosition)
            {
                this.bone = bone;
                this.shortLabel = shortLabel;
                this.normalizedPosition = normalizedPosition;
            }
        }

        private static readonly BoneMapNode[] BoneNodes =
        {
            new BoneMapNode(HumanBodyBones.Head,          "HD",  new Vector2(0.50f, 0.10f)),
            new BoneMapNode(HumanBodyBones.Neck,          "NK",  new Vector2(0.50f, 0.18f)),
            new BoneMapNode(HumanBodyBones.UpperChest,    "UC",  new Vector2(0.50f, 0.28f)),
            new BoneMapNode(HumanBodyBones.Chest,         "CH",  new Vector2(0.50f, 0.35f)),
            new BoneMapNode(HumanBodyBones.Spine,         "SP",  new Vector2(0.50f, 0.46f)),
            new BoneMapNode(HumanBodyBones.Hips,          "HP",  new Vector2(0.50f, 0.60f)),

            new BoneMapNode(HumanBodyBones.LeftShoulder,  "LS",  new Vector2(0.38f, 0.27f)),
            new BoneMapNode(HumanBodyBones.RightShoulder, "RS",  new Vector2(0.62f, 0.27f)),

            new BoneMapNode(HumanBodyBones.LeftUpperArm,  "LU",  new Vector2(0.28f, 0.34f)),
            new BoneMapNode(HumanBodyBones.RightUpperArm, "RU",  new Vector2(0.72f, 0.34f)),

            new BoneMapNode(HumanBodyBones.LeftLowerArm,  "LF",  new Vector2(0.22f, 0.47f)),
            new BoneMapNode(HumanBodyBones.RightLowerArm, "RF",  new Vector2(0.78f, 0.47f)),

            new BoneMapNode(HumanBodyBones.LeftHand,      "LH",  new Vector2(0.18f, 0.60f)),
            new BoneMapNode(HumanBodyBones.RightHand,     "RH",  new Vector2(0.82f, 0.60f)),

            new BoneMapNode(HumanBodyBones.LeftUpperLeg,  "LT",  new Vector2(0.43f, 0.74f)),
            new BoneMapNode(HumanBodyBones.RightUpperLeg, "RT",  new Vector2(0.57f, 0.74f)),

            new BoneMapNode(HumanBodyBones.LeftLowerLeg,  "LC",  new Vector2(0.41f, 0.88f)),
            new BoneMapNode(HumanBodyBones.RightLowerLeg, "RC",  new Vector2(0.59f, 0.88f)),

            new BoneMapNode(HumanBodyBones.LeftFoot,      "LFt", new Vector2(0.38f, 0.98f)),
            new BoneMapNode(HumanBodyBones.RightFoot,     "RFt", new Vector2(0.62f, 0.98f)),
        };

        private static readonly HumanBodyBones[] QuickAimBones =
        {
            HumanBodyBones.Head,
            HumanBodyBones.Neck,
            HumanBodyBones.UpperChest,
            HumanBodyBones.Chest,
            HumanBodyBones.Spine,
            HumanBodyBones.Hips,
            HumanBodyBones.LeftHand,
            HumanBodyBones.RightHand,
            HumanBodyBones.LeftFoot,
            HumanBodyBones.RightFoot,
        };

        private void DrawBonePicker(SerializedProperty boneProperty, string label)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            DrawQuickBoneChips(boneProperty);

            Rect rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 40f, BoneMapHeight, GUILayout.ExpandWidth(true));
            DrawBoneMap(rect, boneProperty);

            EditorGUILayout.PropertyField(boneProperty, new GUIContent("Target Bone"));
        }

        private void DrawQuickBoneChips(SerializedProperty boneProperty)
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Quick Picks", EditorStyles.miniBoldLabel);

            int countPerRow = 5;
            for (int i = 0; i < QuickAimBones.Length; i += countPerRow)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int j = i; j < i + countPerRow && j < QuickAimBones.Length; j++)
                    {
                        HumanBodyBones bone = QuickAimBones[j];
                        bool selected = (HumanBodyBones)boneProperty.enumValueIndex == bone;
                        Color prev = GUI.backgroundColor;
                        GUI.backgroundColor = selected ? BoneNodeSelectedColor : BoneNodeColor;

                        if (GUILayout.Button(GetQuickBoneLabel(bone), quickBoneButtonStyle))
                        {
                            boneProperty.enumValueIndex = (int)bone;
                        }

                        GUI.backgroundColor = prev;
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBoneMap(Rect rect, SerializedProperty boneProperty)
        {
            EditorGUI.DrawRect(rect, BoneMapBg);

            DrawBoneLink(rect, HumanBodyBones.Head, HumanBodyBones.Neck);
            DrawBoneLink(rect, HumanBodyBones.Neck, HumanBodyBones.UpperChest);
            DrawBoneLink(rect, HumanBodyBones.UpperChest, HumanBodyBones.Chest);
            DrawBoneLink(rect, HumanBodyBones.Chest, HumanBodyBones.Spine);
            DrawBoneLink(rect, HumanBodyBones.Spine, HumanBodyBones.Hips);

            DrawBoneLink(rect, HumanBodyBones.UpperChest, HumanBodyBones.LeftShoulder);
            DrawBoneLink(rect, HumanBodyBones.UpperChest, HumanBodyBones.RightShoulder);

            DrawBoneLink(rect, HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm);
            DrawBoneLink(rect, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm);
            DrawBoneLink(rect, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand);

            DrawBoneLink(rect, HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm);
            DrawBoneLink(rect, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm);
            DrawBoneLink(rect, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);

            DrawBoneLink(rect, HumanBodyBones.Hips, HumanBodyBones.LeftUpperLeg);
            DrawBoneLink(rect, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg);
            DrawBoneLink(rect, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);

            DrawBoneLink(rect, HumanBodyBones.Hips, HumanBodyBones.RightUpperLeg);
            DrawBoneLink(rect, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg);
            DrawBoneLink(rect, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);

            HumanBodyBones selectedBone = (HumanBodyBones)boneProperty.enumValueIndex;

            foreach (BoneMapNode node in BoneNodes)
            {
                Rect nodeRect = GetBoneNodeRect(rect, node.normalizedPosition);
                bool selected = selectedBone == node.bone;

                EditorGUI.DrawRect(nodeRect, selected ? BoneNodeSelectedColor : BoneNodeColor);

                if (GUI.Button(nodeRect, GUIContent.none, GUIStyle.none))
                {
                    boneProperty.enumValueIndex = (int)node.bone;
                    GUI.changed = true;
                }

                GUI.Label(nodeRect, node.shortLabel, selected ? selectedBoneLabelStyle : boneLabelStyle);
            }

            GUI.Label(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 20f), "Humanoid Bone Map", miniInfoStyle);
        }

        private void DrawBoneLink(Rect rect, HumanBodyBones a, HumanBodyBones b)
        {
            Vector2 pa = GetBoneNodeCenter(rect, GetBoneNode(a).normalizedPosition);
            Vector2 pb = GetBoneNodeCenter(rect, GetBoneNode(b).normalizedPosition);

            Handles.BeginGUI();
            Color prev = Handles.color;
            Handles.color = BoneMapLine;
            Handles.DrawAAPolyLine(3f, pa, pb);
            Handles.color = prev;
            Handles.EndGUI();
        }

        private static BoneMapNode GetBoneNode(HumanBodyBones bone)
        {
            for (int i = 0; i < BoneNodes.Length; i++)
            {
                if (BoneNodes[i].bone == bone)
                    return BoneNodes[i];
            }

            return BoneNodes[0];
        }

        private static Rect GetBoneNodeRect(Rect container, Vector2 normalized)
        {
            Vector2 center = GetBoneNodeCenter(container, normalized);
            return new Rect(center.x - BoneNodeSize * 0.5f, center.y - BoneNodeSize * 0.5f, BoneNodeSize, BoneNodeSize);
        }

        private static Vector2 GetBoneNodeCenter(Rect container, Vector2 normalized)
        {
            float x = Mathf.Lerp(container.x + 28f, container.xMax - 28f, normalized.x);
            float y = Mathf.Lerp(container.y + 24f, container.yMax - 24f, normalized.y);
            return new Vector2(x, y);
        }

        private static string GetQuickBoneLabel(HumanBodyBones bone)
        {
            switch (bone)
            {
                case HumanBodyBones.Head: return "Head";
                case HumanBodyBones.Neck: return "Neck";
                case HumanBodyBones.UpperChest: return "UpperChest";
                case HumanBodyBones.Chest: return "Chest";
                case HumanBodyBones.Spine: return "Spine";
                case HumanBodyBones.Hips: return "Hips";
                case HumanBodyBones.LeftHand: return "L Hand";
                case HumanBodyBones.RightHand: return "R Hand";
                case HumanBodyBones.LeftFoot: return "L Foot";
                case HumanBodyBones.RightFoot: return "R Foot";
                default: return bone.ToString();
            }
        }
    }
}
#endif