using UnityEngine;
using System;
using UniRx;
using DG.Tweening;

public class StageEffectManager : MonoBehaviour
{
    [SerializeField] private GameObject completeCirclePrefab;
    [SerializeField] private GameObject unpickEffectPrefab;
    [SerializeField] private AudioSource openingJingleAudioSource;
    [SerializeField] private SoundEffectInfo soundEffectInfo;
    [SerializeField] private float completeCircleScale = 2f;

    private GameObject instantiatedCompleteCircle;
    private ParticleSystem instantiatedUnpickEffect;
    private AudioSource audioSource;

    [Serializable]
    public class SoundEffectInfo
    {
        public AudioClip unpickClip;
        public AudioClip completeClip;
    }

    public void Initialize(float cameraSizeRatio)
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = GameOptions.SEVolume.Value;
        GameOptions.SEVolume.Subscribe(v => audioSource.volume = v).AddTo(this);

        instantiatedCompleteCircle = Instantiate(completeCirclePrefab);
        instantiatedCompleteCircle.transform.localScale = Vector3.zero;

        GameObject effectObj = Instantiate(unpickEffectPrefab);
        instantiatedUnpickEffect = effectObj.GetComponent<ParticleSystem>();
        instantiatedUnpickEffect.transform.localScale *= cameraSizeRatio;
    }

    public void PlayUnpickAudio()
    {
        audioSource.PlayOneShot(soundEffectInfo.unpickClip);
    }

    public void PlayUnpickEffect(Vector3 position)
    {
        instantiatedUnpickEffect.transform.position = position;
        instantiatedUnpickEffect.Play();
    }

    public void PlayCompleteEffect(float cameraSizeRatio, bool isLastStage)
    {
        Vector3 center = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        center.z = -10;
        instantiatedCompleteCircle.transform.position = new Vector3(center.x, center.y, 0);
        instantiatedCompleteCircle.transform.DOScale(completeCircleScale * cameraSizeRatio, 0.75f);
        if (!isLastStage) audioSource.PlayOneShot(soundEffectInfo.completeClip);
    }

    public void PlayOpeningJingle()
    {
        openingJingleAudioSource.Play();
    }
}