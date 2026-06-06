//============================================================
//  SceneNavigatorWindow.cs
//  Unity エディタ拡張：ビルド設定に登録されたシーンを一覧表示し、
//  ワンクリックでシーン遷移・再生を行うカスタムウィンドウ
//============================================================

using UnityEditor;                         // エディタ拡張 API
using UnityEngine;                         // 基本エンジン API
using UnityEditor.SceneManagement;         // エディタ上のシーン制御 API
using UnityEngine.SceneManagement;         // ランタイム／エディタ共有のシーン管理 API
using System.IO;                           // パス操作

namespace Malen.EditorTools
{
    /// <summary>
    /// セッション間で用いるキー
    /// </summary>
    internal static class SceneNavigatorSessionKeys
    {
        public const string ShouldReturnKey = "SceneNavigator_ShouldReturnToPrev";
        public const string ReturnPathKey = "SceneNavigator_PrevScenePath";

        // --- 追加: リスタート処理用キー ---
        public const string IsRestartingKey = "SceneNavigator_IsRestarting";
        public const string NextScenePathKey = "SceneNavigator_NextScenePath";
    }

    /// <summary>
    /// 再生状態の監視と復帰処理
    /// </summary>
    [InitializeOnLoad]
    internal static class SceneNavigatorPlayModeWatcher
    {
        static SceneNavigatorPlayModeWatcher()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 再生停止後にエディットモードへ戻った時点で処理を行う
            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            // ---------------------------------------------------------
            // 1. リスタート（再生中のシーン切り替え）判定
            // ---------------------------------------------------------
            bool isRestarting = SessionState.GetBool(SceneNavigatorSessionKeys.IsRestartingKey, false);
            if (isRestarting)
            {
                // フラグ消費
                SessionState.SetBool(SceneNavigatorSessionKeys.IsRestartingKey, false);
                string nextScenePath = SessionState.GetString(SceneNavigatorSessionKeys.NextScenePathKey, string.Empty);
                SessionState.EraseString(SceneNavigatorSessionKeys.NextScenePathKey);

                if (!string.IsNullOrEmpty(nextScenePath) && File.Exists(nextScenePath))
                {
                    // 次のシーンを開いて即再生
                    // (直前まで再生モードだったので変更保存の確認は不要とみなす)
                    try
                    {
                        EditorSceneManager.OpenScene(nextScenePath, OpenSceneMode.Single);
                        EditorApplication.isPlaying = true;
                    }
                    catch
                    {
                        Debug.LogError($"[SceneNavigator] Failed to restart scene: {nextScenePath}");
                    }

                    // リスタート時は「元のシーンへの復帰処理」を行わずにここで終了
                    return;
                }
            }

            // ---------------------------------------------------------
            // 2. 通常の復帰処理（元のシーンに戻る）
            // ---------------------------------------------------------
            bool shouldReturn = SessionState.GetBool(SceneNavigatorSessionKeys.ShouldReturnKey, false);
            string prevPath = SessionState.GetString(SceneNavigatorSessionKeys.ReturnPathKey, string.Empty);

            if (!shouldReturn || string.IsNullOrEmpty(prevPath))
            {
                return;
            }

            // フラグを先に落として二重復帰を防止
            SessionState.SetBool(SceneNavigatorSessionKeys.ShouldReturnKey, false);
            SessionState.EraseString(SceneNavigatorSessionKeys.ReturnPathKey);

            // 既に同一シーンであれば何もしない
            string currentPath = SceneManager.GetActiveScene().path;
            if (currentPath == prevPath)
            {
                return;
            }

            // 変更がある場合は保存確認
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            // 元のシーンへ復帰
            try
            {
                EditorSceneManager.OpenScene(prevPath, OpenSceneMode.Single);
            }
            catch
            {
                // 復帰失敗時はダイアログ通知のみ行う
                EditorUtility.DisplayDialog("Scene Navigator", $"元のシーンへ復帰できませんでした:\n{prevPath}", "OK");
            }
        }
    }

    /// <summary>
    /// シーン遷移／再生支援ウィンドウ
    /// </summary>
    public class SceneNavigatorWindow : EditorWindow
    {
        private Vector2 _scroll;                  // スクロール位置
        private string _searchKeyword = "";       // フィルタ文字列
        private GUIStyle _headerStyle;            // 見出しスタイル

        /// <summary>
        /// メニュー & ショートカット
        /// </summary>
        [MenuItem("Tools/Scene Navigator %#l", priority = 2000)]
        private static void OpenWindow()
        {
            SceneNavigatorWindow window = GetWindow<SceneNavigatorWindow>();
            window.titleContent = new GUIContent("Scene Navigator");
            window.minSize = new Vector2(360, 260);
        }

        /// <summary>
        /// 有効化時
        /// </summary>
        private void OnEnable()
        {
            _headerStyle = null;
        }

        /// <summary>
        /// GUI 描画
        /// </summary>
        private void OnGUI()
        {
            EnsureStyles();

            DrawSearchBar();
            EditorGUILayout.Space(4);
            DrawSceneList();
            EditorGUILayout.Space(8);
            DrawOpenScenesSummary();
        }

        /// <summary>
        /// スタイルの遅延初期化
        /// </summary>
        private void EnsureStyles()
        {
            if (_headerStyle != null) return;

            GUIStyle baseStyle = null;
            try { baseStyle = EditorStyles.label; } catch { baseStyle = null; }

            if (baseStyle == null)
                baseStyle = GUI.skin != null ? GUI.skin.label : new GUIStyle();

            _headerStyle = new GUIStyle(baseStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
        }

        //===========================  UI 部品  ===========================

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("Search", GUILayout.Width(50));

                string newKeyword = EditorGUILayout.TextField(_searchKeyword);
                if (newKeyword != _searchKeyword)
                {
                    _searchKeyword = newKeyword;
                }

                if (GUILayout.Button("Refresh", GUILayout.Width(80)))
                {
                    Repaint();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSceneList()
        {
            GUILayout.Label("Scenes In Build Settings", _headerStyle);

            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            if (buildScenes == null || buildScenes.Length == 0)
            {
                EditorGUILayout.HelpBox("Build Settings にシーンが登録されていません。", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            {
                foreach (EditorBuildSettingsScene buildScene in buildScenes)
                {
                    if (buildScene == null || string.IsNullOrEmpty(buildScene.path)) continue;

                    string sceneName = Path.GetFileNameWithoutExtension(buildScene.path);

                    if (!string.IsNullOrEmpty(_searchKeyword) &&
                        !sceneName.ToLower().Contains(_searchKeyword.ToLower()))
                    {
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(sceneName, GUILayout.MaxWidth(220));

                        if (GUILayout.Button("Open", GUILayout.Width(70)))
                        {
                            HandleOpenScene(buildScene.path);
                        }

                        if (GUILayout.Button("Play", GUILayout.Width(70)))
                        {
                            HandlePlayScene(buildScene.path);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawOpenScenesSummary()
        {
            GUILayout.Label("Currently Open Scenes", _headerStyle);

            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                EditorGUILayout.LabelField($"[{i}] {s.name}");
            }
        }

        //===========================  処理部  ===========================

        private void HandleOpenScene(string scenePath)
        {
            if (!File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog("Scene Navigator", $"シーンが見つかりません:\n{scenePath}", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        /// <summary>
        /// 指定シーンで再生を開始し、停止後に元シーンへ復帰
        /// </summary>
        private void HandlePlayScene(string targetScenePath)
        {
            if (!File.Exists(targetScenePath))
            {
                EditorUtility.DisplayDialog("Scene Navigator", $"シーンが見つかりません:\n{targetScenePath}", "OK");
                return;
            }

            // --------------------------------------------------
            // 修正: 再生中の場合は「リスタート予約」をして停止する
            // --------------------------------------------------
            if (EditorApplication.isPlaying)
            {
                // リスタート用の情報をセット
                SessionState.SetBool(SceneNavigatorSessionKeys.IsRestartingKey, true);
                SessionState.SetString(SceneNavigatorSessionKeys.NextScenePathKey, targetScenePath);

                // 元シーンへの復帰情報は「まだ保持したまま」にするため、
                // ShouldReturnKeyなどは触らない（これにより、最終的に再生を完全に止めた時に最初のシーンに戻れる）

                // 再生停止 -> WatcherのEnteredEditModeで再開処理へ
                EditorApplication.isPlaying = false;
                return;
            }

            // --- 以下、通常（非再生時）の処理 ---

            string currentPath = SceneManager.GetActiveScene().path;

            // 同一シーンなら復帰不要
            if (currentPath == targetScenePath)
            {
                SessionState.SetBool(SceneNavigatorSessionKeys.ShouldReturnKey, false);
                SessionState.EraseString(SceneNavigatorSessionKeys.ReturnPathKey);

                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }

                EditorApplication.isPlaying = true;
                return;
            }

            // 復帰先を記録
            SessionState.SetString(SceneNavigatorSessionKeys.ReturnPathKey, currentPath);
            SessionState.SetBool(SceneNavigatorSessionKeys.ShouldReturnKey, true);

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                // 保存キャンセル時は復帰情報を破棄
                SessionState.SetBool(SceneNavigatorSessionKeys.ShouldReturnKey, false);
                SessionState.EraseString(SceneNavigatorSessionKeys.ReturnPathKey);
                return;
            }

            // 対象シーンを開いてから再生
            try
            {
                EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
            }
            catch
            {
                SessionState.SetBool(SceneNavigatorSessionKeys.ShouldReturnKey, false);
                SessionState.EraseString(SceneNavigatorSessionKeys.ReturnPathKey);
                EditorUtility.DisplayDialog("Scene Navigator", $"シーンを開けませんでした:\n{targetScenePath}", "OK");
                return;
            }

            EditorApplication.isPlaying = true;
        }
    }
}