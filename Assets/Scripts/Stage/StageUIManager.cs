using UnityEngine;
using DG.Tweening;

public class StageUIManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup titleCanvasGroup;
    [SerializeField] private CanvasGroup lastStageCanvasGroup;
    [SerializeField] private CanvasGroup arrowCanvasGroup;
    [SerializeField] private CanvasGroup clickToNextCanvas;
    [SerializeField] private bool allowArrowIcon;

    private float previousCameraY = 0;
    private float cameraStopTimeCounter = 0;
    private Tween arrowTween;

    private StageManager stageManager;

    void Start()
    {
        stageManager = FindFirstObjectByType<StageManager>();
    }

    void Update()
    {
        if (!stageManager.isPlaying) return;

        if (!stageManager.isLastStage && allowArrowIcon)
        {
            if (Camera.main.transform.position.y == previousCameraY)
            {
                cameraStopTimeCounter += Time.deltaTime;
                if (cameraStopTimeCounter >= 7.0f && arrowTween == null)
                {
                    arrowTween = arrowCanvasGroup.DOFade(0.7f, 1.0f).SetEase(Ease.InQuint).OnComplete(() => arrowTween = null);
                }
            }
            else
            {
                cameraStopTimeCounter = 0;
                if (arrowTween == null)
                {
                    arrowTween = arrowCanvasGroup.DOFade(0.0f, 1.0f).SetEase(Ease.InQuint).OnComplete(() => arrowTween = null);
                }
            }
            previousCameraY = Camera.main.transform.position.y;
        }
    }

    public Tween FadeInTitle()
    {
        return titleCanvasGroup.DOFade(1.0f, 2.0f).SetEase(Ease.InQuint);
    }

    public Tween FadeInLastStageCanvas()
    {
        CanvasGroup target = lastStageCanvasGroup != null ? lastStageCanvasGroup : titleCanvasGroup;
        return target.DOFade(1.0f, 2.0f).SetEase(Ease.InQuint);
    }

    public Tween FadeInClickToNext()
    {
        return clickToNextCanvas.DOFade(1.0f, 1.0f).SetEase(Ease.InQuint);
    }
}