
using UnityEngine;
using DG.Tweening;
using UniRx;

public class MapPinBound : MonoBehaviour
{
    [SerializeField] private float boundDistance = 0.1f;
    [SerializeField] private float boundDuration = 1f;
    [SerializeField] private bool randomStart = false;

    [SerializeField] private ReactiveProperty<bool> isBouncing = new ReactiveProperty<bool>(false);
    public bool IsBouncing
    {
        get => isBouncing.Value;
        private set => isBouncing.Value = value;
    }

    private Tween bounceTween;
    private float bounceProgress = 0f; // 0~1
    private float baseY;

    void Start()
    {
        baseY = transform.localPosition.y;
        if (randomStart)
        {
            bounceProgress = Random.Range(0f, 1f);
        }

        isBouncing
            .DistinctUntilChanged()
            .Subscribe(OnBounceStateChanged)
            .AddTo(this);

        if (IsBouncing)
        {
            StartBounce(bounceProgress);
        }
        else
        {
            StopBounce();
        }
    }

    void Update()
    {
        // バウンド中は進捗を保存
        if (bounceTween != null && bounceTween.IsActive())
        {
            if (bounceTween.IsPlaying())
            {
                bounceProgress = bounceTween.ElapsedPercentage() % 1f;
            }
        }

        // Y軸だけカメラ方向に向く
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 camPos = cam.transform.position;
            Vector3 lookDir = camPos - transform.position;
            lookDir.y = 0f; // Y軸のみ回転
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(-lookDir, Vector3.up);
                Vector3 euler = transform.localEulerAngles;
                euler.y = targetRot.eulerAngles.y + 90f;
                transform.localEulerAngles = euler;
            }
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            if (IsBouncing)
            {
                SetBouncing(false);
            }
            else
            {
                SetBouncing(true);
            }
        }
    }

    private void OnBounceStateChanged(bool bouncing)
    {
        if (bouncing)
        {
            StartBounce(bounceProgress);
        }
        else
        {
            StopBounce();
        }
    }

    private void StartBounce(float progress)
    {
        bounceTween?.Kill();
        bounceTween = transform.DOLocalMoveY(baseY + boundDistance, boundDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .OnUpdate(() =>
            {
                bounceProgress = bounceTween.ElapsedPercentage() % 1f;
            });
        bounceTween.Goto(progress * boundDuration, true);
    }

    private void StopBounce()
    {
        bounceTween?.Kill();
        bounceTween = transform.DOLocalMoveY(baseY, boundDuration * (1f - bounceProgress))
            .SetLoops(1, LoopType.Restart)
            .OnComplete(() =>
            {
                bounceTween = null;
                bounceProgress = 0f;
            });
    }

    public void SetBouncing(bool enable)
    {
        IsBouncing = enable;
    }
}
