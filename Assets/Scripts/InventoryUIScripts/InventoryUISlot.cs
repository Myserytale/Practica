using UnityEngine;
using UnityEngine.EventSystems;

public enum InventoryOwner { Player, Chest }

public class InventoryUISlot : MonoBehaviour, IDropHandler
{
    [Header("Slot Info")]
    public int slotIndex;
    public bool isToolbeltSlot = false;
    public InventoryOwner owner = InventoryOwner.Player;

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject == null) return;

        DragDrop dragItem = droppedObject.GetComponent<DragDrop>();
        if (dragItem == null) return;

        InventoryUISlot sourceSlot = dragItem.originalParent.GetComponent<InventoryUISlot>();
        
        // Don't allow dropping onto the same slot
        if (sourceSlot != null && sourceSlot != this)
        {
            // Let the InventoryManager handle all the complex logic
            InventoryManager.Instance.TransferItem(sourceSlot, this);
        }
    }
}