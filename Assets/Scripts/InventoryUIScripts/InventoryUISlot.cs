using UnityEngine;
using UnityEngine.EventSystems;

// Add this enum definition
public enum InventoryOwner { Player, Chest }

public class InventoryUISlot : MonoBehaviour, IDropHandler
{
    [HideInInspector] public int slotIndex;
    [HideInInspector] public bool isToolbeltSlot = false;
    public InventoryOwner owner = InventoryOwner.Player; // Make this public to set in Inspector for chest slots

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject == null) return;

        DragDrop dragDropItem = droppedObject.GetComponent<DragDrop>();
        if (dragDropItem == null) return;

        InventoryUISlot sourceSlotUI = dragDropItem.originalParent.GetComponent<InventoryUISlot>();
        if (sourceSlotUI == null || sourceSlotUI == this) return;

        // --- Get Source and Destination Data Slots ---
        var sourceManager = InventoryManager.Instance;
        var sourceChest = (sourceSlotUI.owner == InventoryOwner.Chest) ? ChestInventoryUI.Instance.GetCurrentChest() : null;
        var destChest = (this.owner == InventoryOwner.Chest) ? ChestInventoryUI.Instance.GetCurrentChest() : null;

        InventorySlot sourceDataSlot = (sourceChest != null)
            ? sourceChest.chestInventory[sourceSlotUI.slotIndex]
            : (sourceSlotUI.isToolbeltSlot ? sourceManager.GetToolSlot(sourceSlotUI.slotIndex) : sourceManager.GetSlot(sourceSlotUI.slotIndex));

        InventorySlot destDataSlot = (destChest != null)
            ? destChest.chestInventory[this.slotIndex]
            : (this.isToolbeltSlot ? sourceManager.GetToolSlot(this.slotIndex) : sourceManager.GetSlot(this.slotIndex));

        if (sourceDataSlot == null || destDataSlot == null || sourceDataSlot.item == null) return;

        // --- Main Logic ---

        // Case 1: Destination slot is empty. Just move the item.
        if (destDataSlot.item == null)
        {
            destDataSlot.item = sourceDataSlot.item;
            destDataSlot.quantity = sourceDataSlot.quantity;
            sourceDataSlot.item = null;
            sourceDataSlot.quantity = 0;
        }
        // Case 2: Items are the same. Try to stack.
        else if (destDataSlot.item == sourceDataSlot.item)
        {
            int maxStack = destDataSlot.item.maxStackSize;
            int spaceLeftInDest = maxStack - destDataSlot.quantity;

            if (spaceLeftInDest > 0)
            {
                int amountToMove = Mathf.Min(spaceLeftInDest, sourceDataSlot.quantity);
                destDataSlot.quantity += amountToMove;
                sourceDataSlot.quantity -= amountToMove;

                if (sourceDataSlot.quantity <= 0)
                {
                    sourceDataSlot.item = null;
                    sourceDataSlot.quantity = 0;
                }
            }
            else // Destination is full, so swap.
            {
                SwapSlots(sourceDataSlot, destDataSlot);
            }
        }
        // Case 3: Items are different. Swap them.
        else
        {
            SwapSlots(sourceDataSlot, destDataSlot);
        }

        // --- Final UI Update ---
        // The DragDrop script handles moving the UI object. We just need to refresh the data.
        InventoryUI.Instance.UpdateUI();
        if (ChestInventoryUI.Instance.IsChestOpen())
        {
            ChestInventoryUI.Instance.UpdateChestUI();
        }
    }

    private void SwapSlots(InventorySlot slotA, InventorySlot slotB)
    {
        Item tempItem = slotB.item;
        int tempQuantity = slotB.quantity;

        slotB.item = slotA.item;
        slotB.quantity = slotA.quantity;

        slotA.item = tempItem;
        slotA.quantity = tempQuantity;
    }
}