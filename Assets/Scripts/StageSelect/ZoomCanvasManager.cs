using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class ZoomCanvasManager : MonoBehaviour
{
    [SerializeField] CanvasGroup stageCanvasGroup;
    [SerializeField] CanvasGroup optionCanvasGroup;
    [SerializeField] float duration = 0.5f;
    [SerializeField] TextMeshProUGUI stageNameText;

    private DifficulitySignManager difficulitySignManager;

    private void Start()
    {
        difficulitySignManager = FindFirstObjectByType<DifficulitySignManager>();
    }

    public void ShowStageCanvas(StageInfo nextStageInfo)
    {
        stageCanvasGroup.DOKill();
        stageCanvasGroup.gameObject.SetActive(true);
        stageNameText.text = nextStageInfo.stageName;
        difficulitySignManager.UpdateDifficulityDisplay(nextStageInfo.difficulty);
        stageCanvasGroup.DOFade(1f, duration);
    }

    public void ShowOptionCanvas(StageInfo nextStageInfo)
    {
        optionCanvasGroup.DOKill();
        optionCanvasGroup.gameObject.SetActive(true);
        optionCanvasGroup.DOFade(1f, duration);
    }

    public void DismissStageCanvas()
    {
        stageCanvasGroup.DOKill();
        stageCanvasGroup.DOFade(0f, duration).OnComplete(() =>
            stageCanvasGroup.gameObject.SetActive(false)
        );
    }

    public void DismissOptionCanvas()
    {
        optionCanvasGroup.DOKill();
        optionCanvasGroup.DOFade(0f, duration).OnComplete(() =>
            optionCanvasGroup.gameObject.SetActive(false)
        );
    }
}
