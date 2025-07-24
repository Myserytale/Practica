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
        // Disable camera movement
        FindFirstObjectByType<MouseMovement>()?.SetCameraActive(false);
        FindFirstObjectByType<FollowPlayer>()?.SetCameraActive(false);
    }

    public void CloseChest()
    {
        chestPanel.SetActive(false);
        currentChest = null;

        InventoryUI.Instance.CloseInventory();
        // Enable camera movement
        FindFirstObjectByType<MouseMovement>()?.SetCameraActive(true);
        FindFirstObjectByType<FollowPlayer>()?.SetCameraActive(true);
    }

    public ChestController GetCurrentChest() => currentChest;

    public bool IsChestOpen()
    {
        return chestPanel.activeSelf && currentChest != null;
    }

    public void UpdateChestUI()
{
    if (currentChest == null || chestPanel == null) return;

    // Find all InventoryUISlot components under your chestPanel (or a slotsParent if you use one)
    var chestSlots = chestPanel.GetComponentsInChildren<InventoryUISlot>();

    // Clear all slot visuals
    foreach (var slot in chestSlots)
    {
        foreach (Transform child in slot.transform)
        {
            Destroy(child.gameObject);
        }
    }

    // Draw items in chest slots
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