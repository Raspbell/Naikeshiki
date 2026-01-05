using UnityEngine;
using Cysharp.Threading.Tasks;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] private StageInfo dummy;

    public async void OnClickBackButton()
    {
        if (MapPinInfo.NowZooming || !MapPinInfo.Zoomed)
        {
            return;
        }

        CameraManager cameraController = FindFirstObjectByType<CameraManager>();
        ZoomCanvasManager zoomCanvasManager = FindFirstObjectByType<ZoomCanvasManager>();

        // cameraControllerとzoomCanvasManagerのnullチェック
        if (cameraController != null && zoomCanvasManager != null)
        {
            if (MapPinInfo.prevStageInfo != null)
            {
                if (MapPinInfo.prevStageInfo.name == "Option")
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
            MapPinInfo.prevStageInfo = null;
        }
    }

    public void OnClickStartButton()
    {
        // OnClickStartButtonはStageCanvasからのみ呼ばれる想定
        // (OptionCanvasから呼ばれる場合は、このロジックも見直しが必要)

        if (MapPinInfo.prevStageInfo == dummy || MapPinInfo.prevStageInfo == null)
        {
            return;
        }

        // Optionが選ばれている場合はスタートさせない
        if (MapPinInfo.prevStageInfo.name == "Option")
        {
            return;
        }

        GameObject transitionObject = GameObject.FindGameObjectWithTag("SceneTransition_In");
        if (transitionObject != null)
        {
            GameOptions.InitFields(MapPinInfo.prevStageInfo);
            FindFirstObjectByType<CrossfadeAudioController>().ChangeClip(MapPinInfo.prevStageInfo.sceneNameScriptableObject.sceneInfos[0].BGM);
            StartCoroutine(transitionObject.GetComponent<SceneTransition>().StartSceneTransition(MapPinInfo.prevStageInfo.sceneNameScriptableObject.sceneInfos[0].SceneName));
        }
    }
}