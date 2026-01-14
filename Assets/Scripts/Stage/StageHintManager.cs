using UnityEngine;
using DG.Tweening;

public class StageHintManager : MonoBehaviour
{
    [SerializeField] private GameObject pickedSpritePrefab;
    private float lastActionTime;
    private GameObject currentHintObject;
    private Tween hintTween;

    public void ResetTimer()
    {
        lastActionTime = Time.time;
    }

    public void CheckAndShowHint(int nextPickIndex, StageManager.SpriteInfo[] spriteInfos)
    {
        if (currentHintObject != null)
        {
            return;
        }

        if (Time.time - lastActionTime > GameOptions.TimeForHint.Value)
        {
            ShowHint(nextPickIndex, spriteInfos);
        }
    }

    private void ShowHint(int nextPickIndex, StageManager.SpriteInfo[] spriteInfos)
    {
        if (nextPickIndex >= spriteInfos.Length) return;
        var info = spriteInfos[nextPickIndex];
        Transform target = info.targetOrigin != null ? info.targetOrigin.transform : info.targetSpriteMask.transform;

        currentHintObject = Instantiate(pickedSpritePrefab, target.position, pickedSpritePrefab.transform.rotation);
        foreach (Transform child in currentHintObject.transform) Destroy(child.gameObject);

        var spriteRenderer = currentHintObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = info.sprite;
            spriteRenderer.color = new Color(1, 1, 1, 0.1f);
            hintTween = spriteRenderer.DOFade(0.4f, 1.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }

    public void StopHint()
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
}