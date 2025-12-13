using UnityEngine;
using UnityEngine.UI;

public class DifficulitySignManager : MonoBehaviour
{
    [SerializeField] Color easyColor;
    [SerializeField] Color normalColor;
    [SerializeField] Color hardColor;
    [SerializeField] Color extraColor;
    [SerializeField] GameObject[] difficulitySignes;

    public void UpdateDifficulityDisplay(StageInfo.Difficulty difficulty)
    {
        foreach (var sign in difficulitySignes)
        {
            sign.SetActive(false);
        }

        int maxSignIndex = -1;
        Color signColor = Color.clear;

        switch (difficulty)
        {
            case StageInfo.Difficulty.Easy:
                maxSignIndex = 0;
                signColor = easyColor;
                break;
            case StageInfo.Difficulty.Normal:
                maxSignIndex = 1;
                signColor = normalColor;
                break;
            case StageInfo.Difficulty.Hard:
                maxSignIndex = 2;
                signColor = hardColor;
                break;
            case StageInfo.Difficulty.Extra:
                maxSignIndex = 3;
                signColor = extraColor;
                break;
            default:
                break;
        }

        for (int i = 0; i <= maxSignIndex; i++)
        {
            difficulitySignes[i].SetActive(true);
            difficulitySignes[i].GetComponent<Image>().color = signColor;
        }
    }
}
