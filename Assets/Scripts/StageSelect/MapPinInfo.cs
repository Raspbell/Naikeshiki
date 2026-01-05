using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine.EventSystems;

public class MapPinInfo : MonoBehaviour
{
    [SerializeField] private ZoomProperty zoomProperty;
    [SerializeField] private StageInfo stageInfo;

    public static bool NowZooming = false;
    public static bool Zoomed = false;
    public static StageInfo prevStageInfo = null;

    private CameraManager cameraManager;
    private ZoomCanvasManager zoomCanvasManager;
    private DifficulitySignManager difficulitySignManager;

    [Serializable]
    public class ZoomProperty
    {
        public Transform zoomPoint;
        public float zoomDistance = 5f;
        public float zoomDuration = 1f;
        public float elevationAngle = -30f;
        public Ease ease;
        public Vector3 targetOffset = Vector3.zero;
    }

    void Start()
    {
        zoomProperty.zoomPoint = transform;
        cameraManager = FindFirstObjectByType<CameraManager>();
        zoomCanvasManager = FindFirstObjectByType<ZoomCanvasManager>();
        difficulitySignManager = FindFirstObjectByType<DifficulitySignManager>();
        Zoomed = false;
        NowZooming = false;
    }

    async UniTask Update()
    {
        Camera cam = Camera.main;

        if (Input.GetMouseButtonDown(0) && !NowZooming)
        {
            if (cam == null)
            {
                return;
            }

            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                if (hit.collider != null && hit.collider.gameObject == gameObject)
                {
                    await OnObjectClicked();
                }
            }
        }
    }

    private async UniTask OnObjectClicked()
    {
        if (Zoomed)
        {
            if (prevStageInfo != null)
            {
                if (prevStageInfo.name == "Option")
                {
                    zoomCanvasManager.DismissOptionCanvas();
                }
                else
                {
                    zoomCanvasManager.DismissStageCanvas();
                }
            }
        }


        await cameraManager.FocusToObject(zoomProperty);

        if (stageInfo.name == "Option")
        {
            zoomCanvasManager.ShowOptionCanvas(stageInfo);
        }
        else
        {
            zoomCanvasManager.ShowStageCanvas(stageInfo);
        }

        prevStageInfo = stageInfo;
    }
}
