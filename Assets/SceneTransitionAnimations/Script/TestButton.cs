using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestButton : MonoBehaviour
{

    public SceneTransition stm;

    public void OnClickSceneChangeButton(string transitionSceneName)
    {
        stm.StartSceneTransition(transitionSceneName);
    }
}
