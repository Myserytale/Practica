using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    private static InventoryManager _instance;
    public static InventoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<InventoryManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("InventoryManager");
                    _instance = obj.AddComponent<InventoryManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Inventory Data")]
    public List<Item> itemDatabase;

    [Header("Inventory Settings")]
    public int inventorySize = 20;
    public int toolBeltSize = 5;

    private List<InventorySlot> inventory;
    private List<InventorySlot> toolbelt;

    private int selectedToolbeltIndex = 0;


    public event Action onInventoryChanged;

    public event Action<int> onSelectedToolbeltSlotChanged;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        inventory = new List<InventorySlot>(inventorySize);
        for (int i = 0; i < inventorySize; i++)
        {
            inventory.Add(new InventorySlot());
        }

        toolbelt = new List<InventorySlot>(toolBeltSize);
        for (int i = 0; i < toolBeltSize; i++)
        {
            toolbelt.Add(new InventorySlot());
        }
    }

    public void SetSelectedToolbeltSlot(int index)
    {
        if (index >= 0 && index < toolBeltSize)
        {
            selectedToolbeltIndex = index;
            onSelectedToolbeltSlotChanged?.Invoke(selectedToolbeltIndex);
        }
    }

    public InventorySlot GetSelectedToolbeltSlot()
    {
        if (selectedToolbeltIndex < 0 || selectedToolbeltIndex >= toolbelt.Count)
        {
            return null;
        }
        return toolbelt[selectedToolbeltIndex];
    }

    public Item GetItemByName(string name)
    {
        return itemDatabase.FirstOrDefault(item => item.itemName.Equals(name, StringComparison.OrdinalIgnoreCase));

    }

    public bool AddItem(string itemName, int amount = 1)
    {
        Item itemToAdd = GetItemByName(itemName);
        if (itemToAdd == null)
        {
            Debug.LogWarning($"Item '{itemName}' not found in the database.");
            return false;
        }

        // Prioritize adding to toolbelt first
        // 1. Try to stack in toolbelt
        foreach (var slot in toolbelt)
        {
            if (slot.item == itemToAdd && slot.quantity < itemToAdd.maxStackSize)
            {
                int amountCanAdd = itemToAdd.maxStackSize - slot.quantity;
                int amountToAddNow = Mathf.Min(amount, amountCanAdd);
                slot.quantity += amountToAddNow;
                amount -= amountToAddNow;
                if (amount <= 0)
                {
                    onInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        // 2. Try to stack in main inventory
        foreach (var slot in inventory)
        {
            if (slot.item == itemToAdd && slot.quantity < itemToAdd.maxStackSize)
            {
                int amountCanAdd = itemToAdd.maxStackSize - slot.quantity;
                int amountToAddNow = Mathf.Min(amount, amountCanAdd);
                slot.quantity += amountToAddNow;
                amount -= amountToAddNow;
                if (amount <= 0)
                {
                    onInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        // If items remain, find new slots
        if (amount > 0)
        {
            // 3. Find empty slot in toolbelt
            foreach (var slot in toolbelt)
            {
                if (slot.item == null)
                {
                    slot.item = itemToAdd;
                    slot.quantity = amount;
                    onInventoryChanged?.Invoke();
                    return true;
                }
            }
        }
        
        if (amount > 0)
        {
            // 4. Find empty slot in main inventory
            foreach (var slot in inventory)
            {
                if (slot.item == null)
                {
                    slot.item = itemToAdd;
                    slot.quantity = amount;
                    onInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        Debug.Log("Inventory is full!");
        return false;
    }

    public void SwapItems(int sourceIndex, bool sourceIsToolbelt, int destIndex, bool destIsToolbelt)
    {
        var sourceList = sourceIsToolbelt ? toolbelt : inventory;
        var destList = destIsToolbelt ? toolbelt : inventory;

        if (sourceIndex >= sourceList.Count || destIndex >= destList.Count) return;

        InventorySlot sourceSlot = sourceList[sourceIndex];
        InventorySlot destSlot = destList[destIndex];

        var tempItem = destSlot.item;
        var tempQuantity = destSlot.quantity;

        destSlot.item = sourceSlot.item;
        destSlot.quantity = sourceSlot.quantity;

        sourceSlot.item = tempItem;
        sourceSlot.quantity = tempQuantity;

        onInventoryChanged?.Invoke();
    }

    public void CraftItem(CraftingRecipe recipe)
    {
        if (CanCraftItem(recipe))
        {
            foreach (var ingredient in recipe.ingredients)
            {
                RemoveItem(ingredient.item.itemName, ingredient.quantity);
            }
            AddItem(recipe.outputItem.itemName, recipe.outputQuantity);
            // onInventoryChanged is already called by RemoveItem and AddItem
        }
        else
        {
            Debug.LogWarning("Cannot craft item: missing ingredients.");
        }
    }


    public bool CanCraftItem(CraftingRecipe recipe)
    {
        foreach (var ingredient in recipe.ingredients)
        {
            if (GetTotalItemQuantity(ingredient.item.itemName) < ingredient.quantity)
            {
                return false;
            }
        }
        return true;
    }

    public bool HasItem(string itemName, int quantity)
    {
        return GetTotalItemQuantity(itemName) >= quantity;
    }

    public void RemoveItem(string itemName, int quantity)
    {
        Item itemToRemove = GetItemByName(itemName);
        if (itemToRemove == null) return;

        int amountLeftToRemove = quantity;

        // Create a list of all slots containing the item
        var allSlots = toolbelt.Concat(inventory);
        var itemSlots = allSlots.Where(s => s.item == itemToRemove).ToList();

        // Iterate through the slots to remove the required quantity
        foreach (var slot in itemSlots)
        {
            if (amountLeftToRemove <= 0) break;

            int amountInSlot = slot.quantity;
            if (amountInSlot >= amountLeftToRemove)
            {
                slot.quantity -= amountLeftToRemove;
                amountLeftToRemove = 0;
            }
            else
            {
                amountLeftToRemove -= amountInSlot;
                slot.quantity = 0;
            }

            if (slot.quantity <= 0)
            {
                slot.item = null;
            }
        }

        // If something was changed, update the inventory UI
        if (amountLeftToRemove < quantity)
        {
            onInventoryChanged?.Invoke();
        }
    }

    private int GetTotalItemQuantity(string itemName)
    {
        int total = 0;
        Item item = GetItemByName(itemName);
        if (item == null) return 0;

        total += inventory.Where(s => s.item == item).Sum(s => s.quantity);
        total += toolbelt.Where(s => s.item == item).Sum(s => s.quantity);
        return total;
    }

    public InventorySlot GetSlot(int index) => (index < inventory.Count) ? inventory[index] : null;
    public InventorySlot GetToolSlot(int index) => (index < toolbelt.Count) ? toolbelt[index] : null;
}