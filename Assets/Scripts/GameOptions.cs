using UnityEngine;
using UniRx;

[DefaultExecutionOrder(-15)]
public class GameOptions : MonoBehaviour
{
    public static SceneNameScriptableObject.SceneInfo[] SceneInfos;
    public static int CurrentSceneIndex;
    public static AudioClip StageSelectBGM;
    public static ReactiveProperty<float> BGMVolume = new ReactiveProperty<float>(1f);
    public static ReactiveProperty<float> SEVolume = new ReactiveProperty<float>(1f);
    public static ReactiveProperty<bool> AutoChangeScene = new ReactiveProperty<bool>(false);
    public static ReactiveProperty<bool> UseHelpMode = new ReactiveProperty<bool>(false);
    public static ReactiveProperty<float> TimeForHint = new ReactiveProperty<float>(180);

    [SerializeField] private SceneNameScriptableObject sceneNameScriptableObject;
    [SerializeField] private AudioClip stageSelectBGM;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        StageSelectBGM = stageSelectBGM;
        BGMVolume.Value = PlayerPrefs.GetFloat("BGMVolume", 0.75f);
        SEVolume.Value = PlayerPrefs.GetFloat("SEVolume", 0.75f);
        AutoChangeScene.Value = PlayerPrefs.GetInt("AutoSkip", 0) == 1 ? true : false;
        UseHelpMode.Value = PlayerPrefs.GetInt("UseHelpMode", 1) == 1 ? true : false;
        TimeForHint.Value = PlayerPrefs.GetFloat("TimeForHint", 180f);
        DumpPlayerPrefs();
    }

    public static void InitFields(StageInfo stageInfo)
    {
        SceneInfos = stageInfo.sceneNameScriptableObject.sceneInfos;
        CurrentSceneIndex = 0;
    }

    private void DumpPlayerPrefs()
    {
        Debug.Log("BGMVolume: " + BGMVolume.Value);
        Debug.Log("SEVolume: " + SEVolume.Value);
        Debug.Log("AutoSkip: " + AutoChangeScene.Value);
        Debug.Log("UseHelpMode: " + UseHelpMode.Value);
        Debug.Log("TimeForHint: " + TimeForHint.Value);
    }
}

