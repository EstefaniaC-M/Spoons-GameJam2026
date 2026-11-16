using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class DraggableMicroscopeSample : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform microscopeDropZone;
    [SerializeField] private MicroscopeTaskController taskController;

    private RectTransform sampleRect;
    private RectTransform dragParent;
    private Vector3 originalLocalPosition;
    private Vector2 pointerOffset;
    private int originalSiblingIndex;
    private bool isPlaced;

    private void Awake()
    {
        sampleRect = GetComponent<RectTransform>();

        Graphic sampleGraphic = GetComponent<Graphic>();
        if (sampleGraphic == null)
            Debug.LogError("DraggableMicroscopeSample requires a UI Graphic component.", this);
        else
            sampleGraphic.raycastTarget = true;

        if (microscopeDropZone == null)
            Debug.LogError("DraggableMicroscopeSample requires a microscope drop zone.", this);
        if (taskController == null)
            Debug.LogError("DraggableMicroscopeSample requires a microscope task controller.", this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlaced || microscopeDropZone == null || taskController == null)
            return;

        dragParent = sampleRect.parent as RectTransform;
        if (dragParent == null)
        {
            Debug.LogError("The draggable sample must be under a UI RectTransform.", this);
            return;
        }

        originalLocalPosition = sampleRect.localPosition;
        originalSiblingIndex = sampleRect.GetSiblingIndex();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragParent, eventData.position, eventData.pressEventCamera, out Vector2 pointerPosition))
        {
            dragParent = null;
            return;
        }

        pointerOffset = (Vector2)sampleRect.localPosition - pointerPosition;
        sampleRect.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPlaced || dragParent == null)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragParent, eventData.position, eventData.pressEventCamera, out Vector2 pointerPosition))
        {
            Vector2 localPosition = pointerPosition + pointerOffset;
            sampleRect.localPosition = new Vector3(
                localPosition.x, localPosition.y, sampleRect.localPosition.z);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPlaced || dragParent == null)
            return;

        bool droppedOnMicroscope = microscopeDropZone != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                microscopeDropZone, eventData.position, eventData.pressEventCamera);

        if (!droppedOnMicroscope)
        {
            sampleRect.localPosition = originalLocalPosition;
            sampleRect.SetSiblingIndex(originalSiblingIndex);
            dragParent = null;
            return;
        }

        sampleRect.SetParent(microscopeDropZone.parent, true);
        sampleRect.position = microscopeDropZone.position;
        isPlaced = true;
        dragParent = null;
        taskController.OnSampleDropped();
        sampleRect.gameObject.SetActive(false);
        microscopeDropZone.gameObject.SetActive(false);
    }
}
