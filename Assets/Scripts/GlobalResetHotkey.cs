using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalResetHotkey : MonoBehaviour
{
    private const string InitialSceneName = "Initial";
    private const float DoublePressWindowSeconds = 1f;

    private static GlobalResetHotkey instance;

    private float firstWheelPressTime = -1f;
    private bool resetStarted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    internal static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        GameObject obj = new GameObject("[GlobalResetHotkey]");
        DontDestroyOnLoad(obj);
        instance = obj.AddComponent<GlobalResetHotkey>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (resetStarted)
        {
            return;
        }

        if (!Input.GetMouseButton(1))
        {
            firstWheelPressTime = -1f;
            return;
        }

        if (!Input.GetMouseButtonDown(2))
        {
            return;
        }

        float now = Time.unscaledTime;
        if (firstWheelPressTime >= 0f && now - firstWheelPressTime <= DoublePressWindowSeconds)
        {
            resetStarted = true;
            FullGameResetRunner.StartReset(InitialSceneName);
            return;
        }

        firstWheelPressTime = now;
    }
}

internal class FullGameResetRunner : MonoBehaviour
{
    public static void StartReset(string initialSceneName)
    {
        GameObject obj = new GameObject("[FullGameResetRunner]");
        DontDestroyOnLoad(obj);
        FullGameResetRunner runner = obj.AddComponent<FullGameResetRunner>();
        runner.StartCoroutine(runner.ResetRoutine(initialSceneName));
    }

    private IEnumerator ResetRoutine(string initialSceneName)
    {
        Time.timeScale = 1f;
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        DOTween.KillAll();
        GameOptions.ResetRuntimeState();

        DestroyDontDestroyOnLoadRootsExcept(gameObject);

        yield return null;

        SceneManager.LoadScene(initialSceneName, LoadSceneMode.Single);

        yield return null;

        GlobalResetHotkey.EnsureInstance();
        Destroy(gameObject);
    }

    private static void DestroyDontDestroyOnLoadRootsExcept(GameObject excludedObject)
    {
        GameObject probe = new GameObject("[DontDestroyOnLoadProbe]");
        DontDestroyOnLoad(probe);
        Scene dontDestroyOnLoadScene = probe.scene;
        GameObject[] roots = dontDestroyOnLoadScene.GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            if (root == null || root == probe || root == excludedObject)
            {
                continue;
            }

            Destroy(root);
        }

        Destroy(probe);
    }
}
