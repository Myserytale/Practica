using UnityEngine;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject interaction_Info_UI;

    [Header("Detection Settings")]
    public float maxDetectionDistance = 10f;
    public LayerMask interactionLayerMask; // This MUST be set to ONLY Layer 7 in the Inspector
    public LayerMask groundMask; // <-- Add this line

    [Header("Debug")]
    public bool showDebugRay = true;

    private Text interaction_text;
    private InteractableObject currentInteractable;
    private Camera mainCamera;
    private NPCController currentNPC;

    private void Start()
    {
        mainCamera = Camera.main;
        if (interaction_Info_UI != null)
        {
            interaction_text = interaction_Info_UI.GetComponent<Text>();
            if (interaction_text != null)
            {
                interaction_text.text = "";
            }
        }
    }

    void Update()
    {
        if (interaction_text == null) return;
        HandleDetection();
        HandleInteractionInput();
    }

    private void HandleDetection()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * maxDetectionDistance, Color.cyan);
        }

        if (Physics.Raycast(ray, out hit, maxDetectionDistance, interactionLayerMask))
        {
            // Check for an NPC first
            NPCController npc = hit.collider.GetComponent<NPCController>();
            if (npc != null)
            {
                currentNPC = npc;
                currentInteractable = null;
                interaction_text.text = npc.GetInteractionText();
                return;
            }

            // If no NPC, check for an interactable object
            InteractableObject interactable = hit.collider.GetComponent<InteractableObject>();
            if (interactable != null)
            {
                currentInteractable = interactable;
                currentNPC = null;
                interaction_text.text = interactable.GetItemName();
            }
            else
            {
                ClearSelection();
            }
        }
        else
        {
            ClearSelection();
        }
    }

   private void HandleInteractionInput()
{
    if (Input.GetKeyDown(KeyCode.E))
    {
        if (currentNPC != null)
        {
            currentNPC.Interact();
            ClearSelection();
        }
        else if (currentInteractable != null)
        {
            InventorySlot selectedSlot = InventoryManager.Instance.GetSelectedToolbeltSlot();
            Item currentTool = (selectedSlot != null) ? selectedSlot.item : null;

            // If it's a chest, open its inventory UI
            ChestController chest = currentInteractable.GetComponent<ChestController>();
            if (chest != null)
            {
                chest.Interact();
            }
            else
            {
                // Normal interact logic
                currentInteractable.Interact(currentTool);
            }
            ClearSelection();
        }
    }

    if (Input.GetMouseButtonDown(1)) // Right mouse button
    {
        PlaceSelectedItem();
    }
}

    private void PlaceSelectedItem()
    {
        InventorySlot selectedSlot = InventoryManager.Instance.GetSelectedToolbeltSlot();
        if (selectedSlot == null || selectedSlot.item == null || selectedSlot.item.itemType != ItemType.Placeable)
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDetectionDistance, groundMask))
        {
            Instantiate(selectedSlot.item.objectPrefab, hit.point, Quaternion.identity);
            InventoryManager.Instance.RemoveItem(selectedSlot.item.itemName, 1);
        }
    }

    private void ClearSelection()
    {
        if (currentInteractable != null)
        {
            currentInteractable = null;
            interaction_text.text = "";
        }
    }
}