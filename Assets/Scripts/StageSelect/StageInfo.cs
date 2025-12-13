using JetBrains.Annotations;
using UnityEngine;

[CreateAssetMenu(fileName = "StageInfo", menuName = "ScriptableObjects/StageInfo", order = 1)]
public class StageInfo : ScriptableObject
{
    public string stageName;
    public SceneNameScriptableObject sceneNameScriptableObject;
    public Difficulty difficulty;

    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
        Extra
    }
}
