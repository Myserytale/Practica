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

    public event Action onInventoryChanged;

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

    public InventorySlot GetSlot(int index) => (index < inventory.Count) ? inventory[index] : null;
    public InventorySlot GetToolSlot(int index) => (index < toolbelt.Count) ? toolbelt[index] : null;
}