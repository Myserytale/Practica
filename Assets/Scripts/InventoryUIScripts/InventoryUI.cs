using UnityEngine;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;
    public Transform slotsParent;
    public Transform toolSlotsParent; // This is now optional

    [Header("Prefabs")]
    public GameObject itemPrefab;

    private List<InventoryUISlot> inventorySlots = new List<InventoryUISlot>();
    private List<InventoryUISlot> toolBarSlots = new List<InventoryUISlot>();

    void Start()
    {
        if (slotsParent == null)
        {
            Debug.LogError("InventoryUI: 'Slots Parent' is not assigned in the Inspector!", this.gameObject);
            this.enabled = false;
            return;
        }
        slotsParent.GetComponentsInChildren<InventoryUISlot>(inventorySlots);

        if (toolSlotsParent != null)
        {
            toolSlotsParent.GetComponentsInChildren<InventoryUISlot>(toolBarSlots);
        }

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            inventorySlots[i].slotIndex = i;
            inventorySlots[i].isToolbeltSlot = false;
        }
        for (int i = 0; i < toolBarSlots.Count; i++)
        {
            toolBarSlots[i].slotIndex = i;
            toolBarSlots[i].isToolbeltSlot = true;
        }

        inventoryPanel.SetActive(false);
        InventoryManager.Instance.onInventoryChanged += OnInventoryChanged;
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChanged -= OnInventoryChanged;
        }
    }

    private void OnInventoryChanged()
    {
        if (inventoryPanel.activeSelf)
        {
            UpdateUI();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            bool isActive = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(isActive);
            Cursor.lockState = isActive ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isActive;

            if (isActive)
            {
                UpdateUI();
            }
        }
    }

    public void UpdateUI()
    {
        ClearAllSlots();
        DrawAllSlots();
    }

    private void ClearAllSlots()
    {
        foreach (var slot in inventorySlots)
        {
            foreach (Transform child in slot.transform) Destroy(child.gameObject);
        }
        if (toolBarSlots.Count > 0)
        {
            foreach (var slot in toolBarSlots)
            {
                foreach (Transform child in slot.transform) Destroy(child.gameObject);
            }
        }
    }

    private void DrawAllSlots()
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            InventorySlot dataSlot = InventoryManager.Instance.GetSlot(i);
            if (dataSlot != null && dataSlot.item != null)
            {
                GameObject itemGO = Instantiate(itemPrefab, inventorySlots[i].transform);
                itemGO.GetComponent<InventoryItemUI>().SetItem(dataSlot);
            }
        }

        if (toolBarSlots.Count > 0)
        {
            for (int i = 0; i < toolBarSlots.Count; i++)
            {
                InventorySlot dataSlot = InventoryManager.Instance.GetToolSlot(i);
                if (dataSlot != null && dataSlot.item != null)
                {
                    GameObject itemGO = Instantiate(itemPrefab, toolBarSlots[i].transform);
                    itemGO.GetComponent<InventoryItemUI>().SetItem(dataSlot);
                }
            }
        }
    }
}