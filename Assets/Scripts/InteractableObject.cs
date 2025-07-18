using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Header("Info")]
    public Item itemToDrop;

    [Header("Depletable Resource Settings")]
    public bool isDepletable = false;
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

    public void Interact()
    {
        if (isDepletable)
        {
            TakeDamage();
        }
        else
        {
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