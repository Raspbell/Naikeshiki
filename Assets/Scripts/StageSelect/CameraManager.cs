using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

public class CameraManager : MonoBehaviour
{
    [SerializeField] float zoomDistanceWeight = 0.5f;

    private Vector3 initialPosition;
    private Quaternion initialRotate;
    private Camera cam;
    private ClickZoomCamera.ZoomProperty prevZoomProperty;

    void Start()
    {
        cam = Camera.main;
        if (cam != null)
        {
            initialPosition = cam.transform.position;
            initialRotate = cam.transform.rotation;
        }
    }

    public async UniTask FocusToObject(ClickZoomCamera.ZoomProperty property)
    {
        if (property == null)
        {
            return;
        }

        if (property == prevZoomProperty && ClickZoomCamera.Zoomed)
        {
            return;
        }

        // float distanceWeight = 1;
        // if (prevZoomProperty != null)
        // {
        //     distanceWeight = Vector3.Distance(prevZoomProperty.zoomPoint.position, property.zoomPoint.position) * zoomDistanceWeight;
        // }
        // else
        // {
        //     distanceWeight = Vector3.Distance(cam.transform.position, property.zoomPoint.position) * zoomDistanceWeight;
        // }

        ClickZoomCamera.NowZooming = true;

        try
        {
            Camera cam = Camera.main;

            Vector3 targetPos = property.zoomPoint.position + property.targetOffset;

            float rad = property.elevationAngle * Mathf.Deg2Rad;
            Vector3 forward = (cam.transform.position - targetPos).normalized;

            Vector3 dirXZ = new Vector3(forward.x, 0f, forward.z).normalized;
            Vector3 zoomDir = Quaternion.AngleAxis(-property.elevationAngle, Vector3.Cross(dirXZ, Vector3.up)) * dirXZ;
            zoomDir = zoomDir.normalized;

            Vector3 zoomTarget = targetPos + zoomDir * property.zoomDistance;

            Quaternion startRot = cam.transform.rotation;

            Vector3 toTarget = (targetPos - zoomTarget).normalized;
            float yDist = Mathf.Sin(property.elevationAngle * Mathf.Deg2Rad);
            float xzDist = Mathf.Cos(property.elevationAngle * Mathf.Deg2Rad);
            Vector3 lookDir = new Vector3(toTarget.x * xzDist, yDist, toTarget.z * xzDist).normalized;
            Quaternion endRot = Quaternion.LookRotation(lookDir, Vector3.up);

            Tweener moveTween = cam.transform
                .DOMove(zoomTarget, property.zoomDuration)
                .SetEase(property.ease);

            Tweener rotTween = cam.transform
                .DORotateQuaternion(endRot, property.zoomDuration)
                .SetEase(property.ease);

            Task moveTask = moveTween.AsyncWaitForCompletion();
            Task rotTask = rotTween.AsyncWaitForCompletion();

            await Task.WhenAll(moveTask, rotTask);

            prevZoomProperty = property;
            ClickZoomCamera.Zoomed = true;
        }
        finally
        {
            ClickZoomCamera.NowZooming = false;
        }
    }

    public async UniTask ReturnToInitialPosition()
    {
        if (Camera.main == null)
        {
            return;
        }

        // float distanceWeight = 1;
        // distanceWeight = Vector3.Distance(prevZoomProperty.zoomPoint.position, initialPosition) * zoomDistanceWeight;

        // 前回のズーム設定が無い場合のフォールバック
        float duration = (prevZoomProperty != null) ? prevZoomProperty.zoomDuration : 0.8f;
        Ease ease = (prevZoomProperty != null) ? prevZoomProperty.ease : Ease.InOutQuad;

        ClickZoomCamera.NowZooming = true;

        try
        {
            Camera cam = Camera.main;

            Tweener moveTw = cam.transform
                .DOMove(initialPosition, duration)
                .SetEase(ease);

            Tweener rotTw = cam.transform
                .DORotateQuaternion(initialRotate, duration)
                .SetEase(ease);

            Task moveTask = moveTw.AsyncWaitForCompletion();
            Task rotTask = rotTw.AsyncWaitForCompletion();

            await Task.WhenAll(moveTask, rotTask);

            ClickZoomCamera.Zoomed = false;
        }
        finally
        {
            ClickZoomCamera.NowZooming = false;
        }
    }
}
