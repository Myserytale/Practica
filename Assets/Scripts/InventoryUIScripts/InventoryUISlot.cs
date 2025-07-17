using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryUISlot : MonoBehaviour, IDropHandler
{
    [HideInInspector] public int slotIndex;
    [HideInInspector] public bool isToolbeltSlot = false;

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject == null) return;

        DragDrop dragDropItem = droppedObject.GetComponent<DragDrop>();
        if (dragDropItem != null)
        {
            dragDropItem.parentAfterDrag = this.transform;

            InventoryUISlot sourceSlot = dragDropItem.originalParent.GetComponent<InventoryUISlot>();

            if (sourceSlot != null && sourceSlot != this)
            {
                InventoryManager.Instance.SwapItems(sourceSlot.slotIndex, sourceSlot.isToolbeltSlot, this.slotIndex, this.isToolbeltSlot);
            }
        }
    }
}