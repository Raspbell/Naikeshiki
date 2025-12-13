using TMPro;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using UnityEngine.UI;
using DG.Tweening;
using Shapes2D;

public class SoundSlider : MonoBehaviour
{
    [SerializeField] private AudioSource SESource;
    [SerializeField] private SoundType sound;

    private Slider BGMSlider;
    private Slider SESlider;

    private enum SoundType
    {
        BGM, SE
    }

    void Start()
    {
        if (sound == SoundType.SE)
        {
            SESlider = GetComponent<Slider>();
            SESlider.value = GameOptions.SEVolume.Value;
            SESource.volume = GameOptions.SEVolume.Value;

            SESlider.OnPointerUpAsObservable()
            .Subscribe(_ =>
            {
                GameOptions.SEVolume.Value = SESlider.value;
                SESource.volume = GameOptions.SEVolume.Value;
                SESource.PlayOneShot(SESource.clip);
                PlayerPrefs.SetFloat("SEVolume", SESlider.value);
                PlayerPrefs.Save();
            })
            .AddTo(this);
        }

        else if (sound == SoundType.BGM)
        {
            BGMSlider = GetComponent<Slider>();
            BGMSlider.value = GameOptions.BGMVolume.Value;

            BGMSlider.OnPointerUpAsObservable()
            .Subscribe(_ =>
            {
                PlayerPrefs.SetFloat("BGMVolume", BGMSlider.value);
                PlayerPrefs.Save();
            })
            .AddTo(this);
        }
    }

    public void OnValueChanged()
    {
        if (sound == SoundType.BGM)
        {
            GameOptions.BGMVolume.Value = BGMSlider.value;
        }
    }
}
