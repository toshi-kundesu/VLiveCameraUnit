using UnityEngine;

[ExecuteAlways]
public class SplitLines : MonoBehaviour
{
    [SerializeField] private SplitMode m_splitMode = SplitMode.Thirds;
    [SerializeField] private bool m_isDraw = true;
    [SerializeField] private Color m_lineColor = new Color(1.0f, 0.0f, 0.0f, 0.5f);
    // 線の透明度
    // [SerializeField] private float m_lineAlpha = 1.0f;
    [SerializeField] [Range(0.0f, 10.0f)] private float m_lineWidth = 2.0f;

    // [SerializeField] private float m_lineHeight = 1.0f;

    // 分割線のスタイルを選べるようにする
    private enum SplitMode
    {
        Symmetrical, // 対称
        Bisection,   // 二分割
        Thirds,      // 三分割
        Diagonal,    // 対角線
        // 分割線と対角線を組み合わせる
        ThirstAndDiagonal,
        CinemaScope, // シネマスコープ
    }

    private void OnGUI()
    {
        if (!m_isDraw) return;

        if (m_splitMode == SplitMode.Symmetrical)
        {
            // 対称
            float halfWidth = Screen.width / 2.0f;
            float halfHeight = Screen.height / 2.0f;
            float lineWidth = m_lineWidth;

            GUIStyle lineStyle = new GUIStyle();
            Texture2D lineTexture = new Texture2D(1, 1);
            lineTexture.SetPixel(0, 0, m_lineColor);
            lineTexture.Apply();

            lineStyle.normal.background = lineTexture;

            Rect lineRect1 = new Rect(halfWidth - (lineWidth / 2), 0, lineWidth, Screen.height);
            GUI.Box(lineRect1, GUIContent.none, lineStyle);

            // Rect lineRect2 = new Rect(0, halfHeight - (lineWidth / 2), Screen.width, lineWidth);
            // GUI.Box(lineRect2, GUIContent.none, lineStyle);

            // メモリリークするものがあれば明示的に破棄
            // プレイモードのときに動作させる
            if (Application.isPlaying)
            {
                Destroy(lineTexture);
                // Debug.Log("play mode");
            }
            else
            {
                // Debug.Log("edit mode");
            }
        }

        if (m_splitMode == SplitMode.Bisection)
        {
            // 二分割
            float halfWidth = Screen.width / 2.0f;
            float halfHeight = Screen.height / 2.0f;
            float lineWidth = m_lineWidth;

            GUIStyle lineStyle = new GUIStyle();
            Texture2D lineTexture = new Texture2D(1, 1);
            lineTexture.SetPixel(0, 0, m_lineColor);
            lineTexture.Apply();

            lineStyle.normal.background = lineTexture;

            Rect lineRect1 = new Rect(halfWidth - (lineWidth / 2), 0, lineWidth, Screen.height);
            GUI.Box(lineRect1, GUIContent.none, lineStyle);

            Rect lineRect2 = new Rect(0, halfHeight - (lineWidth / 2), Screen.width, lineWidth);
            GUI.Box(lineRect2, GUIContent.none, lineStyle);

            // メモリリークするものがあれば明示的に破棄
            // プレイモードのときに動作させる
            if (Application.isPlaying)
            {
                Destroy(lineTexture);
                // Debug.Log("play mode");
            }
            else
            {
                // Debug.Log("edit mode");
            }
        }

        if (m_splitMode == SplitMode.Thirds)
        {
            // 三分割
            float thirdWidth = Screen.width / 3.0f;
            float thirdHeight = Screen.height / 3.0f;
            float lineWidth = m_lineWidth;

            GUIStyle lineStyle = new GUIStyle();
            Texture2D lineTexture = new Texture2D(1, 1);
            lineTexture.SetPixel(0, 0, m_lineColor);
            lineTexture.Apply();

            lineStyle.normal.background = lineTexture;

            Rect lineRect1 = new Rect(thirdWidth - (lineWidth / 2), 0, lineWidth, Screen.height);
            GUI.Box(lineRect1, GUIContent.none, lineStyle);

            Rect lineRect2 = new Rect(2 * thirdWidth - (lineWidth / 2), 0, lineWidth, Screen.height);
            GUI.Box(lineRect2, GUIContent.none, lineStyle);

            Rect lineRect3 = new Rect(0, thirdHeight - (lineWidth / 2), Screen.width, lineWidth);
            GUI.Box(lineRect3, GUIContent.none, lineStyle);

            Rect lineRect4 = new Rect(0, 2 * thirdHeight - (lineWidth / 2), Screen.width, lineWidth);
            GUI.Box(lineRect4, GUIContent.none, lineStyle);

            // メモリリークするものがあれば明示的に破棄
            // プレイモードのときに動作させる
            if (Application.isPlaying)
            {
                Destroy(lineTexture);
                // Debug.Log("play mode");
            }
            else
            {
                // Debug.Log("edit mode");
            }
        }

        if (m_splitMode == SplitMode.Diagonal)
        {
            // 対角線
            float lineWidth = m_lineWidth;
            GUIStyle lineStyle = new GUIStyle();
            Texture2D lineTexture = new Texture2D(1, 1);
            lineTexture.SetPixel(0, 0, m_lineColor);
            lineTexture.Apply();

            lineStyle.normal.background = lineTexture;

            // 左上から右下へ
            Vector2 start1 = new Vector2(0, 0);
            Vector2 end1 = new Vector2(Screen.width, Screen.height);
            DrawLine(start1, end1, lineTexture, lineWidth);

            // 右上から左下へ
            Vector2 start2 = new Vector2(Screen.width, 0);
            Vector2 end2 = new Vector2(0, Screen.height);
            DrawLine(start2, end2, lineTexture, lineWidth);

            // メモリリークするものがあれば明示的に破棄
            // プレイモードのときに動作させる
            if (Application.isPlaying)
            {
                Destroy(lineTexture);
                // Debug.Log("play mode");
            }
            else
            {
                // Debug.Log("edit mode");
            }
        }

        if (m_splitMode == SplitMode.ThirstAndDiagonal)
        {
            // 分割線と対角線を組み合わせる
            // 対角線
            float lineWidth = m_lineWidth;
            GUIStyle lineStyle = new GUIStyle();
            Texture2D lineTexture = new Texture2D(1, 1);
            lineTexture.SetPixel(0, 0, m_lineColor);
            lineTexture.Apply();

            lineStyle.normal.background = lineTexture;

            // 左上から右下へ
            Vector2 start1 = new Vector2(0, 0);
            Vector2 end1 = new Vector2(Screen.width, Screen.height);
            DrawLine(start1, end1, lineTexture, lineWidth);

            // 右上から左下へ
            Vector2 start2 = new Vector2(Screen.width, 0);
            Vector2 end2 = new Vector2(0, Screen.height);
            DrawLine(start2, end2, lineTexture, lineWidth);

            float thirdWidth = Screen.width / 3.0f;
            float thirdHeight = Screen.height / 3.0f;
            // float lineWidth = m_lineWidth;

            // GUIStyle lineStyle = new GUIStyle();
            // Texture2D lineTexture = new Texture2D(1, 1);
            // lineTexture.SetPixel(0, 0, m_lineColor);
            // lineTexture.Apply();

            // lineStyle.normal.background = lineTexture;

            Rect lineRect1 = new Rect(thirdWidth - (lineWidth / 2), 0, lineWidth, Screen.height);
            GUI.Box(lineRect1, GUIContent.none, lineStyle);

            Rect lineRect2 = new Rect(2 * thirdWidth - (lineWidth / 2), 0, lineWidth, Screen.height);
            GUI.Box(lineRect2, GUIContent.none, lineStyle);

            Rect lineRect3 = new Rect(0, thirdHeight - (lineWidth / 2), Screen.width, lineWidth);
            GUI.Box(lineRect3, GUIContent.none, lineStyle);

            Rect lineRect4 = new Rect(0, 2 * thirdHeight - (lineWidth / 2), Screen.width, lineWidth);
            GUI.Box(lineRect4, GUIContent.none, lineStyle);


            // メモリリークするものがあれば明示的に破棄
            // プレイモードのときに動作させる
            if (Application.isPlaying)
            {
                Destroy(lineTexture);
                // Debug.Log("play mode");
            }
            else
            {
                // Debug.Log("edit mode");
            }
            // if (lineStyle != null)
            // {
            //     Destroy(lineStyle);
            // }

            // Destroy(lineStyle);

        }

        if (m_splitMode == SplitMode.CinemaScope)
        {
            // シネマスコープ
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float cinemaScopeHeight = screenWidth / 2.35f; // 2.35:1のアスペクト比
            float letterboxHeight = (screenHeight - cinemaScopeHeight) / 2; // 上下のレターボックスの高さ
            float lineWidth = m_lineWidth;

            GUIStyle lineStyle = new GUIStyle();
            Texture2D lineTexture = new Texture2D(1, 1);
            lineTexture.SetPixel(0, 0, m_lineColor);
            lineTexture.Apply();

            lineStyle.normal.background = lineTexture;

            // シネマスコープ内の分割線を描画
            float thirdWidth = screenWidth / 3.0f;
            float thirdHeight = cinemaScopeHeight / 3.0f;

            Rect lineRect1 = new Rect(thirdWidth - (lineWidth / 2), letterboxHeight, lineWidth, cinemaScopeHeight);
            GUI.Box(lineRect1, GUIContent.none, lineStyle);

            Rect lineRect2 = new Rect(2 * thirdWidth - (lineWidth / 2), letterboxHeight, lineWidth, cinemaScopeHeight);
            GUI.Box(lineRect2, GUIContent.none, lineStyle);

            Rect lineRect3 = new Rect(0, letterboxHeight + thirdHeight - (lineWidth / 2), screenWidth, lineWidth);
            GUI.Box(lineRect3, GUIContent.none, lineStyle);

            Rect lineRect4 = new Rect(0, letterboxHeight + 2 * thirdHeight - (lineWidth / 2), screenWidth, lineWidth);
            GUI.Box(lineRect4, GUIContent.none, lineStyle);

            // メモリリークするものがあれば明示的に破棄
            if (Application.isPlaying)
            {
                Destroy(lineTexture);
            }
        }

        void DrawLine(Vector2 start, Vector2 end, Texture2D texture, float width)
        {
            Matrix4x4 matrix = GUI.matrix;
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * 180f / Mathf.PI;
            float length = Vector2.Distance(start, end);
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y - (width / 2), length, width), texture);
            GUI.matrix = matrix;
        }

        
    }
}