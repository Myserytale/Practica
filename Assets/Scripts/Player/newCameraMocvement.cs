using UnityEngine;

public class newCameraMovement : MonoBehaviour
{
    [Header("Mouse Settings")]
    public float mouseSensitivity = 100f;
    public Transform playerBody; // Drag Player GameObject here
    
    [Header("Look Constraints")]
    public float minLookAngle = -90f;
    public float maxLookAngle = 90f;
    
    [Header("Control")]
    public bool cameraActive = true;
    public KeyCode toggleCursorKey = KeyCode.Escape;
    
    private float xRotation = 0f;
    private float yRotation = 0f;
    
    void Start()
    {
        SetCameraActive(true);
        
        // Auto-find player if not assigned
        if (playerBody == null)
        {
            playerBody = transform.parent; // Camera is child of Player
        }
        
        // Get initial rotation
        if (playerBody != null)
        {
            yRotation = playerBody.eulerAngles.y;
        }
    }
    
    void Update()
    {
        // Toggle cursor
        if (Input.GetKeyDown(toggleCursorKey))
        {
            SetCameraActive(!cameraActive);
        }
        
        if (!cameraActive) return;
        
        // Get raw mouse input (without Time.deltaTime for now)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 0.02f;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 0.02f;
        
        // Accumulate rotations
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minLookAngle, maxLookAngle);
        
        yRotation += mouseX;
        
        // Apply rotations
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        
        if (playerBody != null)
        {
            playerBody.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }
    }
    
    public void SetCameraActive(bool active)
    {
        cameraActive = active;
        
        if (active)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}