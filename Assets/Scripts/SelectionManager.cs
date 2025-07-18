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

        if (Physics.Raycast(ray, out hit, maxDetectionDistance, interactionLayerMask))
        {
            InteractableObject interactable = hit.collider.GetComponent<InteractableObject>();
            if (interactable != null)
            {
                // If we are looking at a different object, or if the text is currently empty, update it.
                if (interactable != currentInteractable || interaction_text.text == "")
                {
                    currentInteractable = interactable;
                }
                // Always update the text to ensure it's showing for the current object.
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
        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
        {
            // The InteractableObject now handles its own logic (pickup vs. deplete)
            currentInteractable.Interact();

            // After interacting, the object might be destroyed, so we clear the selection text.
            ClearSelection();
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