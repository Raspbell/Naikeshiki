using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

public class StageCameraManager : MonoBehaviour
{
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinemachinePositionComposer cinemachinePositionComposer;
    [SerializeField] private Volume volume;
    [SerializeField] private float bloomIntensity = 0.5f;
    [SerializeField] private Vector3 lastTargetPosition = new Vector3(0, -2, -10);

    private StageManager stageManager;

    void Start()
    {
        stageManager = FindFirstObjectByType<StageManager>();
    }

    void Update()
    {
        cinemachinePositionComposer.Composition.ScreenPosition.x = 0;
        if (stageManager.isPlaying)
        {
            Vector3 pos = Camera.main.transform.position;
            Camera.main.transform.position = new Vector3(0f, pos.y, pos.z);
        }
    }

    public void DisableCinemachine()
    {
        cinemachineBrain.enabled = false;
    }

    public Tween MoveCameraToTutorialPos()
    {
        return Camera.main.transform.DOMove(new Vector3(0f, -2f, -10f), 1.0f);
    }

    public Tween MoveCameraToLastPos()
    {
        return Camera.main.transform.DOMove(lastTargetPosition, 1.0f);
    }

    public void LaunchBloomEffect()
    {
        if (volume.profile.TryGet<Bloom>(out var bloom))
        {
            DOTween.To(() => bloom.intensity.value, x => bloom.intensity.value = x, bloomIntensity, 2.5f).SetDelay(1.2f);
        }
    }
}