using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;
    public Transform slotsParent;
    public Transform toolSlotsParent; 

    [Header("Prefabs")]
    public GameObject itemPrefab;

    [Header("Toolbar Selection")]
    public Color selectedSlotColor = Color.yellow;
    public Color defaultSlotColor = Color.white;

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
        InventoryManager.Instance.onSelectedToolbeltSlotChanged += UpdateToolbarSelectionVisuals;

        // Set initial selection visual
        if (toolBarSlots.Count > 0)
        {
            UpdateToolbarSelectionVisuals(0);
        }
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChanged -= OnInventoryChanged;
            InventoryManager.Instance.onSelectedToolbeltSlotChanged -= UpdateToolbarSelectionVisuals;
        }
    }

    private void OnInventoryChanged()
    {
        UpdateUI();
    }

    void Update()
    {

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            return; // Ignore input if dialogue is active
        }
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

        HandleToolbarSelectionInput();
    }

    private void HandleToolbarSelectionInput()
    {
        for (int i = 0; i < toolBarSlots.Count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                InventoryManager.Instance.SetSelectedToolbeltSlot(i);
            }
        }
    }

    public void UpdateUI()
    {
        ClearAllSlots();
        DrawAllSlots();
    }

    private void UpdateToolbarSelectionVisuals(int selectedIndex)
    {
        for (int i = 0; i < toolBarSlots.Count; i++)
        {
            Image slotImage = toolBarSlots[i].GetComponent<Image>();
            if (slotImage != null)
            {
                slotImage.color = (i == selectedIndex) ? selectedSlotColor : defaultSlotColor;
            }
        }
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
        // Draw inventory slots
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            InventorySlot dataSlot = InventoryManager.Instance.GetSlot(i);
            if (dataSlot != null && dataSlot.item != null)
            {
                GameObject itemGO = Instantiate(itemPrefab, inventorySlots[i].transform);
                itemGO.GetComponent<InventoryItemUI>().SetItem(dataSlot);
            }
        }

        // Draw toolbar slots
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