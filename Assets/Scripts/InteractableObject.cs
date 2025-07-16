using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Header("Info")]
    public string ItemName; // For display on UI

    [Header("Depletable Resource Settings")]
    public bool isDepletable = false;
    public int durability = 5;
    public Item itemToDrop; // The item to give the player (e.g., StickItem)
    public int minDropAmount = 1;
    public int maxDropAmount = 3;

    public string GetItemName()
    {
        // Show remaining durability in the UI text
        if (isDepletable)
        {
            return $"{ItemName} [{durability}]";
        }
        return ItemName;
    }

    // This is called by the SelectionManager
    public void Interact()
    {
        if (isDepletable)
        {
            TakeDamage();
        }
        else
        {
            // Handle other non-depletable interactions (like opening a door)
            Debug.Log($"Interacted with a non-depletable object: {this.ItemName}");
        }
    }

    private void TakeDamage()
    {
        durability--;

        // Give the player some sticks
        int amountToDrop = Random.Range(minDropAmount, maxDropAmount + 1);
        if (itemToDrop != null && amountToDrop > 0)
        {
            InventoryManager.Instance.AddItem(itemToDrop.itemName, amountToDrop);
            Debug.Log($"Gathered {amountToDrop} {itemToDrop.itemName}. Bush durability is now {durability}.");
        }

        // Check if the bush is destroyed
        if (durability <= 0)
        {
            Debug.Log($"{ItemName} has been depleted and is destroyed.");
            Destroy(gameObject);
        }
    }
}