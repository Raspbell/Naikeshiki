using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class CheckboxController : MonoBehaviour
{
    [SerializeField] private float checkAnimDuration = 0.2f;
    [SerializeField] private float uncheckAnimDuration = 0.2f;

    [SerializeField] private RectTransform uncheckedSprite;
    [SerializeField] private RectTransform checkedSprite;

    [SerializeField] private Options option;

    private enum Options
    {
        AutoSkip, HelpMode
    }

    private Image checkedImage;
    private Image uncheckedImage;

    private Tween checkboxTween;

    private void Start()
    {
        checkedImage = checkedSprite.GetComponent<Image>();
        uncheckedImage = uncheckedSprite.GetComponent<Image>();

        // 初期状態の反映
        switch (option)
        {
            case Options.AutoSkip:
                if (GameOptions.AutoChangeScene.Value)
                {
                    checkedSprite.gameObject.SetActive(true);
                    uncheckedSprite.gameObject.SetActive(false);
                }
                else
                {
                    checkedSprite.gameObject.SetActive(false);
                    uncheckedSprite.gameObject.SetActive(true);
                }
                break;

            case Options.HelpMode:
                if (GameOptions.UseHelpMode.Value)
                {
                    checkedSprite.gameObject.SetActive(true);
                    uncheckedSprite.gameObject.SetActive(false);
                }
                else
                {
                    checkedSprite.gameObject.SetActive(false);
                    uncheckedSprite.gameObject.SetActive(true);
                }
                break;
        }
    }

    public void OnPointerClick()
    {
        switch (option)
        {
            case Options.AutoSkip:
                if (GameOptions.AutoChangeScene.Value)
                {
                    PlayUncheckAnimation();
                }
                else
                {
                    PlayCheckAnimation();
                }
                GameOptions.AutoChangeScene.Value = !GameOptions.AutoChangeScene.Value;
                PlayerPrefs.SetInt("AutoSkip", GameOptions.AutoChangeScene.Value ? 1 : 0);
                PlayerPrefs.Save();
                break;

            case Options.HelpMode:
                if (GameOptions.UseHelpMode.Value)
                {
                    PlayUncheckAnimation();
                }
                else
                {
                    PlayCheckAnimation();
                }
                GameOptions.UseHelpMode.Value = !GameOptions.UseHelpMode.Value;
                PlayerPrefs.SetInt("UseHelpMode", GameOptions.UseHelpMode.Value ? 1 : 0);
                PlayerPrefs.Save();
                break;
        }
    }

    public void OnPointerDown()
    {

    }

    private void PlayCheckAnimation()
    {
        if (checkboxTween != null)
        {
            checkboxTween.Kill();
        }

        Color color = checkedImage.color;
        color.a = 1f;
        checkedImage.color = color;
        checkedSprite.localScale = Vector3.one * 1.2f;

        checkedSprite.gameObject.SetActive(true);
        uncheckedSprite.gameObject.SetActive(true);

        checkboxTween = checkedSprite.DOScale(Vector3.one, checkAnimDuration)
        .OnComplete(() =>
        {
            uncheckedSprite.gameObject.SetActive(false);
        });
    }

    private void PlayUncheckAnimation()
    {
        if (checkboxTween != null)
        {
            checkboxTween.Kill();
        }

        Color color = checkedImage.color;
        color.a = 1f;
        checkedImage.color = color;
        checkedSprite.localScale = Vector3.one;

        checkedSprite.gameObject.SetActive(true);
        uncheckedSprite.gameObject.SetActive(true);

        checkboxTween = checkedImage.DOFade(0f, uncheckAnimDuration)
        .OnComplete(() =>
        {
            checkedSprite.gameObject.SetActive(false);
            color = checkedImage.color;
            color.a = 1f;
            checkedImage.color = color;
        });
    }
}
