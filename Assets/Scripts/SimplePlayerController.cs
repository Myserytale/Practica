using UnityEngine;
using UnityEngine.InputSystem;

public class SimplePlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float jumpForce = 8f;
    
    [Header("Ground Check")]
    public float groundCheckDistance = 0.1f;
    public LayerMask groundMask = 1;
    
    private Vector3 velocity;
    private bool isGrounded;
    private float gravity = -9.81f;
    private CharacterController characterController;
    
    void Start()
    {
        Debug.Log($"SimplePlayerController started on: {gameObject.name}");
        Debug.Log($"Initial position: {transform.position}");
        Debug.Log($"Speed: {speed}");
        
        // Try to get CharacterController, add one if not present
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            Debug.Log("Added CharacterController component");
        }
    }
    
    void Update()
    {
        HandleGroundCheck();
        HandleMovement();
        HandleJump();
        HandleGravity();
    }
    
    void HandleGroundCheck()
    {
        // Check if player is on ground
        isGrounded = Physics.CheckSphere(transform.position, groundCheckDistance, groundMask);
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative value to stay grounded
        }
    }
    
    void HandleMovement()
    {
        Vector2 moveInput = Vector2.zero;
        
        // Get input using New Input System
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) moveInput.y += 1;
            if (Keyboard.current.sKey.isPressed) moveInput.y -= 1;
            if (Keyboard.current.aKey.isPressed) moveInput.x -= 1;
            if (Keyboard.current.dKey.isPressed) moveInput.x += 1;
        }
        
        // Convert to 3D movement
        Vector3 movement = new Vector3(moveInput.x, 0, moveInput.y) * speed * Time.deltaTime;
        
        // Apply movement
        characterController.Move(movement);
        
        // Debug movement
        if (moveInput != Vector2.zero)
        {
            Debug.Log($"Movement: {movement}, Position: {transform.position}");
        }
    }
    
    void HandleJump()
    {
        // Check for jump input (Space key)
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            velocity.y = jumpForce;
            Debug.Log("Jump!");
        }
    }
    
    void HandleGravity()
    {
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        
        // Apply vertical movement
        characterController.Move(velocity * Time.deltaTime);
    }
}
