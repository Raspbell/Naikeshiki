using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

namespace Malen.EditorTools
{
    internal static class SceneControllerSessionKeys
    {
        public const string ShouldReturnKey = "SceneController_ShouldReturnToPrev";
        public const string ReturnPathKey = "SceneController_PrevScenePath";
    }


    [InitializeOnLoad]
    internal static class SceneControllerPlayModeWatcher
    {
        static SceneControllerPlayModeWatcher()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            bool shouldReturn = SessionState.GetBool(SceneControllerSessionKeys.ShouldReturnKey, false);
            string prevPath = SessionState.GetString(SceneControllerSessionKeys.ReturnPathKey, string.Empty);

            if (!shouldReturn || string.IsNullOrEmpty(prevPath))
            {
                return;
            }

            SessionState.SetBool(SceneControllerSessionKeys.ShouldReturnKey, false);
            SessionState.EraseString(SceneControllerSessionKeys.ReturnPathKey);

            string currentPath = SceneManager.GetActiveScene().path;
            if (currentPath == prevPath)
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            try
            {
                EditorSceneManager.OpenScene(prevPath, OpenSceneMode.Single);
            }
            catch
            {
                return;
            }
        }
    }

    public class SceneControllerWindow : EditorWindow
    {
        private Vector2 _scroll;
        private string _searchKeyword = "";
        private GUIStyle _headerStyle;

        [MenuItem("Tools/Scene Controller")]
        private static void OpenWindow()
        {
            SceneControllerWindow window = GetWindow<SceneControllerWindow>();
            window.titleContent = new GUIContent("Scene Controller");
            window.minSize = new Vector2(360, 260);
        }

        private void OnEnable()
        {
            _headerStyle = null;
        }

        private void OnGUI()
        {
            EnsureStyles();

            DrawSearchBar();
            EditorGUILayout.Space(4);
            DrawSceneList();
            EditorGUILayout.Space(8);
            DrawOpenScenesSummary();
        }

        private void EnsureStyles()
        {
            if (_headerStyle != null)
            {
                return;
            }

            GUIStyle baseStyle = null;
            try
            {
                baseStyle = EditorStyles.label;
            }
            catch
            {
                baseStyle = null;
            }

            if (baseStyle == null)
            {
                baseStyle = GUI.skin != null ? GUI.skin.label : new GUIStyle();
            }

            _headerStyle = new GUIStyle(baseStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
        }

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
                    if (buildScene == null || string.IsNullOrEmpty(buildScene.path))
                    {
                        continue;
                    }

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

        private void HandleOpenScene(string scenePath)
        {
            if (!File.Exists(scenePath))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        private void HandlePlayScene(string targetScenePath)
        {
            if (!File.Exists(targetScenePath))
            {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            string currentPath = SceneManager.GetActiveScene().path;

            if (currentPath == targetScenePath)
            {
                SessionState.SetBool(SceneControllerSessionKeys.ShouldReturnKey, false);
                SessionState.EraseString(SceneControllerSessionKeys.ReturnPathKey);

                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }

                EditorApplication.isPlaying = true;
                return;
            }

            SessionState.SetString(SceneControllerSessionKeys.ReturnPathKey, currentPath);
            SessionState.SetBool(SceneControllerSessionKeys.ShouldReturnKey, true);

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                SessionState.SetBool(SceneControllerSessionKeys.ShouldReturnKey, false);
                SessionState.EraseString(SceneControllerSessionKeys.ReturnPathKey);
                return;
            }

            try
            {
                EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
            }
            catch
            {
                SessionState.SetBool(SceneControllerSessionKeys.ShouldReturnKey, false);
                SessionState.EraseString(SceneControllerSessionKeys.ReturnPathKey);
                return;
            }

            EditorApplication.isPlaying = true;
        }
    }
}
