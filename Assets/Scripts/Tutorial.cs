using DG.Tweening;
using UnityEngine;

public class Tutorial : MonoBehaviour
{
    [SerializeField] private GameObject guideClock;
    [SerializeField] private GameObject guideSapporo;
    [SerializeField] private CanvasGroup openingCanvasGroup;

    private Tween guideTween;
    private CursorManager cursorManager;
    private GuideType currentGuideType;
    private bool isPlaying = false;

    public enum GuideType
    {
        Clock,
        Sapporo
    }

    void LateUpdate()
    {
        if (Input.GetMouseButtonDown(0) && !isPlaying)
        {
            cursorManager = GetComponent<CursorManager>();
            isPlaying = true;
            guideClock.SetActive(true);
            openingCanvasGroup.DOFade(0, 1f).SetEase(Ease.InQuint).OnComplete(() =>
            {
                openingCanvasGroup.gameObject.SetActive(false);
            });
        }
    }


    public void StartGuide(GuideType guideType)
    {
        StopCurrentGuide(); // 現在のガイドを停止してから新しいガイドを開始
        switch (guideType)
        {
            case GuideType.Clock:
                guideClock.SetActive(true);
                guideTween = guideClock.GetComponent<SpriteRenderer>().DOFade(1f, 1f).SetLoops(-1, LoopType.Yoyo);
                break;
            case GuideType.Sapporo:
                guideSapporo.SetActive(true);
                guideTween = guideSapporo.GetComponent<SpriteRenderer>().DOFade(1f, 1f).SetLoops(-1, LoopType.Yoyo);
                break;
        }
    }

    public void StopCurrentGuide()
    {
        if (guideTween != null)
        {
            guideTween.Kill();
            guideTween = null;
        }

        guideClock.SetActive(false);
        guideSapporo.SetActive(false);
    }
}
