using System;
using UnityEngine;

public class ChestInventoryUI : MonoBehaviour
{
    public static ChestInventoryUI Instance;

    public GameObject chestPanel;
    private ChestController currentChest;

    private void Awake()
    {
        Instance = this;
        chestPanel.SetActive(false);
        var chestSlots = chestPanel.GetComponentsInChildren<InventoryUISlot>();
        for (int i = 0; i < chestSlots.Length; i++) {
            chestSlots[i].slotIndex = i;
            chestSlots[i].owner = InventoryOwner.Chest;
    }
    }

    // NEW: Subscribe to the event when the script starts
    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChanged += OnInventoryDataChanged;
        }
    }

    // NEW: Unsubscribe when the script is destroyed to prevent errors
    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChanged -= OnInventoryDataChanged;
        }
    }

    // NEW: This method is called whenever ANY inventory changes
    private void OnInventoryDataChanged()
    {
        // If the chest UI is currently open, refresh it to show the latest data
        if (IsChestOpen())
        {
            UpdateChestUI();
        }
    }

    private void Update()
    {
        // Close chest with Escape or I
        if (IsChestOpen() && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.I)))
        {
            CloseChest();
        }
    }

    public void OpenChest(ChestController chest)
    {
        currentChest = chest;
        chestPanel.SetActive(true);
        InventoryUI.Instance.OpenInventory();

        // ADDED: Immediately update the UI when the chest is opened
        UpdateChestUI();
    }

    public void CloseChest()
    {
        chestPanel.SetActive(false);
        currentChest = null;
        InventoryUI.Instance.CloseInventory();
    }

    public ChestController GetCurrentChest() => currentChest;

    public bool IsChestOpen()
    {
        return chestPanel.activeSelf && currentChest != null;
    }

    public void UpdateChestUI()
    {
        if (currentChest == null || chestPanel == null) return;

        var chestSlots = chestPanel.GetComponentsInChildren<InventoryUISlot>();

        // This part is important: Clear existing item icons before redrawing
        foreach (var slot in chestSlots)
        {
            foreach (Transform child in slot.transform)
            {
                Destroy(child.gameObject);
            }
        }

        // Redraw items based on the chest's current inventory data
        for (int i = 0; i < currentChest.chestInventory.Count && i < chestSlots.Length; i++)
        {
            InventorySlot dataSlot = currentChest.chestInventory[i];
            if (dataSlot != null && dataSlot.item != null)
            {
                GameObject itemGO = Instantiate(InventoryUI.Instance.itemPrefab, chestSlots[i].transform);
                itemGO.GetComponent<InventoryItemUI>().SetItem(dataSlot);
            }
        }
    }
}