#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace toshi.VLiveKit.Photography.Editor
{
    public partial class VLiveCameraEditor
    {
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle boxStyle;
        private GUIStyle foldoutStyle;
        private GUIStyle miniInfoStyle;
        private GUIStyle boneLabelStyle;
        private GUIStyle selectedBoneLabelStyle;
        private GUIStyle quickBoneButtonStyle;

        private static readonly Color HeaderBg = new Color(0.08f, 0.09f, 0.12f);
        private static readonly Color HeaderAccent = new Color(0.20f, 0.80f, 1.00f, 0.95f);
        private static readonly Color BoneMapBg = new Color(0.10f, 0.11f, 0.14f, 1f);
        private static readonly Color BoneMapLine = new Color(0.45f, 0.55f, 0.70f, 0.85f);
        private static readonly Color BoneNodeColor = new Color(0.17f, 0.21f, 0.29f, 1f);
        private static readonly Color BoneNodeSelectedColor = new Color(0.18f, 0.64f, 1.0f, 1f);
        private static readonly Color StatusOk = new Color(0.25f, 0.85f, 0.45f, 1f);
        private static readonly Color StatusWarn = new Color(1.00f, 0.78f, 0.20f, 1f);

        private void EnsureStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 15,
                    normal = { textColor = Color.white }
                };
            }

            if (subHeaderStyle == null)
            {
                subHeaderStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.68f, 0.86f, 1f, 1f) }
                };
            }

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle("HelpBox")
                {
                    padding = new RectOffset(10, 10, 8, 8),
                    margin = new RectOffset(4, 4, 4, 6)
                };
            }

            if (foldoutStyle == null)
            {
                foldoutStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold
                };
            }

            if (miniInfoStyle == null)
            {
                miniInfoStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.82f, 0.90f, 1f, 1f) }
                };
            }

            if (boneLabelStyle == null)
            {
                boneLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
            }

            if (selectedBoneLabelStyle == null)
            {
                selectedBoneLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
            }

            if (quickBoneButtonStyle == null)
            {
                quickBoneButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fixedHeight = 24f,
                    fontSize = 10
                };
            }
        }
    }
}
#endif