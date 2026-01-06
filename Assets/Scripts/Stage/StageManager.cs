using UnityEngine;
using System.Collections;
using System;
using DG.Tweening;
using UniRx;
using UnityEditor.SceneManagement;

public class StageManager : MonoBehaviour
{
    [SerializeField] private float tolerance;
    [SerializeField] private float ver1CameraSize = 13f;
    [SerializeField] private float spriteYOffset = 0.5f;
    [SerializeField] private float effectWaitingDuration = 0.2f;
    [SerializeField] private Color outlineColor;
    [SerializeField] private GameObject cursor;
    [SerializeField] private GameObject pickedSpritePrefab;
    [SerializeField] private SceneTransition sceneTransition;
    [SerializeField] private SpriteInfo[] spriteInfos;
    public bool isTutorial;
    public bool isLastStage;
    [SerializeField] private bool isGrowing;

    private StageEffectManager effectManager;
    private StageUIManager uiManager;
    private StageHintManager hintManager;
    private StageCameraManager cameraManager;

    [Serializable]
    public class SpriteInfo
    {
        public Sprite sprite;
        public GameObject targetSpriteMask;
        public GameObject targetOrigin;
        public Color overridedOutlineColor = Color.clear;
        public float overridedTolerance = -1;
    }

    public GameObject currentSpriteObject { get; private set; }
    private int nextPickIndex = 0;
    private float currentTolerance;
    public bool isPlaying { get; private set; } = true;
    private int passedFlame = 0;
    private bool waitingAfterComplete = false;
    private float cameraSizeRatio = 1f;
    private CrossfadeAudioController crossfadeAudioController;
    private Tutorial tutorial;

    void Start()
    {
        crossfadeAudioController = FindFirstObjectByType<CrossfadeAudioController>();
        effectManager = FindFirstObjectByType<StageEffectManager>();
        uiManager = FindFirstObjectByType<StageUIManager>();
        hintManager = FindFirstObjectByType<StageHintManager>();
        cameraManager = FindFirstObjectByType<StageCameraManager>();
        cameraSizeRatio = Camera.main.orthographicSize / ver1CameraSize;

        foreach (SpriteInfo pickedSprite in spriteInfos)
        {
            pickedSprite.targetSpriteMask.SetActive(false);
        }

        if (isTutorial)
        {
            tutorial = GetComponent<Tutorial>();
            tutorial.StartGuide(Tutorial.GuideType.Clock);
        }

        hintManager.Initialize();
        effectManager.Initialize(cameraSizeRatio);
    }

    void Update()
    {
        if (passedFlame == 1)
        {
            PickNextSprite();
        }

        Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (isPlaying)
        {
            cursor.transform.position = new Vector3(cursorPosition.x, cursorPosition.y, 0);

            if (!isTutorial && !isLastStage && GameOptions.UseHelpMode.Value)
            {
                hintManager.CheckAndShowHint(nextPickIndex, spriteInfos);
            }
        }

        if (Input.GetKeyDown(KeyCode.UpArrow)) GameOptions.BGMVolume.Value += 0.05f;
        if (Input.GetKeyDown(KeyCode.DownArrow)) GameOptions.BGMVolume.Value -= 0.05f;

        if (currentSpriteObject != null)
        {
            currentSpriteObject.transform.position = new Vector3(cursorPosition.x, cursorPosition.y, 0);
            if (Input.GetMouseButtonDown(0))
            {
                HandleSelection(cursorPosition);
            }
        }

        if (waitingAfterComplete && Input.GetMouseButtonDown(0))
        {
            HandlePostCompleteInput();
        }

        passedFlame++;
    }

    private void HandleSelection(Vector3 cursorPosition)
    {
        float distance = 100000000;
        var info = spriteInfos[nextPickIndex];
        if (info.targetOrigin == null)
        {
            distance = Vector3.Distance(currentSpriteObject.transform.position, info.targetSpriteMask.transform.position);
        }
        else
        {
            distance = Vector3.Distance(currentSpriteObject.transform.position, info.targetOrigin.transform.position);
        }

        if (distance <= currentTolerance)
        {
            info.targetSpriteMask.SetActive(true);
            if (nextPickIndex < spriteInfos.Length - 1)
            {
                UnpickSprite(() => PickNextSprite(), false);
            }
            else
            {
                UnpickSprite(() =>
                {
                    isPlaying = false;
                    FinalizeStage();
                }, true);
            }
        }
    }

    public void PickNextSprite()
    {
        Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (nextPickIndex < spriteInfos.Length)
        {
            var info = spriteInfos[nextPickIndex];
            currentSpriteObject = Instantiate(pickedSpritePrefab);
            currentSpriteObject.GetComponent<SpriteRenderer>().sprite = info.sprite;
            currentTolerance = info.overridedTolerance == 0 ? tolerance * cameraSizeRatio : info.overridedTolerance;
            currentSpriteObject.transform.position = new Vector3(cursorPosition.x, cursorPosition.y, 0);

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
            UpdateOutlineSprite(info.sprite, info.overridedOutlineColor == Color.clear ? outlineColor : info.overridedOutlineColor);
        }
    }

    void UnpickSprite(Action onComplete, bool isLast)
    {
        if (currentSpriteObject != null)
        {
            hintManager.StopHint();
            hintManager.ResetTimer();
            effectManager.PlayUnpickAudio();
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
        SpriteRenderer[] srs = currentSpriteObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in srs)
        {
            if (sr.gameObject != currentSpriteObject) sr.sprite = sprite;
        }
    }

    IEnumerator PlayEffectAndWait(Action onComplete, bool isLast)
    {
        var info = spriteInfos[nextPickIndex - 1];
        Vector3 pos = info.targetOrigin == null ? info.targetSpriteMask.transform.position : info.targetOrigin.transform.position;
        effectManager.PlayUnpickEffect(pos);
        yield return new WaitForSeconds(isLast ? 0 : effectWaitingDuration);
        onComplete?.Invoke();
    }

    void FinalizeStage()
    {
        if (isTutorial)
        {
            TurtorialStageEffect();
        }
        else if (isLastStage)
        {
            LastStageEffect();
        }
        else
        {
            if (GameOptions.AutoChangeScene.Value)
            {
                MigrateToNextScene();
            }
            else
            {
                effectManager.PlayCompleteEffect(cameraSizeRatio, false);
                GameOptions.CurrentSceneIndex++;
                uiManager.FadeInClickToNext().OnComplete(() => waitingAfterComplete = true);
            }
        }
    }

    void MigrateToNextScene()
    {
        effectManager.PlayCompleteEffect(cameraSizeRatio, false);
        crossfadeAudioController.ChangeClip(GameOptions.SceneInfos[GameOptions.CurrentSceneIndex].BGM);
        GameOptions.CurrentSceneIndex++;
        StartCoroutine(sceneTransition.StartSceneTransition(GameOptions.SceneInfos[GameOptions.CurrentSceneIndex].SceneName, 0f));
    }

    void TurtorialStageEffect()
    {
        cameraManager.DisableCinemachine();
        effectManager.PlayCompleteEffect(cameraSizeRatio, false);

        Sequence sequence = DOTween.Sequence();
        sequence.SetDelay(0.5f)
            .Append(cameraManager.MoveCameraToTutorialPos())
            .Append(uiManager.FadeInTitle())
            .InsertCallback(2.5f, () => effectManager.PlayOpeningJingle())
            .AppendInterval(3.5f)
            .Append(uiManager.FadeInClickToNext())
            .OnComplete(() => waitingAfterComplete = true);
    }

    void LastStageEffect()
    {
        cameraManager.DisableCinemachine();
        effectManager.PlayCompleteEffect(cameraSizeRatio, true);
        crossfadeAudioController.ChangeClip(GameOptions.SceneInfos[GameOptions.CurrentSceneIndex + 1].BGM);

        Sequence sequence = DOTween.Sequence();
        sequence.SetDelay(1.0f)
            .Append(cameraManager.MoveCameraToLastPos())
            .AppendCallback(() => { })
            .Append(uiManager.FadeInLastStageCanvas())
            .Append(uiManager.FadeInClickToNext())
            .OnComplete(() => waitingAfterComplete = true);

        if (isGrowing) cameraManager.LaunchBloomEffect();
    }

    void HandlePostCompleteInput()
    {
        waitingAfterComplete = false;
        if (isTutorial || isLastStage)
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