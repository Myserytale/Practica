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
            // SUCCESS: The ray ignored the ground and hit an interactable object.
            Debug.Log($"SUCCESS: Raycast hit '{hit.collider.name}' on the correct layer.");
            
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
                Debug.LogWarning($"Object '{hit.collider.name}' is on the Interactable layer but is missing the InteractableObject script.");
                ClearSelection();
            }
        }
        else
        {
            // The ray did NOT hit anything on the interactable layer.
            // Let's use RaycastAll to see everything the ray is passing through.
            RaycastHit[] allHits = Physics.RaycastAll(ray, maxDetectionDistance);
            if (allHits.Length > 0)
            {
                Debug.LogWarning($"Raycast MISSED interactable layer. Here's everything it hit in order:");
                foreach (var singleHit in allHits)
                {
                    // This will show you the ground, then the tree, etc.
                    Debug.LogWarning($"-- Hit '{singleHit.collider.name}' on layer '{LayerMask.LayerToName(singleHit.collider.gameObject.layer)}' (Layer {singleHit.collider.gameObject.layer})");
                }
            }
            ClearSelection();
        }
    }

    private void HandleInteractionInput()
    {
        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
        {
            Debug.Log($"Interacted with: {currentInteractable.GetItemName()}");
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