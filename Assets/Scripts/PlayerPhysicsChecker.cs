using UnityEngine;

public class PlayerPhysicsChecker : MonoBehaviour
{
    [Header("Physics Components Check")]
    public bool autoFixPhysics = true;
    public bool showDebugInfo = true;
    
    [Header("Player Settings")]
    public float playerMass = 1f;
    public float playerDrag = 0f;
    public float playerAngularDrag = 0.05f;
    public bool freezeRotation = true;
    
    void Start()
    {
        CheckPlayerPhysics();
    }
    
    void CheckPlayerPhysics()
    {
        GameObject player = gameObject;
        
        if (showDebugInfo)
        {
            Debug.Log($"=== Player Physics Check for: {player.name} ===");
        }
        
        // Check for Rigidbody
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null)
        {
            if (autoFixPhysics)
            {
                rb = player.AddComponent<Rigidbody>();
                Debug.Log("✓ Added Rigidbody to player");
            }
            else
            {
                Debug.LogWarning("✗ Player missing Rigidbody component!");
            }
        }
        else
        {
            if (showDebugInfo) Debug.Log("✓ Rigidbody found");
        }
        
        // Check for Collider
        Collider collider = player.GetComponent<Collider>();
        if (collider == null)
        {
            if (autoFixPhysics)
            {
                CapsuleCollider capsule = player.AddComponent<CapsuleCollider>();
                capsule.height = 2f;
                capsule.radius = 0.5f;
                capsule.center = new Vector3(0, 1f, 0);
                Debug.Log("✓ Added CapsuleCollider to player");
            }
            else
            {
                Debug.LogWarning("✗ Player missing Collider component!");
            }
        }
        else
        {
            if (showDebugInfo) Debug.Log($"✓ Collider found: {collider.GetType().Name}");
        }
        
        // Configure Rigidbody if available
        if (rb != null)
        {
            ConfigureRigidbody(rb);
        }
        
        // Check player position relative to terrain
        CheckPlayerPosition();
    }
    
    void ConfigureRigidbody(Rigidbody rb)
    {
        rb.mass = playerMass;
        rb.linearDamping = playerDrag;
        rb.angularDamping = playerAngularDrag;
        
        if (freezeRotation)
        {
            rb.freezeRotation = true;
        }
        
        // Make sure it's not kinematic
        rb.isKinematic = false;
        
        if (showDebugInfo)
        {
            Debug.Log($"✓ Rigidbody configured - Mass: {rb.mass}, Drag: {rb.linearDamping}");
        }
    }
    
    void CheckPlayerPosition()
    {
        Vector3 playerPos = transform.position;
        
        // Raycast down to check for ground
        if (Physics.Raycast(playerPos, Vector3.down, out RaycastHit hit, 100f))
        {
            float distanceToGround = hit.distance;
            
            if (showDebugInfo)
            {
                Debug.Log($"✓ Ground found {distanceToGround:F2} units below player");
                Debug.Log($"Ground object: {hit.collider.gameObject.name}");
                Debug.Log($"Ground layer: {hit.collider.gameObject.layer}");
            }
            
            // If player is too far above ground, move them down
            if (distanceToGround > 5f)
            {
                Vector3 newPos = hit.point + Vector3.up * 2f;
                transform.position = newPos;
                Debug.Log($"✓ Moved player closer to ground: {newPos}");
            }
        }
        else
        {
            Debug.LogWarning("✗ No ground found below player!");
            Debug.LogWarning("Player might be too high or terrain collision is missing");
        }
    }
    
    [ContextMenu("Check Player Physics")]
    public void ManualPhysicsCheck()
    {
        CheckPlayerPhysics();
    }
    
    [ContextMenu("Fix Player Position")]
    public void FixPlayerPosition()
    {
        // Move player to a safe position above terrain
        Terrain terrain = FindFirstObjectByType<Terrain>();
        if (terrain != null)
        {
            Vector3 terrainCenter = terrain.transform.position + terrain.terrainData.size / 2;
            terrainCenter.y = terrain.SampleHeight(terrainCenter) + 2f;
            transform.position = terrainCenter;
            Debug.Log($"✓ Player moved to terrain center: {terrainCenter}");
        }
        else
        {
            transform.position = new Vector3(0, 5, 0);
            Debug.Log("✓ Player moved to safe position: (0, 5, 0)");
        }
    }
    
    void OnDrawGizmos()
    {
        if (showDebugInfo)
        {
            // Draw a line downward to show ground detection
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 10f);
            
            // Draw player bounds
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
    }
}
