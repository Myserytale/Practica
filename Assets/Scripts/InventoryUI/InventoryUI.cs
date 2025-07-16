using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;
    public Transform slotsParent; // The parent object for all the slots

    [Header("Prefabs")]
    public GameObject slotPrefab;

    private InventoryUISlot[] uiSlots;

    void Start()
    {
        // Generate the UI slots based on inventory size
        uiSlots = new InventoryUISlot[InventoryManager.Instance.inventorySize];
        for (int i = 0; i < uiSlots.Length; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, slotsParent);
            uiSlots[i] = slotGO.GetComponent<InventoryUISlot>();
        }

        // Deactivate the panel to start
        inventoryPanel.SetActive(false);
    }

    void Update()
    {
        // Toggle inventory with the "I" key
        if (Input.GetKeyDown(KeyCode.I))
        {
            bool isActive = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(isActive);

            // Update the UI only when it's opened
            if (isActive)
            {
                UpdateUI();
            }
        }
    }

    // Updates all UI slots to match the InventoryManager's data
    public void UpdateUI()
    {
        for (int i = 0; i < uiSlots.Length; i++)
        {
            // Get the corresponding slot data from the manager
            InventorySlot dataSlot = InventoryManager.Instance.GetSlot(i);
            if (dataSlot != null)
            {
                uiSlots[i].DrawSlot(dataSlot);
            }
            else
            {
                uiSlots[i].ClearSlot();
            }
        }
    }
}