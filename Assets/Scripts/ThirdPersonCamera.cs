using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // The player to follow
    
    [Header("Camera Distance")]
    public float distance = 10f; // Distance from target
    public float minDistance = 2f;
    public float maxDistance = 20f;
    
    [Header("Camera Height")]
    public float height = 5f; // Height above target
    public float minHeight = 1f;
    public float maxHeight = 15f;
    
    [Header("Camera Rotation")]
    public float rotationSpeed = 2f;
    public float maxRotationSpeed = 5f;
    
    [Header("Smoothing")]
    public float positionSmoothing = 10f;
    public float rotationSmoothing = 5f;
    
    [Header("Input Settings")]
    public bool enableMouseControl = true;
    public float mouseSensitivity = 2f;
    
    private float currentRotationY = 0f;
    private float currentRotationX = 0f;
    
    void Start()
    {
        // Find player if not assigned
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
        
        // Set initial rotation
        if (target != null)
        {
            Vector3 angles = transform.eulerAngles;
            currentRotationY = angles.y;
            currentRotationX = angles.x;
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        HandleMouseInput();
        UpdateCameraPosition();
    }
    
    void HandleMouseInput()
    {
        if (!enableMouseControl) return;
        
        // Get mouse input (works with New Input System)
        float mouseX = 0f;
        float mouseY = 0f;
        
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            Vector2 mouseDelta = UnityEngine.InputSystem.Mouse.current.delta.ReadValue();
            mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
            mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;
        }
        
        // Update rotation
        currentRotationY += mouseX;
        currentRotationX -= mouseY;
        currentRotationX = Mathf.Clamp(currentRotationX, -60f, 60f);
    }
    
    void UpdateCameraPosition()
    {
        // Calculate desired position
        Quaternion rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
        Vector3 direction = rotation * Vector3.back;
        
        Vector3 desiredPosition = target.position + Vector3.up * height + direction * distance;
        
        // Smooth position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmoothing * Time.deltaTime);
        
        // Look at target
        Vector3 lookTarget = target.position + Vector3.up * (height * 0.5f);
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing * Time.deltaTime);
    }
    
    // Method to adjust camera distance (useful for zoom)
    public void AdjustDistance(float delta)
    {
        distance = Mathf.Clamp(distance + delta, minDistance, maxDistance);
    }
    
    // Method to adjust camera height
    public void AdjustHeight(float delta)
    {
        height = Mathf.Clamp(height + delta, minHeight, maxHeight);
    }
}
