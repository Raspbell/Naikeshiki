using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class StageSelectText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private float fadeDuration = 0.3f;

    private void Update()
    {
        if (MapPinInfo.Zoomed || MapPinInfo.NowZooming)
        {
            if (targetText.color.a > 0f)
                targetText.DOFade(0f, fadeDuration);
        }
        else
        {
            if (targetText.color.a < 1f)
                targetText.DOFade(1f, fadeDuration);
        }
    }
}
