using TMPro;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using UnityEngine.UI;
using DG.Tweening;
using Shapes2D;

public class HintTimeSlider : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI hintTimeText;
    [SerializeField] private float alphaWhenHintDisabled = 0.3f;
    [SerializeField] private float timeForFade = 0.2f;
    [SerializeField] private float[] hintTimes = new float[6];
    [SerializeField] private float hintTextOffsetY = 30f;

    private Slider hintTimeSlider;

    void Start()
    {
        hintTimeSlider = GetComponent<Slider>();

        CanvasGroup hintTimeSliderGroup = GetComponent<CanvasGroup>();
        if (GameOptions.UseHelpMode.Value == false)
        {
            hintTimeSliderGroup.alpha = alphaWhenHintDisabled;
        }
        else
        {
            hintTimeSliderGroup.alpha = 1f;
        }

        hintTimeSlider.interactable = GameOptions.UseHelpMode.Value;
        hintTimeSlider.maxValue = hintTimes.Length - 1;

        int nearestIndex = 0;
        if (hintTimes != null && hintTimes.Length > 0)
        {
            float savedValue = PlayerPrefs.GetFloat("TimeForHint", -1f);

            if (savedValue >= 0 && savedValue < hintTimes.Length)
            {
                nearestIndex = (int)savedValue;
            }
            else
            {
                float bestDiff = Mathf.Abs(GameOptions.TimeForHint.Value - hintTimes[0]);
                for (int i = 1; i < hintTimes.Length; i++)
                {
                    float diff = Mathf.Abs(GameOptions.TimeForHint.Value - hintTimes[i]);
                    if (diff < bestDiff)
                    {
                        bestDiff = diff;
                        nearestIndex = i;
                    }
                }
            }

            hintTimeSlider.value = nearestIndex;
        }

        hintTimeText.text = GenerateHintText();
        MoveHintText();

        GameOptions.UseHelpMode.Subscribe(isOn =>
        {
            if (isOn)
            {
                hintTimeSliderGroup.DOFade(1f, timeForFade);
                hintTimeSlider.interactable = true;
                string hintText = GenerateHintText();
                hintTimeText.text = hintText;
                MoveHintText();
            }
            else
            {
                hintTimeSliderGroup.DOFade(alphaWhenHintDisabled, timeForFade);
                hintTimeSlider.interactable = false;
            }
        })
        .AddTo(this);

        hintTimeSlider.OnPointerUpAsObservable()
            .Subscribe(_ =>
            {
                PlayerPrefs.SetFloat("TimeForHint", hintTimeSlider.value);
                PlayerPrefs.Save();
            })
            .AddTo(this);
    }

    public void OnValueChanged()
    {
        string hintText = GenerateHintText();
        hintTimeText.text = hintText;
        MoveHintText();
    }

    private string GenerateHintText()
    {
        int index = (int)hintTimeSlider.value;
        string hintText = "";
        if (index >= 0 && index < hintTimes.Length)
        {
            float timeForHint = hintTimes[index];
            if (timeForHint < 0)
            {
                timeForHint = 10;
            }
            GameOptions.TimeForHint.Value = timeForHint;

            if (timeForHint >= 60)
            {
                hintText += (int)(timeForHint / 60) + "分";
            }
            if (timeForHint % 60 != 0)
            {
                hintText += (int)(timeForHint % 60) + "秒";
            }
        }
        return hintText;
    }

    private void MoveHintText()
    {
        RectTransform handleRect = hintTimeSlider.handleRect;
        hintTimeText.rectTransform.position = handleRect.position + new Vector3(0, hintTextOffsetY, 0);
    }
}
