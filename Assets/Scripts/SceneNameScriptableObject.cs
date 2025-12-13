using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneNameScriptableObject", menuName = "ScriptableObjects/SceneNameScriptableObject", order = 1)]
public class SceneNameScriptableObject : ScriptableObject
{
    public SceneInfo[] sceneInfos;

    [Serializable]
    public class SceneInfo
    {
        public string SceneName;
        public AudioClip BGM;
    }
}
