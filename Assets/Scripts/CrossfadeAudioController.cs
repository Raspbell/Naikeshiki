using UnityEngine;
using UnityEngine.SceneManagement;
using UniRx;

[RequireComponent(typeof(AudioSource))]
public class CrossfadeAudioController : MonoBehaviour
{
    [Header("音声クリップ設定")]
    [SerializeField] private AudioClip[] audioClips;

    [Header("クロスフェードにかける時間(秒)")]
    [SerializeField] private float fadeTime = 3f;

    [Header("音量(最大音量)")]
    [SerializeField] private float volume = 1f;

    [Header("再生開始後にシーン移行するか")]
    [SerializeField] private bool translateAfterBGMStarted = true;

    [Header("移行先シーン名")]
    [SerializeField] private string sceneName = "Title";

    public float Volume
    {
        get { return volume; }
        set
        {
            volume = Mathf.Max(value, 0f);
            RecalculateVolumes();
        }
    }

    private enum FadeMode
    {
        None,
        FadeIn,
        CrossFade,
        FadeOut
    }


    private AudioSource audioSourceA;
    private AudioSource audioSourceB;

    private bool isPlayingA = true;

    private int currentClipIndex = 0;

    private FadeMode fadeMode = FadeMode.None;
    private float fadeElapsed = 0f;
    private float startVolumeA = 0f;
    private float startVolumeB = 0f;
    private AudioSource fadeOutSource;
    private AudioSource fadeInSource;

    private bool autoCrossFadeNearEnd = true;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length < 2)
        {
            Debug.LogError("AudioSourceが2つ以上アタッチされている必要があります。");
            return;
        }
        audioSourceA = sources[0];
        audioSourceB = sources[1];
        audioSourceA.volume = 0f;
        audioSourceB.volume = 0f;

        if (audioClips != null && audioClips.Length > 0)
        {
            audioSourceA.clip = audioClips[0];
            audioSourceA.time = 0f;
            audioSourceA.Play();

            BeginFadeIn(audioSourceA);
        }
        else
        {
            Debug.LogWarning("audioClips が空です。最初は無音で開始します。");
        }

        if (translateAfterBGMStarted)
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    void Start()
    {
        GameOptions.BGMVolume.Subscribe(vol =>
        {
            Volume = vol;
            RecalculateVolumes();
        }).AddTo(this);
    }


    void Update()
    {
        if (fadeMode != FadeMode.None)
        {
            fadeElapsed += Time.deltaTime;
            RecalculateVolumes();

            if (fadeElapsed >= fadeTime)
            {
                EndFade();
            }
        }
        else
        {
            if (autoCrossFadeNearEnd)
            {
                AudioSource activeSource = isPlayingA ? audioSourceA : audioSourceB;
                if (activeSource.clip != null)
                {
                    float remain = activeSource.clip.length - activeSource.time;
                    if (remain <= fadeTime)
                    {
                        BeginCrossFadeSameClip();
                    }
                }
            }
            RecalculateVolumes();
        }
    }

    public void ChangeClip(int nextIndex)
    {
        if (0 <= nextIndex && nextIndex < audioClips.Length)
        {
            BeginCrossFadeToClip(nextIndex);
        }
        else
        {
            BeginFadeOutCurrent();
        }
    }

    public void ChangeClip(AudioClip clip)
    {
        if (clip != null)
        {
            BeginCrossFadeToClip(clip);
        }
        else
        {
            BeginFadeOutCurrent();
        }
    }

    private void BeginFadeIn(AudioSource inSource)
    {
        fadeMode = FadeMode.FadeIn;
        fadeElapsed = 0f;
        fadeOutSource = null;
        fadeInSource = inSource;
        startVolumeB = inSource.volume;
    }

    private void BeginCrossFadeSameClip()
    {
        AudioSource current = isPlayingA ? audioSourceA : audioSourceB;
        AudioSource other = isPlayingA ? audioSourceB : audioSourceA;

        other.clip = current.clip;
        other.time = 0f;
        other.volume = 0f;
        other.Play();

        fadeMode = FadeMode.CrossFade;
        fadeElapsed = 0f;

        fadeOutSource = current;
        fadeInSource = other;

        startVolumeA = current.volume;
        startVolumeB = other.volume;
    }

    private void BeginCrossFadeToClip(int nextIndex)
    {
        AudioSource current = isPlayingA ? audioSourceA : audioSourceB;
        AudioSource other = isPlayingA ? audioSourceB : audioSourceA;

        currentClipIndex = nextIndex;
        other.clip = audioClips[currentClipIndex];
        other.time = 0f;
        other.volume = 0f;
        other.Play();

        fadeMode = FadeMode.CrossFade;
        fadeElapsed = 0f;

        fadeOutSource = current;
        fadeInSource = other;

        startVolumeA = current.volume;
        startVolumeB = other.volume;
    }

    private void BeginCrossFadeToClip(AudioClip clip)
    {
        AudioSource current = isPlayingA ? audioSourceA : audioSourceB;
        AudioSource other = isPlayingA ? audioSourceB : audioSourceA;

        other.clip = clip;
        other.time = 0f;
        other.volume = 0f;
        other.Play();

        fadeMode = FadeMode.CrossFade;
        fadeElapsed = 0f;

        fadeOutSource = current;
        fadeInSource = other;

        startVolumeA = current.volume;
        startVolumeB = other.volume;
    }

    private void BeginFadeOutCurrent()
    {
        AudioSource current = isPlayingA ? audioSourceA : audioSourceB;

        fadeMode = FadeMode.FadeOut;
        fadeElapsed = 0f;

        fadeOutSource = current;
        fadeInSource = null;

        startVolumeA = current.volume;
        startVolumeB = 0f;
    }

    private void EndFade()
    {
        RecalculateVolumes();

        switch (fadeMode)
        {
            case FadeMode.FadeIn:
                if (fadeInSource != null)
                {
                    fadeInSource.volume = Volume;
                }
                break;

            case FadeMode.CrossFade:
                if (fadeOutSource != null)
                {
                    fadeOutSource.Stop();
                    fadeOutSource.volume = 0f;
                }
                if (fadeInSource != null)
                {
                    fadeInSource.volume = Volume;
                }
                isPlayingA = !isPlayingA;
                break;

            case FadeMode.FadeOut:
                if (fadeOutSource != null)
                {
                    fadeOutSource.Stop();
                    fadeOutSource.volume = 0f;
                }
                break;
        }

        fadeMode = FadeMode.None;
        fadeElapsed = 0f;
        fadeOutSource = null;
        fadeInSource = null;

        RecalculateVolumes();
    }

    private void RecalculateVolumes()
    {
        float t = (fadeTime > 0f) ? Mathf.Clamp01(fadeElapsed / fadeTime) : 1f;

        switch (fadeMode)
        {
            case FadeMode.None:
                {
                    AudioSource main = isPlayingA ? audioSourceA : audioSourceB;
                    AudioSource sub = isPlayingA ? audioSourceB : audioSourceA;

                    if (main.isPlaying)
                    {
                        main.volume = Volume;
                    }
                    else
                    {
                        main.volume = 0f;
                    }

                    if (sub.isPlaying)
                    {
                        sub.volume = 0f;
                    }
                    else
                    {
                        sub.volume = 0f;
                    }
                }
                break;

            case FadeMode.FadeIn:
                if (fadeInSource != null)
                {
                    fadeInSource.volume = Mathf.Lerp(startVolumeB, Volume, t);
                }
                break;

            case FadeMode.CrossFade:
                if (fadeOutSource != null)
                {
                    fadeOutSource.volume = Mathf.Lerp(startVolumeA, 0f, t);
                }
                if (fadeInSource != null)
                {
                    fadeInSource.volume = Mathf.Lerp(startVolumeB, Volume, t);
                }
                break;

            case FadeMode.FadeOut:
                if (fadeOutSource != null)
                {
                    fadeOutSource.volume = Mathf.Lerp(startVolumeA, 0f, t);
                }
                break;
        }
    }
}
