using UnityEngine;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject interaction_Info_UI;

    [Header("Detection Settings")]
    public float maxDetectionDistance = 10f;
    public LayerMask interactionLayerMask; // This MUST be set to ONLY Layer 7 in the Inspector

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
                currentInteractable = null; // Ensure we're not targeting an object
                interaction_text.text = npc.GetInteractionText();
                return; // Found an NPC, no need to check for objects
            }

            // If no NPC, check for an interactable object
            InteractableObject interactable = hit.collider.GetComponent<InteractableObject>();
            if (interactable != null)
            {
                currentInteractable = interactable;
                currentNPC = null; // Ensure we're not targeting an NPC
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
                // Get the currently selected item from the toolbar
                InventorySlot selectedSlot = InventoryManager.Instance.GetSelectedToolbeltSlot();
                Item currentTool = (selectedSlot != null) ? selectedSlot.item : null;

                // Pass the current tool to the Interact method
                currentInteractable.Interact(currentTool);

                // After interacting, the object might be destroyed, so we clear the selection text.
                ClearSelection();
            }
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