using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Header("Info")]
    public Item itemToDrop;

    [Header("Depletable Resource Settings")]
    public bool isDepletable = false;
    public Item requiredTool;
    public int durability = 5;
    public int minDropAmount = 1;
    public int maxDropAmount = 3;

    public string GetItemName()
    {
        if (itemToDrop == null) return "Unnamed Object";

        if (isDepletable)
        {
            return $"{itemToDrop.itemName} [{durability}]";
        }
        return itemToDrop.itemName;
    }

    public void Interact(Item toolUsed)
    {
        if (isDepletable)
        {
            // It's a resource like a tree. Check if the correct tool is used.
            if (requiredTool == null || requiredTool == toolUsed)
            {
                TakeDamage();
            }
            else
            {
                // Wrong tool was used. Do nothing, or give feedback.
                Debug.Log($"You need a {requiredTool.itemName} to harvest this.");
            }
        }
        else
        {
            // It's a simple pickup item.
            PickupItem();
        }
    }


    private void PickupItem()
    {
        if (itemToDrop == null) return;

        bool success = InventoryManager.Instance.AddItem(itemToDrop.itemName, 1);
        if (success)
        {
            Destroy(gameObject);
        }
    }

    private void TakeDamage()
    {
        durability--;

        int amountToDrop = Random.Range(minDropAmount, maxDropAmount + 1);
        if (itemToDrop != null && amountToDrop > 0)
        {
            InventoryManager.Instance.AddItem(itemToDrop.itemName, amountToDrop);
        }

        if (durability <= 0)
        {
            Destroy(gameObject);
        }
    }
}