using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Combat Settings")]
    public float attackRange = 2f;
    public float attackDamage = 25f;
    public float attackCooldown = 1f;
    public LayerMask enemyLayer;
    
    private float lastAttackTime;
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left click to attack
        {
            TryAttack();
        }
    }
    
    void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, attackRange, enemyLayer))
        {
            HealthSystem enemyHealth = hit.collider.GetComponent<HealthSystem>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage, gameObject);
                lastAttackTime = Time.time;
                Debug.Log($"Player attacked {hit.collider.name} for {attackDamage} damage!");
            }
        }
    }
}