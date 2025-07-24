using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Combat Settings")]
    public float attackRange = 3f;
    public float baseDamage = 25f;
    public float attackCooldown = 1f;
    public LayerMask enemyLayer = 256; // Layer 8 for enemies
    
    [Header("Tool Combat")]
    public bool useToolDamage = true;
    public float toolDamageMultiplier = 1.5f;
    
    [Header("Visual Feedback")]
    public GameObject hitEffect;
    public Color damageColor = Color.red;
    
    private Camera playerCamera;
    private float lastAttackTime;
    private Animator animator;
    
    void Start()
    {
        playerCamera = Camera.main;
        animator = GetComponent<Animator>();
        
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }
    }
    
    void Update()
    {
        // Check for attack input
        if (Input.GetMouseButtonDown(0)) // Left click
        {
            TryAttack();
        }
        
        // Alternative: Check for key press
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryMeleeAttack();
        }
    }
    
    void TryAttack()
{
    if (Time.time - lastAttackTime < attackCooldown) return;
    
    Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
    Ray ray = playerCamera.ScreenPointToRay(screenCenter);
    RaycastHit hit;
    
    Debug.DrawRay(ray.origin, ray.direction * attackRange, Color.red, 0.5f);
    
    // Cast against all layers, then filter
    if (Physics.Raycast(ray, out hit, attackRange))
    {
        Debug.Log($"Player attack hit: {hit.collider.name}, Tag: {hit.collider.tag}, Layer: {hit.collider.gameObject.layer}");
        
        // Check if we hit an enemy (Layer 8 or Enemy tag)
        bool hitEnemy = hit.collider.gameObject.layer == 8 || hit.collider.CompareTag("Enemy");
        
        if (hitEnemy)
        {
            HealthSystem enemyHealth = hit.collider.GetComponent<HealthSystem>();
            if (enemyHealth != null)
            {
                float finalDamage = CalculateDamage();
                enemyHealth.TakeDamage(finalDamage, gameObject);
                
                Debug.Log($"Player hit enemy {hit.collider.name} for {finalDamage} damage!");
                lastAttackTime = Time.time;
                
                if (animator != null)
                {
                    animator.SetTrigger("Attack");
                }
            }
            else
            {
                Debug.LogWarning($"Enemy {hit.collider.name} has no HealthSystem component!");
            }
        }
        else
        {
            Debug.Log($"Hit {hit.collider.name} but it's not an enemy");
        }
    }
    else
    {
        Debug.Log("Player attack missed - no object in range");
    }
}
    
    void TryMeleeAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        
        // Sphere cast around player for melee combat
        Collider[] hitEnemies = Physics.OverlapSphere(
            transform.position + transform.forward * (attackRange * 0.5f), 
            attackRange * 0.7f, 
            enemyLayer
        );
        
        if (hitEnemies.Length > 0)
        {
            foreach (Collider enemyCol in hitEnemies)
            {
                HealthSystem enemyHealth = enemyCol.GetComponent<HealthSystem>();
                if (enemyHealth != null && !enemyHealth.IsDead)
                {
                    float finalDamage = CalculateDamage();
                    enemyHealth.TakeDamage(finalDamage, gameObject);
                    
                    Debug.Log($"Melee attack hit {enemyCol.name} for {finalDamage} damage!");
                    
                    // Only attack the first enemy found
                    break;
                }
            }
            
            lastAttackTime = Time.time;
            
            if (animator != null)
            {
                animator.SetTrigger("MeleeAttack");
            }
        }
    }
    
    float CalculateDamage()
    {
        float damage = baseDamage;
        
        if (useToolDamage)
        {
            // Get currently held tool
            InventorySlot selectedSlot = InventoryManager.Instance.GetSelectedToolbeltSlot();
            if (selectedSlot != null && selectedSlot.item != null)
            {
                // Check if it's a weapon/tool
                if (selectedSlot.item.itemType == ItemType.Tool)
                {
                    damage *= toolDamageMultiplier;
                    Debug.Log($"Using tool {selectedSlot.item.itemName} - damage increased!");
                }
            }
        }
        
        return damage;
    }
    
    void ShowHitEffect(Vector3 position)
    {
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, position, Quaternion.identity);
            Destroy(effect, 1f);
        }
        
        // Screen shake effect (optional)
        // CameraShake.Instance.Shake(0.1f, 0.2f);
    }
    
    // Visual debugging
    void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * (attackRange * 0.5f), attackRange * 0.7f);
        
        // Draw raycast attack range
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * attackRange);
    }
}