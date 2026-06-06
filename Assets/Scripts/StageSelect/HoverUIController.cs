using UnityEngine;
using UnityEngine.EventSystems;

public class HoverUIController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject targetUI;
    [SerializeField] private bool hideOnStart = true;

    private void Start()
    {
        if (targetUI != null && hideOnStart)
        {
            targetUI.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetTargetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetTargetActive(false);
    }

    private void OnDisable()
    {
        SetTargetActive(false);
    }

    private void SetTargetActive(bool isActive)
    {
        if (targetUI != null)
        {
            targetUI.SetActive(isActive);
        }
    }
}