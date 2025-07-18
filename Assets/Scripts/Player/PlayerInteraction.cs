using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public float interactionDistance = 3f;
    public LayerMask interactionLayer;

    // This would be used to show UI text like "Chop Tree"
    // public Text interactionText; 

    void Update()
    {
        // Hide interaction text by default
        // if (interactionText != null) interactionText.text = "";

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactionLayer))
        {
            InteractableObject interactable = hit.collider.GetComponent<InteractableObject>();

            if (interactable != null)
            {
                // Show what object we are looking at
                // if (interactionText != null) interactionText.text = interactable.GetItemName();

                // Check for player input to interact (e.g., left mouse button)
                if (Input.GetMouseButtonDown(0))
                {
                    // Get the currently selected item from the toolbar
                    InventorySlot selectedSlot = InventoryManager.Instance.GetSelectedToolbeltSlot();
                    Item currentTool = (selectedSlot != null) ? selectedSlot.item : null;

                    // Call the interact method on the object, passing the tool
                    interactable.Interact(currentTool);
                }
            }
        }
    }
}