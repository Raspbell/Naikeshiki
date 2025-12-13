using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{

    [Header("シーン遷移オブジェクト")] public SceneTransitionObject[] sceneTransitionObjects;
    [Header("アニメーションフェーズ")] public TransitionPhase transitionPhase = TransitionPhase.In;
    [Header("アニメーションタイプ")] public TransitionType transitionType = TransitionType.Bar_Slide;

    // Bar_Slide, Bar_Flip, Tile_Slide
    [HideInInspector][Header("オブジェクトの移動開始間隔")] public float sceneTransitionStartInterval;
    [HideInInspector][Header("オブジェクトの移動時間")] public float sceneTransitionSpeed;

    // Tile_Rotate
    [HideInInspector][Header("回転角")] public float sceneTransitionRadian;

    // Sprite
    [HideInInspector][Header("マスクするスプライト")] public Sprite sceneTransitionSprite;
    [HideInInspector][Header("マスクカラー")] public Color sceneTransitionSpriteColor;
    [HideInInspector][Header("マスクオブジェクトの最大サイズ")] public Vector3 sceneTransitionMaxScale;
    [HideInInspector][Header("スプライトの拡大速度")] public float sceneTransitionSpriteSpeed;
    [HideInInspector] public GameObject square;
    [HideInInspector] public SpriteMask mask;

    [Header("シーン遷移までの時間")] public float timeUpToSceneTransition;

    private string transitionSceneName;
    private bool sceneTransitionFlag = false;
    private GameObject sceneTransitionImages;
    private float sceneTransitionTime = 0;
    private int lastStartedTransitionObjectIndex = -1;

    public enum TransitionPhase { In, Out }
    public enum TransitionType
    {
        Bar_Slide,
        Bar_Flip,
        Tile_Slide,
        Tile_Flip,
        Tile_Rotate,
        Sprite
    }

    [Serializable]
    public class SceneTransitionObject
    {
        public GameObject transitionObject;
        public Vector3 targetPoint;
        public int order;
    }

    private void Start()
    {
        DOTween.SetTweensCapacity(200, 200);
        if (transitionPhase == TransitionPhase.Out)
        {
            sceneTransitionFlag = true;
            DOTween.KillAll();
        }

        foreach (Transform child in transform)
        {
            if (child.gameObject.name == "Scene Transition Images")
            {
                sceneTransitionImages = child.gameObject;
            }
        }
        sceneTransitionImages.SetActive(true);
        sceneTransitionTime = 0;
    }

    public void FixedUpdate()
    {
        if (sceneTransitionFlag)
        {

            switch (transitionType)
            {
                case TransitionType.Bar_Slide:
                    sceneTransitionTime += Time.deltaTime;
                    if (lastStartedTransitionObjectIndex < sceneTransitionObjects.Length - 1)
                    {
                        if (sceneTransitionTime > (lastStartedTransitionObjectIndex + 1) * sceneTransitionStartInterval)
                        {
                            sceneTransitionObjects[lastStartedTransitionObjectIndex + 1].transitionObject.transform.DOLocalMove(sceneTransitionObjects[lastStartedTransitionObjectIndex + 1].targetPoint, sceneTransitionSpeed);
                            lastStartedTransitionObjectIndex++;
                        }
                    }
                    if (timeUpToSceneTransition < sceneTransitionTime)
                    {
                        if (transitionPhase == TransitionPhase.In)
                        {
                            SceneManager.LoadScene(transitionSceneName);
                        }
                        else if (transitionPhase == TransitionPhase.Out)
                        {
                            sceneTransitionImages.SetActive(false);
                            sceneTransitionFlag = false;
                        }
                    }
                    break;

                case TransitionType.Bar_Flip:
                    sceneTransitionTime += Time.deltaTime;
                    if (lastStartedTransitionObjectIndex < sceneTransitionObjects.Length - 1)
                    {
                        if (sceneTransitionTime > (lastStartedTransitionObjectIndex + 1) * sceneTransitionStartInterval)
                        {
                            sceneTransitionObjects[lastStartedTransitionObjectIndex + 1].transitionObject.transform.DOLocalRotate(sceneTransitionObjects[lastStartedTransitionObjectIndex + 1].targetPoint, sceneTransitionSpeed);
                            lastStartedTransitionObjectIndex++;
                        }
                    }
                    if (timeUpToSceneTransition < sceneTransitionTime)
                    {
                        if (transitionPhase == TransitionPhase.In)
                        {
                            SceneManager.LoadScene(transitionSceneName);
                        }
                        else if (transitionPhase == TransitionPhase.Out)
                        {
                            sceneTransitionImages.SetActive(false);
                            sceneTransitionFlag = false;
                        }
                    }
                    break;

                case TransitionType.Tile_Slide:
                    sceneTransitionTime += Time.deltaTime;
                    if (lastStartedTransitionObjectIndex < sceneTransitionObjects.Length - 1)
                    {
                        if (sceneTransitionTime > (lastStartedTransitionObjectIndex + 1) * sceneTransitionStartInterval)
                        {
                            foreach (SceneTransitionObject sceneTransitionObject in sceneTransitionObjects)
                            {
                                if (sceneTransitionObject.order == lastStartedTransitionObjectIndex + 1)
                                {
                                    sceneTransitionObject.transitionObject.transform.DOLocalMove(sceneTransitionObject.targetPoint, sceneTransitionSpeed);
                                }
                            }
                            lastStartedTransitionObjectIndex++;
                        }
                    }
                    if (timeUpToSceneTransition < sceneTransitionTime)
                    {
                        if (transitionPhase == TransitionPhase.In)
                        {
                            SceneManager.LoadScene(transitionSceneName);
                        }
                        else if (transitionPhase == TransitionPhase.Out)
                        {
                            sceneTransitionImages.SetActive(false);
                            sceneTransitionFlag = false;
                        }
                    }
                    break;

                case TransitionType.Tile_Flip:
                    sceneTransitionTime += Time.deltaTime;
                    if (lastStartedTransitionObjectIndex < sceneTransitionObjects.Length - 1)
                    {
                        if (sceneTransitionTime > (lastStartedTransitionObjectIndex + 1) * sceneTransitionStartInterval)
                        {
                            foreach (SceneTransitionObject sceneTransitionObject in sceneTransitionObjects)
                            {
                                if (sceneTransitionObject.order == lastStartedTransitionObjectIndex + 1)
                                {
                                    sceneTransitionObject.transitionObject.transform.DOLocalRotate(sceneTransitionObject.targetPoint, sceneTransitionSpeed);
                                }
                            }
                            lastStartedTransitionObjectIndex++;
                        }
                    }
                    if (timeUpToSceneTransition < sceneTransitionTime)
                    {
                        if (transitionPhase == TransitionPhase.In)
                        {
                            SceneManager.LoadScene(transitionSceneName);
                        }
                        else if (transitionPhase == TransitionPhase.Out)
                        {
                            sceneTransitionImages.SetActive(false);
                            sceneTransitionFlag = false;
                        }
                    }
                    break;

                case TransitionType.Tile_Rotate:
                    sceneTransitionTime += Time.deltaTime;
                    if (lastStartedTransitionObjectIndex < sceneTransitionObjects.Length - 1)
                    {
                        if (sceneTransitionTime > (lastStartedTransitionObjectIndex + 1) * sceneTransitionStartInterval)
                        {
                            foreach (SceneTransitionObject sceneTransitionObject in sceneTransitionObjects)
                            {
                                if (sceneTransitionObject.order == lastStartedTransitionObjectIndex + 1)
                                {
                                    Sequence sequence = DOTween.Sequence();
                                    sequence.Append(sceneTransitionObject.transitionObject.transform.DOScale(sceneTransitionObject.targetPoint, sceneTransitionSpeed).SetEase(Ease.Linear));
                                    sequence.Join(sceneTransitionObject.transitionObject.transform.DOLocalRotate(new Vector3(0, 0, sceneTransitionRadian), sceneTransitionSpeed, RotateMode.FastBeyond360).SetEase(Ease.Linear));
                                }
                            }
                            lastStartedTransitionObjectIndex++;
                        }
                    }
                    if (timeUpToSceneTransition < sceneTransitionTime)
                    {
                        if (transitionPhase == TransitionPhase.In)
                        {
                            SceneManager.LoadScene(transitionSceneName);
                        }
                        else if (transitionPhase == TransitionPhase.Out)
                        {
                            sceneTransitionImages.SetActive(false);
                            sceneTransitionFlag = false;
                        }
                    }
                    break;

                case TransitionType.Sprite:
                    if (sceneTransitionTime == 0)
                    {
                        sceneTransitionObjects[0].transitionObject.transform.DOScale(sceneTransitionMaxScale, sceneTransitionSpriteSpeed);
                        square.GetComponent<SpriteRenderer>().color = sceneTransitionSpriteColor;
                        mask.sprite = sceneTransitionSprite;
                    }
                    sceneTransitionTime += Time.deltaTime;

                    if (timeUpToSceneTransition < sceneTransitionTime)
                    {
                        if (transitionPhase == TransitionPhase.In)
                        {
                            SceneManager.LoadScene(transitionSceneName);
                        }
                        else if (transitionPhase == TransitionPhase.Out)
                        {
                            sceneTransitionImages.SetActive(false);
                            sceneTransitionFlag = false;
                        }
                    }
                    break;
            }
        }
    }

    private void ChangeColorInChildren(Transform current)
    {
        // 現在のオブジェクトのSpriteRendererコンポーネントがあれば色を変更する
        if (current.GetComponent<SpriteRenderer>() != null)
        {
            current.GetComponent<SpriteRenderer>().color = sceneTransitionSpriteColor;
        }

        // 子オブジェクトがあれば再帰的に処理を行う
        foreach (Transform child in current)
        {
            ChangeColorInChildren(child);
        }
    }

    /// <summary>
    /// �A�j���[�V������Đ������̂��w�肵���V�[���ɑJ��
    /// </summary>
    public IEnumerator StartSceneTransition(string sceneName, float waitTime = 0f)
    {
        yield return new WaitForSeconds(waitTime);
        transitionSceneName = sceneName;
        sceneTransitionFlag = true;
        DOTween.KillAll();
    }
}
