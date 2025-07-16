using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    private static InventoryManager _instance;
    public static InventoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("InventoryManager is NULL.");
            }
            return _instance;
        }
    }

    [Header("Inventory Settings")]
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();
    public int inventorySize = 20;

    [Header("Item Database")]
    public List<Item> itemDatabase = new List<Item>();

    private bool isInitialized = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("Duplicate InventoryManager found. Destroying it.");
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
    }

    private void Initialize()
    {
        if (isInitialized) return;

        slots.Clear();
        for (int i = 0; i < inventorySize; i++)
        {
            slots.Add(new InventorySlot(null, 0));
        }
        isInitialized = true;
        Debug.Log("Inventory Initialized with " + slots.Count + " slots.");
    }

    public bool AddItem(string itemName, int amount = 1)
    {
        if (!isInitialized) Initialize(); // Failsafe

        Item itemToAdd = GetItemFromDatabase(itemName);
        if (itemToAdd == null)
        {
            Debug.LogError($"CRITICAL: Item '{itemName}' not found in the database.");
            return false;
        }

        foreach (InventorySlot slot in slots)
        {
            if (slot.item == itemToAdd && slot.quantity < itemToAdd.maxStackSize)
            {
                int spaceLeft = itemToAdd.maxStackSize - slot.quantity;
                int amountToAdd = Mathf.Min(amount, spaceLeft);
                slot.quantity += amountToAdd;
                amount -= amountToAdd;
                if (amount <= 0)
                {
                    Debug.Log($"Added {amountToAdd} {itemName}(s) to an existing stack.");
                    return true;
                }
            }
        }

        foreach (InventorySlot slot in slots)
        {
            if (slot.item == null)
            {
                slot.item = itemToAdd;
                slot.quantity = amount;
                Debug.Log($"Added {amount} {itemName}(s) to a new empty slot.");
                return true;
            }
        }

        Debug.LogWarning("Inventory is full!");
        return false;
    }

    private Item GetItemFromDatabase(string itemName)
    {
        foreach (Item item in itemDatabase)
        {
            if (item != null && item.itemName.Equals(itemName, System.StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }
        return null;
    }

    public InventorySlot GetSlot(int index)
    {
        if (index >= 0 && index < slots.Count)
        {
            return slots[index];
        }
        return null;
    }
}