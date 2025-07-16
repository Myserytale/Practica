using UnityEngine;
using UnityEngine.InputSystem; // Add this line to use the Keyboard class

public class NewPlayerMovement : MonoBehaviour
{
[Header("Movement")]
    public CharacterController controller;
    public float speed = 12f;
    public float gravity = -9.81f * 2;
    public float jumpHeight = 3f;
 
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask = 1;
    
    [Header("Auto Setup")]
    public bool autoCreateGroundCheck = true;
    public Vector3 groundCheckOffset = new Vector3(0, -1, 0);
 
    Vector3 velocity;
    bool isGrounded;
    
    // Input variables
    private Vector2 moveInput;
    private bool jumpInput;
    
    void Start()
    {
        // Auto-create ground check if needed
        if (autoCreateGroundCheck && groundCheck == null)
        {
            CreateGroundCheck();
        }
        
        // Ensure we have a CharacterController
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
            if (controller == null)
            {
                Debug.LogError("PlayerMovement: No CharacterController found!");
            }
        }
    }
    
    void Update()
    {
        // Handle input
        HandleInput();
        
        // Ground check with debugging
        PerformGroundCheck();
        
        // Reset falling velocity when grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        // Handle movement
        HandleMovement();
        
        // Handle jumping with proper ground check
        HandleJumping();
        
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        
        // Apply vertical movement
        controller.Move(velocity * Time.deltaTime);
    }
    
    void HandleInput()
    {
        // New Input System - using Keyboard class
        if (Keyboard.current != null)
        {
            // Movement input
            moveInput = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) moveInput.y += 1;
            if (Keyboard.current.sKey.isPressed) moveInput.y -= 1;
            if (Keyboard.current.aKey.isPressed) moveInput.x -= 1;
            if (Keyboard.current.dKey.isPressed) moveInput.x += 1;
            
            // Jump input
            jumpInput = Keyboard.current.spaceKey.wasPressedThisFrame;
        }
    }
    
    void PerformGroundCheck()
    {
        if (groundCheck == null)
        {
            Debug.LogWarning("PlayerMovement: Ground check is null! Cannot detect ground.");
            isGrounded = false;
            return;
        }
        
        // Perform ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        
        // Debug info every 60 frames (once per second at 60fps)
        if (Time.frameCount % 60 == 0)
        {
            //Debug.Log($"Ground Check - Position: {groundCheck.position}, IsGrounded: {isGrounded}, Distance: {groundDistance}");
            
            // Check what we're hitting
            Collider[] colliders = Physics.OverlapSphere(groundCheck.position, groundDistance, groundMask);
            if (colliders.Length > 0)
            {
                //Debug.Log($"Ground objects detected: {colliders.Length}");
                foreach (var col in colliders)
                {
                    //Debug.Log($"- {col.gameObject.name} on layer {col.gameObject.layer}");
                }
            }
            else
            {
                //Debug.Log("No ground objects detected in range");
            }
        }
    }
    
    void HandleMovement()
    {
        // Calculate movement direction
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        
        // Apply movement
        controller.Move(move * speed * Time.deltaTime);
    }
    
    void HandleJumping()
    {
        if (jumpInput)
        {
            if (isGrounded)
            {
                // Calculate jump velocity
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                Debug.Log($"JUMP! Velocity: {velocity.y}");
            }
            else
            {
                Debug.Log("Can't jump - not grounded!");
            }
        }
    }
    
    void CreateGroundCheck()
    {
        GameObject groundCheckObj = new GameObject("GroundCheck");
        groundCheckObj.transform.SetParent(transform);
        groundCheckObj.transform.localPosition = groundCheckOffset;
        groundCheck = groundCheckObj.transform;
        
        Debug.Log($"Created GroundCheck at local position: {groundCheckOffset}");
    }
    
    // Context menu for debugging
    [ContextMenu("Debug Ground Check")]
    void DebugGroundCheck()
    {
        if (groundCheck == null)
        {
            Debug.LogError("Ground Check is null!");
            return;
        }
        
        Debug.Log($"=== GROUND CHECK DEBUG ===");
        Debug.Log($"Ground Check Position: {groundCheck.position}");
        Debug.Log($"Ground Check Local Position: {groundCheck.localPosition}");
        Debug.Log($"Ground Distance: {groundDistance}");
        Debug.Log($"Ground Mask: {groundMask}");
        Debug.Log($"Is Grounded: {isGrounded}");
        
        // Check what's in the sphere
        Collider[] colliders = Physics.OverlapSphere(groundCheck.position, groundDistance, groundMask);
        Debug.Log($"Objects in ground check sphere: {colliders.Length}");
        
        foreach (var col in colliders)
        {
            Debug.Log($"- {col.gameObject.name} (Layer: {col.gameObject.layer})");
        }
    }
    
    [ContextMenu("Fix Ground Check Position")]
    void FixGroundCheckPosition()
    {
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }
        
        if (controller != null)
        {
            float capsuleBottom = controller.center.y - (controller.height * 0.5f);
            Vector3 newOffset = new Vector3(0, capsuleBottom - 0.1f, 0);
            
            if (groundCheck != null)
            {
                groundCheck.localPosition = newOffset;
            }
            else
            {
                groundCheckOffset = newOffset;
                CreateGroundCheck();
            }
            
            Debug.Log($"Fixed ground check position to: {newOffset}");
        }
    }
    
    // Visual debugging
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}
