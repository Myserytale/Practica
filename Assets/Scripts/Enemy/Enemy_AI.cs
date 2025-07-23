using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public LayerMask playerLayer = 1;
    
    [Header("Combat")]
    public float damage = 10f;
    public float attackCooldown = 1.5f;
    public float moveSpeed = 3.5f;
    
    [Header("Behavior")]
    public float patrolRadius = 5f;
    public float waitTimeAtPatrolPoint = 2f;
    
    private enum EnemyState { Patrolling, Chasing, Attacking, Dead }
    private EnemyState currentState = EnemyState.Patrolling;
    
    private NavMeshAgent agent;
    private Animator animator;
    private HealthSystem healthSystem;
    private Transform player;
    private Vector3 startPosition;
    private Vector3 patrolTarget;
    private float lastAttackTime;
    private float patrolWaitTimer;
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        healthSystem = GetComponent<HealthSystem>();
        
        startPosition = transform.position;
        agent.speed = moveSpeed;
        
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        
        // Set up health system events
        if (healthSystem != null)
        {
            healthSystem.OnDeath += OnDeath;
        }
        
        SetNewPatrolTarget();
    }
    
    void Update()
    {
        if (currentState == EnemyState.Dead) return;
        
        // Update animator
        if (animator != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
            animator.SetBool("IsAttacking", currentState == EnemyState.Attacking);
        }
        
        switch (currentState)
        {
            case EnemyState.Patrolling:
                HandlePatrolling();
                break;
            case EnemyState.Chasing:
                HandleChasing();
                break;
            case EnemyState.Attacking:
                HandleAttacking();
                break;
        }
        
        CheckForPlayer();
    }
    
    void CheckForPlayer()
    {
        if (player == null || currentState == EnemyState.Dead) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Check if player is in detection range
        if (distanceToPlayer <= detectionRange)
        {
            // Check line of sight
            RaycastHit hit;
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            
            if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out hit, detectionRange, ~playerLayer))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    if (distanceToPlayer <= attackRange)
                    {
                        currentState = EnemyState.Attacking;
                    }
                    else
                    {
                        currentState = EnemyState.Chasing;
                    }
                }
            }
        }
        else if (currentState != EnemyState.Patrolling)
        {
            // Player too far, return to patrol
            currentState = EnemyState.Patrolling;
            SetNewPatrolTarget();
        }
    }
    
    void HandlePatrolling()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= waitTimeAtPatrolPoint)
            {
                SetNewPatrolTarget();
                patrolWaitTimer = 0f;
            }
        }
    }
    
    void HandleChasing()
    {
        if (player != null)
        {
            agent.SetDestination(player.position);
        }
    }
    
    void HandleAttacking()
    {
        if (player == null) return;
        
        // Stop moving and face the player
        agent.isStopped = true;
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        transform.rotation = Quaternion.LookRotation(direction);
        
        // Attack if cooldown is ready
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            Attack();
            lastAttackTime = Time.time;
        }
        
        // Check if player moved out of attack range
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > attackRange)
        {
            agent.isStopped = false;
            currentState = EnemyState.Chasing;
        }
    }
    
    void Attack()
    {
        Debug.Log($"{gameObject.name} attacks player!");
        
        // Trigger attack animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        // Deal damage to player
        HealthSystem playerHealth = player.GetComponent<HealthSystem>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, gameObject);
        }
    }
    
    void SetNewPatrolTarget()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += startPosition;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            patrolTarget = hit.position;
            agent.SetDestination(patrolTarget);
        }
    }
    
    public void OnDeath()
    {
        currentState = EnemyState.Dead;
        agent.isStopped = true;
        
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }
        
        // Disable collider
        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;
        
        Debug.Log($"{gameObject.name} enemy died!");
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw patrol area
        Gizmos.color = Color.blue;
        Vector3 patrolCenter = Application.isPlaying ? startPosition : transform.position;
        Gizmos.DrawWireSphere(patrolCenter, patrolRadius);
    }
}