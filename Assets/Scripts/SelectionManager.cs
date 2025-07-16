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

        // This raycast ONLY looks for objects on the 'interactionLayerMask' (Layer 7)
        if (Physics.Raycast(ray, out hit, maxDetectionDistance, interactionLayerMask))
        {
            InteractableObject interactable = hit.collider.GetComponent<InteractableObject>();
            if (interactable != null)
            {
                if (interactable != currentInteractable)
                {
                    currentInteractable = interactable;
                    interaction_text.text = currentInteractable.GetItemName();
                }
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
        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
        {
            // Handle different item types
            string itemName = currentInteractable.ItemName; // Use the base name
            Debug.Log($"Attempting to interact with: {itemName}");
            
            // Simple pickup logic for items like "Stone"
            if (itemName.ToLower().Contains("rock") || itemName.ToLower().Contains("stone"))
            {
                PickupStone(currentInteractable);
            }
            else
            {
                // For everything else (like bushes), call the general Interact method
                currentInteractable.Interact();
            }
        }
    }

     private void PickupStone(InteractableObject stone)
    {
        Debug.Log($"Picked up: {stone.GetItemName()}");
        
        // Add to inventory using the new system
        bool success = InventoryManager.Instance.AddItem(stone.GetItemName(), 1);

        if (success)
        {
            // Remove the stone from the world
            Destroy(stone.gameObject);

            // Clear selection since object is gone
            ClearSelection();
        }
        else
        {
            Debug.Log("Inventory is full!");
            Debug.Log("Failed to add item. Inventory might be full.");

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