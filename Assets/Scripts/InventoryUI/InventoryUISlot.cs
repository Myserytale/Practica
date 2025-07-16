using UnityEngine;
using UnityEngine.UI;

public class InventoryUISlot : MonoBehaviour
{
    public Image icon;
    public Text quantityText;

    // Clears the slot, making it look empty
    public void ClearSlot()
    {
        icon.enabled = false;
        quantityText.enabled = false;
    }

    // Draws the slot with the item's data
    public void DrawSlot(InventorySlot slot)
    {
        if (slot.item == null)
        {
            ClearSlot();
            return;
        }

        icon.enabled = true;
        quantityText.enabled = true;

        icon.sprite = slot.item.icon;
        quantityText.text = slot.quantity.ToString();
    }
}