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
    [SerializeField] private float completeCircleScale = 2f;
    [SerializeField] private bool isTutorial;
    [SerializeField] private bool isLastStage;
    [SerializeField] private Vector3 lastTargetPosition = new Vector3(0, -2, -10);
    [SerializeField] private float bloomIntensity = 0.5f;
    [SerializeField] private SceneTransition sceneTransition;
    [SerializeField] private CrossfadeAudioController crossfadeAudioController;
    [SerializeField] private SoundEffectInfo soundEffectInfo;
    [SerializeField] private SpriteInfo[] spriteInfos;

    [SerializeField] private float ver1CameraSize = 13f; // バージョン1 で使っていた画像に対するカメラサイズ参照用

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
    private float cameraSizeRatio = 1f;

    // --- ヒント機能用変数 ---
    private float lastActionTime; // 最後に正解した時間（または開始時間）
    private GameObject currentHintObject; // 現在表示中のヒントオブジェクト
    private Tween hintTween; // ヒントの点滅Tween
    // ---------------------------

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
        cameraSizeRatio = Camera.main.orthographicSize / ver1CameraSize;
        unpickEffect.transform.localScale = unpickEffect.transform.localScale * cameraSizeRatio;

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

        // --- タイマー初期化 ---
        lastActionTime = Time.time;

        // デバッグログ: 現在設定されているヒント時間を表示
        Debug.Log($"Hint System Initialized. TimeForHint: {GameOptions.TimeForHint.Value} seconds.");
        // -------------------------
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

            // --- ヒント表示判定 ---
            // チュートリアル以外、かつヘルプモード有効、かつ最後のステージでない場合
            if (!isTutorial && !isLastStage && GameOptions.UseHelpMode.Value)
            {
                CheckAndShowHint();
            }
            // -------------------------
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

                // Debug.Log(distance);

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

    // --- ヒント表示用メソッド ---
    private void CheckAndShowHint()
    {
        // 既にヒントが出ている場合は何もしない
        if (currentHintObject != null) return;

        // 経過時間が設定を超えているかチェック
        // GameOptions.TimeForHint.Value は PlayerPrefs に保存された値が優先されます
        if (Time.time - lastActionTime > GameOptions.TimeForHint.Value)
        {
            ShowHint();
        }
    }

    private void ShowHint()
    {
        if (nextPickIndex >= spriteInfos.Length) return;

        var currentInfo = spriteInfos[nextPickIndex];

        // 正解位置を取得 (TargetOrigin優先、なければMask)
        Transform targetTransform = currentInfo.targetOrigin != null
            ? currentInfo.targetOrigin.transform
            : currentInfo.targetSpriteMask.transform;

        // 【修正】ヒント生成: 
        // 回転は targetTransform.rotation (正解配置の傾き) ではなく
        // pickedSpritePrefab.transform.rotation (カーソル持ち状態と同じ) を使用します
        currentHintObject = Instantiate(pickedSpritePrefab, targetTransform.position, pickedSpritePrefab.transform.rotation);

        // 【修正】子オブジェクト（アウトライン用など）をすべて削除して見た目をシンプルにする
        foreach (Transform child in currentHintObject.transform)
        {
            Destroy(child.gameObject);
        }

        // スプライトの設定
        var sr = currentHintObject.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = currentInfo.sprite;

            // 【修正】目立ちすぎないようにアルファ値を制限
            // 初期状態: かなり薄く(0.1)
            Color baseColor = Color.white;
            baseColor.a = 0.1f;
            sr.color = baseColor;

            // アニメーション: 0.1(ほぼ見えない) ～ 0.4(薄く見える) の間でゆっくり点滅
            // 完全に不透明(1.0f)にはならないようにする
            hintTween = sr.DOFade(0.4f, 1.5f) // 時間も少しゆっくりに(1.5秒)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }

    private void StopHint()
    {
        if (hintTween != null)
        {
            hintTween.Kill();
            hintTween = null;
        }

        if (currentHintObject != null)
        {
            Destroy(currentHintObject);
            currentHintObject = null;
        }
    }
    // ---------------------------

    public void PickNextSprite()
    {
        Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (spriteInfos[nextPickIndex] != null)
        {
            currentSpriteObject = Instantiate(pickedSpritePrefab);
            currentSpriteObject.GetComponent<SpriteRenderer>().sprite = spriteInfos[nextPickIndex].sprite;
            currentTolerance = spriteInfos[nextPickIndex].overridedTolerance == 0 ? tolerance * cameraSizeRatio : spriteInfos[nextPickIndex].overridedTolerance;
            currentSpriteObject.transform.position = new Vector3(cursorPosition.x, cursorPosition.y, 0);
            // currentSpriteObject.transform.localScale = pickedSpritePrefab.transform.localScale / cameraSizeRatio;
            foreach (Transform child in currentSpriteObject.transform)
            {
                child.transform.localPosition = child.transform.localPosition * cameraSizeRatio;
            }

            Sequence sequence = DOTween.Sequence();
            foreach (Transform child in currentSpriteObject.transform)
            {
                Vector3 basePosition = child.transform.localPosition;
                child.transform.localPosition += new Vector3(0, spriteYOffset * cameraSizeRatio, 0);
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
            // --- 正解時にヒントを停止しタイマーをリセット ---
            StopHint();
            lastActionTime = Time.time;
            // ---------------------------------------------

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
        completeCircle.transform.DOScale(completeCircleScale * cameraSizeRatio, 0.75f);
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
            .Append(Camera.main.transform.DOMove(lastTargetPosition, 1.0f))
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