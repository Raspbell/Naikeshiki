using UnityEngine;
using Cysharp.Threading.Tasks;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] private StageInfo dummy;

    public async void OnClickBackButton()
    {
        if (ClickZoomCamera.NowZooming || !ClickZoomCamera.Zoomed)
        {
            return;
        }

        CameraManager cameraController = FindFirstObjectByType<CameraManager>();
        ZoomCanvasManager zoomCanvasManager = FindFirstObjectByType<ZoomCanvasManager>();

        // cameraControllerとzoomCanvasManagerのnullチェック
        if (cameraController != null && zoomCanvasManager != null)
        {
            if (ClickZoomCamera.prevStageInfo != null)
            {
                if (ClickZoomCamera.prevStageInfo.name == "Option")
                {
                    zoomCanvasManager.DismissOptionCanvas();
                }
                else
                {
                    zoomCanvasManager.DismissStageCanvas();
                }
            }
            // (もしもの場合) prevStageInfoがnullでも、両方閉じる要求を出す
            // DOKill()のおかげで、表示されていない方に実行してもエラーにはならない
            else
            {
                zoomCanvasManager.DismissOptionCanvas();
                zoomCanvasManager.DismissStageCanvas();
            }

            await cameraController.ReturnToInitialPosition();

            // ズームアウト完了後にprevStageInfoをリセット
            ClickZoomCamera.prevStageInfo = null;
        }
    }

    public void OnClickStartButton()
    {
        // OnClickStartButtonはStageCanvasからのみ呼ばれる想定
        // (OptionCanvasから呼ばれる場合は、このロジックも見直しが必要)

        if (ClickZoomCamera.prevStageInfo == dummy || ClickZoomCamera.prevStageInfo == null)
        {
            return;
        }

        // Optionが選ばれている場合はスタートさせない
        if (ClickZoomCamera.prevStageInfo.name == "Option")
        {
            return;
        }

        GameObject transitionObject = GameObject.FindGameObjectWithTag("SceneTransition_In");
        if (transitionObject != null)
        {
            GameOptions.InitFields(ClickZoomCamera.prevStageInfo);
            FindFirstObjectByType<CrossfadeAudioController>().ChangeClip(ClickZoomCamera.prevStageInfo.sceneNameScriptableObject.sceneInfos[0].BGM);
            StartCoroutine(transitionObject.GetComponent<SceneTransition>().StartSceneTransition(ClickZoomCamera.prevStageInfo.sceneNameScriptableObject.sceneInfos[0].SceneName));
        }
    }
}