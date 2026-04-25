#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace toshi.VLiveKit.Utilities
{
    public sealed class MissingScriptCleanerWindow : EditorWindow
    {
        private const string MenuPath = "toshi/VLiveKit/Missing Script Cleaner";

        [System.Serializable]
        private sealed class ScanResult
        {
            public GameObject gameObject;
            public string hierarchyPath;
            public int missingCount;
        }

        private readonly List<GameObject> _roots = new List<GameObject>();
        private readonly List<ScanResult> _results = new List<ScanResult>();

        private Vector2 _rootScroll;
        private Vector2 _resultScroll;

        [SerializeField] private bool _includeRootObject = false;
        [SerializeField] private bool _includeInactive = true;
        [SerializeField] private bool _autoClearResultsBeforeScan = true;

        private int _totalMissingCount = 0;
        private int _totalTargetObjectCount = 0;

        [MenuItem(MenuPath)]
        private static void Open()
        {
            var window = GetWindow<MissingScriptCleanerWindow>();
            window.titleContent = new GUIContent("Missing Script Cleaner");
            window.minSize = new Vector2(700f, 450f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Missing Script Cleaner", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Root を指定して、その配下の GameObject に含まれる Missing Script をスキャン・一覧表示・削除します。",
                MessageType.Info);

            DrawRootSection();
            EditorGUILayout.Space(8);
            DrawOptionSection();
            EditorGUILayout.Space(8);
            DrawActionSection();
            EditorGUILayout.Space(8);
            DrawResultSection();
        }

        private void DrawRootSection()
        {
            EditorGUILayout.LabelField("Roots", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("選択中を追加", GUILayout.Height(24)))
                    {
                        AddSelectionToRoots();
                    }

                    if (GUILayout.Button("選択中で置き換え", GUILayout.Height(24)))
                    {
                        ReplaceRootsWithSelection();
                    }

                    if (GUILayout.Button("Clear Roots", GUILayout.Height(24), GUILayout.Width(100)))
                    {
                        _roots.Clear();
                    }
                }

                EditorGUILayout.Space(4);

                _rootScroll = EditorGUILayout.BeginScrollView(_rootScroll, GUILayout.MinHeight(110));
                int removeIndex = -1;

                for (int i = 0; i < _roots.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _roots[i] = (GameObject)EditorGUILayout.ObjectField(
                            $"Root {i + 1}",
                            _roots[i],
                            typeof(GameObject),
                            true);

                        GUI.enabled = _roots[i] != null;
                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            Selection.activeObject = _roots[i];
                            EditorGUIUtility.PingObject(_roots[i]);
                        }
                        GUI.enabled = true;

                        if (GUILayout.Button("X", GUILayout.Width(28)))
                        {
                            removeIndex = i;
                        }
                    }
                }

                if (_roots.Count == 0)
                {
                    EditorGUILayout.HelpBox("Root が未指定です。Hierarchy から選んで追加してください。", MessageType.Warning);
                }

                EditorGUILayout.EndScrollView();

                if (removeIndex >= 0)
                {
                    _roots.RemoveAt(removeIndex);
                }
            }
        }

        private void DrawOptionSection()
        {
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                _includeRootObject = EditorGUILayout.ToggleLeft("Root 自身も対象に含める", _includeRootObject);
                _includeInactive = EditorGUILayout.ToggleLeft("非アクティブオブジェクトも対象に含める", _includeInactive);
                _autoClearResultsBeforeScan = EditorGUILayout.ToggleLeft("スキャン前に前回結果をクリア", _autoClearResultsBeforeScan);
            }
        }

        private void DrawActionSection()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = HasValidRoots();
                if (GUILayout.Button("Scan", GUILayout.Height(30)))
                {
                    Scan();
                }

                GUI.enabled = _results.Count > 0;
                if (GUILayout.Button("一覧を選択", GUILayout.Height(30)))
                {
                    SelectResults();
                }

                GUI.enabled = _results.Count > 0 && _totalMissingCount > 0;
                if (GUILayout.Button("Remove Missing Scripts", GUILayout.Height(30)))
                {
                    RemoveMissingScripts();
                }

                GUI.enabled = true;
            }
        }

        private void DrawResultSection()
        {
            EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"対象オブジェクト数: {_totalTargetObjectCount}");
                EditorGUILayout.LabelField($"Missing Script 合計数: {_totalMissingCount}");
                EditorGUILayout.LabelField($"検出オブジェクト数: {_results.Count}");

                EditorGUILayout.Space(4);

                if (_results.Count == 0)
                {
                    EditorGUILayout.HelpBox("まだスキャン結果がありません。", MessageType.None);
                    return;
                }

                _resultScroll = EditorGUILayout.BeginScrollView(_resultScroll);

                for (int i = 0; i < _results.Count; i++)
                {
                    var result = _results[i];

                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.ObjectField(result.gameObject, typeof(GameObject), true);
                            GUILayout.Label($"Missing: {result.missingCount}", GUILayout.Width(90));

                            if (GUILayout.Button("Ping", GUILayout.Width(50)))
                            {
                                EditorGUIUtility.PingObject(result.gameObject);
                            }

                            if (GUILayout.Button("Select", GUILayout.Width(55)))
                            {
                                Selection.activeObject = result.gameObject;
                                EditorGUIUtility.PingObject(result.gameObject);
                            }
                        }

                        EditorGUILayout.SelectableLabel(
                            result.hierarchyPath,
                            EditorStyles.textField,
                            GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void AddSelectionToRoots()
        {
            foreach (var go in Selection.gameObjects)
            {
                if (go == null) continue;
                if (_roots.Contains(go)) continue;
                _roots.Add(go);
            }
        }

        private void ReplaceRootsWithSelection()
        {
            _roots.Clear();
            AddSelectionToRoots();
        }

        private bool HasValidRoots()
        {
            for (int i = 0; i < _roots.Count; i++)
            {
                if (_roots[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void Scan()
        {
            if (_autoClearResultsBeforeScan)
            {
                _results.Clear();
            }

            _totalMissingCount = 0;
            _totalTargetObjectCount = 0;

            var visited = new HashSet<GameObject>();

            foreach (var root in _roots)
            {
                if (root == null) continue;

                if (_includeRootObject)
                {
                    ScanGameObject(root, visited);
                }

                foreach (Transform child in root.transform)
                {
                    ScanRecursive(child, visited);
                }
            }

            Repaint();

            Debug.Log(
                $"[MissingScriptCleaner] Scan completed. TargetObjects={_totalTargetObjectCount}, " +
                $"FoundObjects={_results.Count}, MissingCount={_totalMissingCount}");
        }

        private void ScanRecursive(Transform current, HashSet<GameObject> visited)
        {
            if (current == null) return;

            var go = current.gameObject;

            if (_includeInactive || go.activeInHierarchy)
            {
                ScanGameObject(go, visited);
            }

            foreach (Transform child in current)
            {
                ScanRecursive(child, visited);
            }
        }

        private void ScanGameObject(GameObject go, HashSet<GameObject> visited)
        {
            if (go == null) return;
            if (!visited.Add(go)) return;

            _totalTargetObjectCount++;

            int missingCount = CountMissingScripts(go);
            if (missingCount <= 0) return;

            _results.Add(new ScanResult
            {
                gameObject = go,
                hierarchyPath = GetHierarchyPath(go.transform),
                missingCount = missingCount
            });

            _totalMissingCount += missingCount;
        }

        private static int CountMissingScripts(GameObject go)
        {
            int missingCount = 0;

            SerializedObject serializedObject = new SerializedObject(go);
            SerializedProperty componentProp = serializedObject.FindProperty("m_Component");

            if (componentProp == null || !componentProp.isArray)
            {
                return 0;
            }

            for (int i = 0; i < componentProp.arraySize; i++)
            {
                SerializedProperty element = componentProp.GetArrayElementAtIndex(i);
                if (element == null) continue;

                SerializedProperty componentRef = element.FindPropertyRelative("component");
                if (componentRef == null) continue;

                if (componentRef.objectReferenceValue == null &&
                    componentRef.objectReferenceInstanceIDValue != 0)
                {
                    missingCount++;
                }
            }

            return missingCount;
        }

        private void SelectResults()
        {
            var objects = new List<Object>(_results.Count);

            foreach (var result in _results)
            {
                if (result?.gameObject != null)
                {
                    objects.Add(result.gameObject);
                }
            }

            Selection.objects = objects.ToArray();
        }

        private void RemoveMissingScripts()
        {
            if (_results.Count == 0 || _totalMissingCount <= 0)
            {
                EditorUtility.DisplayDialog("Missing Script Cleaner", "削除対象がありません。", "OK");
                return;
            }

            bool ok = EditorUtility.DisplayDialog(
                "Missing Script を削除",
                $"検出された Missing Script を削除します。\n\n" +
                $"対象オブジェクト数: {_results.Count}\n" +
                $"Missing Script 合計数: {_totalMissingCount}\n\n" +
                $"実行しますか？",
                "削除する",
                "キャンセル");

            if (!ok) return;

            int removedObjectCount = 0;
            int removedMissingCount = 0;

            StringBuilder logBuilder = new StringBuilder();
            logBuilder.AppendLine("[MissingScriptCleaner] Removed:");

            foreach (var result in _results)
            {
                if (result == null || result.gameObject == null) continue;

                Undo.RegisterCompleteObjectUndo(result.gameObject, "Remove Missing Scripts");

                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(result.gameObject);
                if (removed > 0)
                {
                    removedObjectCount++;
                    removedMissingCount += removed;

                    PrefabUtility.RecordPrefabInstancePropertyModifications(result.gameObject);
                    EditorUtility.SetDirty(result.gameObject);

                    logBuilder.AppendLine($"- {result.hierarchyPath} : {removed}");
                }
            }

            Debug.Log(logBuilder.ToString());
            Debug.Log(
                $"[MissingScriptCleaner] Complete. RemovedObjects={removedObjectCount}, RemovedMissingScripts={removedMissingCount}");

            AssetDatabase.SaveAssets();
            Scan();
        }

        private static string GetHierarchyPath(Transform current)
        {
            if (current == null) return string.Empty;

            var stack = new Stack<string>();
            while (current != null)
            {
                stack.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", stack.ToArray());
        }
    }
}
#endif