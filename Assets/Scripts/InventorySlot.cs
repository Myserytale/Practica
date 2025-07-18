[System.Serializable]
public class InventorySlot
{
    public Item item;
    public int quantity;

    // Constructor for creating a slot with a specific item and quantity.
    public InventorySlot(Item item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }

    // Add this empty constructor.
    // This allows you to create an empty slot, like in your InventoryManager.
    public InventorySlot()
    {
        item = null;
        quantity = 0;
    }

    // Helper method to add quantity to this slot
    public void AddQuantity(int amount)
    {
        quantity += amount;
    }
}