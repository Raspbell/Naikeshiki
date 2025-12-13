using UnityEngine;
using System.Collections;
using System;
using DG.Tweening;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using UniRx;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private bool allowArrowIcon;
    [SerializeField] private bool isGrowing;
    [SerializeField] private GameObject cursor;
    [SerializeField] private GameObject pickedSpritePrefab;
    [SerializeField] private GameObject completeCircle;
    [SerializeField] private Volume volume;
    [SerializeField] private Color outlineColor;
    [SerializeField] private ParticleSystem unpickEffect;
    [SerializeField] private CanvasGroup titleCanvasGroup;
    [SerializeField] private CanvasGroup arrowCanvasGroup;
    [SerializeField] private CanvasGroup clickToNextCanvas;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private AudioSource openingJingleAudioSource;
    [SerializeField] private float tolerance;
    [SerializeField] private float effectWaitingDuration = 0.2f;
    [SerializeField] private float spriteYOffset = 0.5f;
    [SerializeField] private bool isTutorial;
    [SerializeField] private bool isLastStage;
    [SerializeField] private Vector3 lastTargetPosition = new Vector3(0, -2, -10);
    [SerializeField] private float bloomIntensity = 0.5f;
    [SerializeField] private SceneTransition sceneTransition;
    [SerializeField] private CrossfadeAudioController crossfadeAudioController;
    [SerializeField] private SoundEffectInfo soundEffectInfo;
    [SerializeField] private SpriteInfo[] spriteInfos;

    public GameObject currentSpriteObject;

    private int nextPickIndex = 0;
    private float currentTolerance;
    private Tween spawnTween;
    private bool isPlaying = true;
    private Bloom bloom;
    private AudioSource audioSource;
    private float previousCameraY = 0;
    private float cameraStopTimeCounter = 0;
    private Tween arrowTween;
    private Tutorial tutorial;
    private CinemachinePositionComposer cinemachinePositionComposer;
    private int passedFlame = 0;
    private bool waitingAfterComplete = false;

    [Serializable]
    private class SoundEffectInfo
    {
        public AudioClip unpickClip;
        public AudioClip completeClip;
    }

    [Serializable]
    private class SpriteInfo
    {
        public Sprite sprite;
        public GameObject targetSpriteMask;
        public GameObject targetOrigin;
        public Color overridedOutlineColor = Color.clear;
        public float overridedTolerance = -1;
    }

    void Start()
    {
        crossfadeAudioController = FindFirstObjectByType<CrossfadeAudioController>();
        cinemachinePositionComposer = FindFirstObjectByType<CinemachinePositionComposer>();
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = GameOptions.SEVolume.Value;

        foreach (SpriteInfo pickedSprite in spriteInfos)
        {
            pickedSprite.targetSpriteMask.SetActive(false);
        }

        if (isTutorial)
        {
            tutorial = GetComponent<Tutorial>();
            tutorial.StartGuide(Tutorial.GuideType.Clock);
        }

        if (audioSource != null)
        {
            audioSource.volume = GameOptions.SEVolume.Value;
            GameOptions.SEVolume.Subscribe(volume =>
            {
                audioSource.volume = volume;
            }).AddTo(this);
        }
    }

    void Update()
    {
        if (passedFlame == 1)
        {
            PickNextSprite();
        }
        Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cinemachinePositionComposer.Composition.ScreenPosition.x = 0;
        if (isPlaying)
        {
            cursor.transform.position = new Vector3(cursorPosition.x, cursorPosition.y, 0);
            Vector3 tempPosition = Camera.main.transform.position;
            Camera.main.transform.position = new Vector3(0f, tempPosition.y, tempPosition.z);
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            GameOptions.BGMVolume.Value += 0.05f;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            GameOptions.BGMVolume.Value -= 0.05f;
        }

        if (!isLastStage && allowArrowIcon)
        {
            if (Camera.main.transform.position.y == previousCameraY)
            {
                cameraStopTimeCounter += Time.deltaTime;
                if (cameraStopTimeCounter >= 7.0f)
                {
                    if (arrowTween == null)
                    {
                        arrowTween = arrowCanvasGroup.DOFade(0.7f, 1.0f).SetEase(Ease.InQuint)
                            .OnComplete(() => arrowTween = null);
                    }
                }
            }
            else
            {
                cameraStopTimeCounter = 0;
                if (arrowTween == null)
                {
                    arrowTween = arrowCanvasGroup.DOFade(0.0f, 1.0f).SetEase(Ease.InQuint)
                        .OnComplete(() => arrowTween = null);
                }
            }
            previousCameraY = Camera.main.transform.position.y;
        }

        if (currentSpriteObject != null)
        {
            currentSpriteObject.transform.position = new Vector3(cursorPosition.x, cursorPosition.y, 0);
            if (Input.GetMouseButtonDown(0))
            {
                float distance = 100000000;
                if (spriteInfos[nextPickIndex].targetOrigin == null)
                {
                    distance = Vector3.Distance(currentSpriteObject.transform.position, spriteInfos[nextPickIndex].targetSpriteMask.transform.position);
                }
                else
                {
                    distance = Vector3.Distance(currentSpriteObject.transform.position, spriteInfos[nextPickIndex].targetOrigin.transform.position);
                }

                Debug.Log(distance);

                if (distance <= currentTolerance)
                {
                    spriteInfos[nextPickIndex].targetSpriteMask.SetActive(true);
                    if (nextPickIndex < spriteInfos.Length - 1)
                    {
                        UnpickSprite(() =>
                        {
                            PickNextSprite();
                        }, false);
                    }
                    else
                    {
                        UnpickSprite(() =>
                        {
                            isPlaying = false;
                            if (isTutorial || isLastStage)
                            {
                                MigrateToNextScene();
                            }
                            else
                            {
                                if (GameOptions.AutoChangeScene.Value)
                                {
                                    MigrateToNextScene();
                                }
                                else
                                {
                                    PlayCompleteEffect();
                                    GameOptions.CurrentSceneIndex++;
                                    clickToNextCanvas.DOFade(1.0f, 1.0f).SetEase(Ease.InQuint)
                                        .OnComplete(() =>
                                            waitingAfterComplete = true
                                        );

                                }
                            }
                        }, true);
                    }
                }
            }
        }

        if (waitingAfterComplete)
        {
            if (Input.GetMouseButtonDown(0))
            {
                waitingAfterComplete = false;
                if (isTutorial)
                {
                    crossfadeAudioController.ChangeClip(GameOptions.StageSelectBGM);
                    StartCoroutine(sceneTransition.StartSceneTransition("StageSelect", 0f));
                }
                else if (isLastStage)
                {
                    crossfadeAudioController.ChangeClip(GameOptions.StageSelectBGM);
                    StartCoroutine(sceneTransition.StartSceneTransition("StageSelect", 0f));
                }
                else
                {
                    crossfadeAudioController.ChangeClip(GameOptions.SceneInfos[GameOptions.CurrentSceneIndex].BGM);
                    StartCoroutine(sceneTransition.StartSceneTransition(GameOptions.SceneInfos[GameOptions.CurrentSceneIndex].SceneName, 0f));
                }
            }
        }
        passedFlame++;
    }

    public void PickNextSprite()
    {
        Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (spriteInfos[nextPickIndex] != null)
        {
            currentSpriteObject = Instantiate(pickedSpritePrefab);
            currentSpriteObject.GetComponent<SpriteRenderer>().sprite = spriteInfos[nextPickIndex].sprite;
            currentTolerance = spriteInfos[nextPickIndex].overridedTolerance == 0 ? tolerance : spriteInfos[nextPickIndex].overridedTolerance;
            currentSpriteObject.transform.position = new Vector3(cursorPosition.x, cursorPosition.y, 0);

            Sequence sequence = DOTween.Sequence();
            foreach (Transform child in currentSpriteObject.transform)
            {
                Vector3 basePosition = child.transform.localPosition;
                child.transform.localPosition += new Vector3(0, spriteYOffset, 0);
                child.GetComponent<SpriteRenderer>().material.SetColor("_Color", Color.clear);
                sequence.Join(child.transform.DOLocalMove(basePosition, 0.2f))
                        .Join(DOTween.To(() => child.GetComponent<SpriteRenderer>().material.GetColor("_Color"), x => child.GetComponent<SpriteRenderer>().material.SetColor("_Color", x), outlineColor, 0.2f));
            }
            UpdateOutlineSprite(spriteInfos[nextPickIndex].sprite, spriteInfos[nextPickIndex].overridedOutlineColor == Color.clear ? outlineColor : spriteInfos[nextPickIndex].overridedOutlineColor);
        }
    }

    void UnpickSprite(Action onComplete, bool isLast)
    {
        if (currentSpriteObject != null)
        {
            audioSource.PlayOneShot(soundEffectInfo.unpickClip);
            nextPickIndex++;
            Destroy(currentSpriteObject);
            currentSpriteObject = null;
            StartCoroutine(PlayEffectAndWait(onComplete, isLast));

            if (isTutorial)
            {
                tutorial.StopCurrentGuide();
                tutorial.StartGuide(nextPickIndex == 1 ? Tutorial.GuideType.Sapporo : Tutorial.GuideType.Clock);
            }
        }
    }

    void UpdateOutlineSprite(Sprite sprite, Color color)
    {
        SpriteRenderer[] spriteRenderers = currentSpriteObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer.gameObject == currentSpriteObject)
            {
                continue;
            }
            //spriteRenderer.material.SetColor("_Color", color);
            spriteRenderer.sprite = sprite;
        }
    }

    IEnumerator PlayEffectAndWait(Action onComplete, bool isLast)
    {
        if (spriteInfos[nextPickIndex - 1].targetOrigin == null)
        {
            unpickEffect.transform.position = spriteInfos[nextPickIndex - 1].targetSpriteMask.transform.position;
        }
        else
        {
            unpickEffect.transform.position = spriteInfos[nextPickIndex - 1].targetOrigin.transform.position;
        }
        unpickEffect.Play();
        yield return new WaitForSeconds(isLast ? 0 : effectWaitingDuration);
        onComplete?.Invoke();
    }

    void PlayCompleteEffect()
    {
        Vector3 cameraCenter = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        cameraCenter.z = cursor.transform.position.z;
        completeCircle.transform.position = cameraCenter;
        completeCircle.transform.DOScale(2f, 0.75f);
        if (!isLastStage)
        {
            audioSource.PlayOneShot(soundEffectInfo.completeClip);
        }
    }

    void MigrateToNextScene()
    {
        if (isTutorial)
        {
            TurtorialStageEffect();
            return;
        }
        else if (isLastStage)
        {
            LastStageEffect();
            return;
        }
        PlayCompleteEffect();
        crossfadeAudioController.ChangeClip(GameOptions.SceneInfos[GameOptions.CurrentSceneIndex].BGM);
        GameOptions.CurrentSceneIndex++;
        if (GameOptions.AutoChangeScene.Value)
        {
            StartCoroutine(sceneTransition.StartSceneTransition(GameOptions.SceneInfos[GameOptions.CurrentSceneIndex].SceneName, 0f));
        }
    }

    void LaunchBloomEffect()
    {
        volume.profile.TryGet<Bloom>(out bloom);
        Sequence sequence = DOTween.Sequence();
        sequence.SetDelay(1.2f)
            .Append(DOTween.To(() => bloom.intensity.value, x => bloom.intensity.value = x, bloomIntensity, 2.5f));
    }

    void TurtorialStageEffect()
    {
        cinemachineBrain.enabled = false;
        PlayCompleteEffect();

        Sequence sequence = DOTween.Sequence();
        float soundTimingAdjustment = 1.5f;

        sequence
            .SetDelay(0.5f)
            // 1) カメラ移動
            .Append(Camera.main.transform.DOMove(new Vector3(0f, -2f, -10f), 1.0f))
            // 3) タイトルのフェードイン
            .Append(titleCanvasGroup.DOFade(1.0f, 2.0f).SetEase(Ease.InQuint))
            .InsertCallback(1.0f + soundTimingAdjustment, () =>
            {
                openingJingleAudioSource.Play();
            })
            // 4) タイトルのフェード完了後にクリック案内をフェードイン
            .AppendInterval(3.5f)
            .Append(clickToNextCanvas.DOFade(1.0f, 1.0f).SetEase(Ease.InQuint))

            // 5) すべて完了後のフラグ立て
            .OnComplete(() =>
            {
                waitingAfterComplete = true;
            });

    }

    void LastStageEffect()
    {
        cinemachineBrain.enabled = false;
        Sequence sequence = DOTween.Sequence();
        PlayCompleteEffect();
        crossfadeAudioController.ChangeClip(GameOptions.SceneInfos[GameOptions.CurrentSceneIndex + 1].BGM);

        sequence
            .SetDelay(1.0f)
            // 1) カメラ移動
            .Append(Camera.main.transform.DOMove(new Vector3(0f, -2f, -10f), 1.0f))
            // 2) カメラ移動終了後にコールバック
            .AppendCallback(() =>
            {
                // crossfadeAudioController.MoveToNextClip();
            })
            // 3) タイトルのフェードイン
            .Append(titleCanvasGroup.DOFade(1.0f, 2.0f).SetEase(Ease.InQuint))
            // 4) タイトルのフェード完了後にクリック案内をフェードイン
            .Append(clickToNextCanvas.DOFade(1.0f, 1.0f).SetEase(Ease.InQuint))

            // 5) すべて完了後のフラグ立て
            .OnComplete(() =>
            {
                waitingAfterComplete = true;
            });


        if (isGrowing)
        {
            LaunchBloomEffect();
        }
    }
}
