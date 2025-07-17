using UnityEngine;
using UnityEngine.UI;

// This script goes on your "itemPrefab"
[RequireComponent(typeof(DragDrop))]
public class InventoryItemUI : MonoBehaviour
{
    [Header("UI References")]
    public Image icon;
    public Text quantityText;

    // This method is called by InventoryUI to set up the item's appearance
    public void SetItem(InventorySlot slot)
    {
        if (slot == null || slot.item == null)
        {
            Destroy(gameObject); // If data is invalid, destroy this item UI object
            return;
        }

        // Update the visuals
        if (icon != null)
        {
            icon.enabled = true;
            icon.sprite = slot.item.icon;
        }

        // Update the quantity text
        if (quantityText != null)
        {
            if (slot.quantity > 1)
            {
                quantityText.text = slot.quantity.ToString();
                quantityText.enabled = true;
            }
            else
            {
                quantityText.enabled = false;
            }
        }
    }
}