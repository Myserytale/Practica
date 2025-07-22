using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ChestController : InteractableObject
{
    [Header("Chest Settings")]
    public AI_Movement assignedNPC = null;

    [Header("Chest Inventory")]
    public List<InventorySlot> chestInventory = new List<InventorySlot>();
    public int chestSize = 20;

    void Awake()
    {
        // Chest is not a resource, so ignore inherited resource fields
        isDepletable = false;
        requiredTool = null;
        durability = int.MaxValue; // Prevent accidental destruction

        for (int i = 0; i < chestSize; i++)
        {
            chestInventory.Add(new InventorySlot());
        }
    }

    // OVERRIDE Interact: Only open chest UI, ignore pickup/resource logic
    public override void Interact(Item toolUsed = null)
{
    ChestInventoryUI.Instance.OpenChest(this);
}

    // Chest-specific inventory logic below
    public bool AddItem(Item item, int quantity)
    {
        foreach (var slot in chestInventory)
        {
            if (slot.item == item && slot.quantity < item.maxStackSize)
            {
                slot.AddQuantity(quantity);
                return true;
            }
        }
        foreach (var slot in chestInventory)
        {
            if (slot.item == null)
            {
                slot.item = item;
                slot.quantity = quantity;
                return true;
            }
        }
        return false; // Full
    }

    public bool RemoveItem(Item item, int quantity)
    {
        if (!HasItem(item, quantity)) return false;

        int amountLeftToRemove = quantity;
        foreach (var slot in chestInventory.Where(s => s.item == item))
        {
            if (amountLeftToRemove <= 0) break;
            int amountToRemoveFromSlot = Mathf.Min(amountLeftToRemove, slot.quantity);
            slot.quantity -= amountToRemoveFromSlot;
            amountLeftToRemove -= amountToRemoveFromSlot;
            if (slot.quantity <= 0) slot.item = null;
        }
        return true;
    }

    public bool HasItem(Item item, int quantity)
    {
        return GetItemCount(item) >= quantity;
    }

    public int GetItemCount(Item item)
    {
        if (item == null) return 0;
        return chestInventory.Where(s => s.item == item).Sum(s => s.quantity);
    }

    public bool HasRecipeIngredients(CraftingRecipe recipe)
    {
        if (recipe == null) return true;
        foreach (var ingredient in recipe.ingredients)
        {
            if (!HasItem(ingredient.item, ingredient.quantity))
            {
                Debug.LogWarning($"Chest missing ingredient: {ingredient.quantity}x {ingredient.item.itemName}");
                return false;
            }
        }
        return true;
    }

    public void ConsumeRecipeIngredients(CraftingRecipe recipe)
    {
        if (recipe == null) return;
        foreach (var ingredient in recipe.ingredients)
        {
            RemoveItem(ingredient.item, ingredient.quantity);
        }
    }
}