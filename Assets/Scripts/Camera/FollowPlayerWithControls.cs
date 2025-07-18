using UnityEngine;
using UnityEngine.InputSystem;

public class FollowPlayer : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0, 5, -10);
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private bool lookAtTarget = true;
    [SerializeField] private Vector3 lookOffset = Vector3.zero;
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Camera Controls")]
    [SerializeField] private bool enableMouseLook = true;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalLookLimit = 80f;
    [SerializeField] private bool invertY = false;
    
    [Header("Zoom Controls")]
    [SerializeField] private bool enableZoom = true;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 15f;
    
    [Header("Camera Smoothing")]
    [SerializeField] private bool enableSmoothing = true;
    [SerializeField] private float positionSmoothTime = 0.3f;
    [SerializeField] private float rotationSmoothTime = 0.1f;


    
    // Private variables for camera control
    private float mouseX;
    private float mouseY;
    private float currentZoom;
    private Vector3 currentVelocity;
    private Vector3 rotationVelocity;
    private bool isMouseLookActive = false;
    
    void Start()
    {
        // We will now assign the target manually in the inspector
        // instead of finding it by tag.
        if (target == null)
        {
            Debug.LogWarning("FollowPlayer: Target is not assigned. The camera will not function.");
        }
        
        // Initialize zoom
        currentZoom = Vector3.Distance(Vector3.zero, offset);
    }

    void Update()
    {
        // The target should be assigned, so we don't need to search for it anymore.
        if (target != null)
        {
            HandleInput();
            UpdateCameraPosition();
        }
    }
    
    void HandleInput()
    {
        // Handle mouse look input
        if (enableMouseLook)
        {
            HandleMouseLook();
        }
        
        // Handle zoom input
        if (enableZoom)
        {
            HandleZoom();
        }
        
        // Handle cursor lock toggle (Right-click to toggle)
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            ToggleCursorLock();
        }
    }
    
    void HandleMouseLook()
    {
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            
            // Only apply mouse look if cursor is locked or mouse look is always active
            if (Cursor.lockState == CursorLockMode.Locked || isMouseLookActive)
            {
                mouseX += mouseDelta.x * mouseSensitivity * Time.deltaTime;
                mouseY += mouseDelta.y * mouseSensitivity * Time.deltaTime * (invertY ? 1 : -1);
                
                // Clamp vertical rotation
                mouseY = Mathf.Clamp(mouseY, -verticalLookLimit, verticalLookLimit);
            }
        }
    }
    
    void HandleZoom()
    {
        if (Mouse.current != null)
        {
            Vector2 scroll = Mouse.current.scroll.ReadValue();
            if (scroll.y != 0)
            {
                currentZoom -= scroll.y * zoomSpeed * Time.deltaTime;
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            }
        }
    }
    
    void UpdateCameraPosition()
    {
        // Calculate rotation based on mouse input
        Quaternion rotation = Quaternion.Euler(mouseY, mouseX, 0);
        
        // Calculate offset with zoom
        Vector3 zoomedOffset = offset.normalized * currentZoom;
        
        // Apply rotation to offset
        Vector3 rotatedOffset = rotation * zoomedOffset;
        
        // Calculate desired position
        Vector3 desiredPosition = target.position + rotatedOffset;
        
        // Apply position with smoothing
        if (enableSmoothing)
        {
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, positionSmoothTime);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        }
        
        // Handle rotation
        if (lookAtTarget)
        {
            Vector3 lookDirection = (target.position + lookOffset) - transform.position;
            if (lookDirection != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
                
                if (enableSmoothing)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSmoothTime * Time.deltaTime / Time.fixedDeltaTime);
                }
                else
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
                }
            }
        }
    }
    
    void ToggleCursorLock()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            isMouseLookActive = false;
            Debug.Log("Cursor unlocked");
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            isMouseLookActive = true;
            Debug.Log("Cursor locked - Mouse look active");
        }
    }
    
    // Public methods for external control
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    public void SetZoom(float zoom)
    {
        currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
    }
    
    public void ResetCamera()
    {
        mouseX = 0;
        mouseY = 0;
        currentZoom = Vector3.Distance(Vector3.zero, offset);
    }
}
