using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRange = 25f;
    public float attackRange = 3f;
    public LayerMask playerLayer = 11;

    [Header("Combat")]
    public float damage = 10f;
    public float attackCooldown = 1.5f;
    public float moveSpeed = 3.5f;

    [Header("Behavior")]
    public float patrolRadius = 55f;
    public float waitTimeAtPatrolPoint = 2f;

    [Header("Advanced Behavior")]
public float lostPlayerGracePeriod = 3f; // Time to wait before giving up on player
public float returnToStartSpeed = 2f; // Slower speed when returning to start position

private float lostPlayerTimer = 0f;
private bool isReturningToStart = false;

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

        agent.stoppingDistance = 0.1f;

        // IMPORTANT: Ensure enemy starts on NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            Debug.Log($"Enemy {gameObject.name} positioned on NavMesh");
        }
        else
        {
            Debug.LogError($"Enemy {gameObject.name} cannot be placed on NavMesh! Check NavMesh baking.");
        }

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log($"Enemy found player: {player.name}");
        }
        else
        {
            Debug.LogError("Enemy cannot find player! Make sure player has 'Player' tag.");
        }

        // Set up health system events
        if (healthSystem != null)
        {
        healthSystem.OnTakeDamage += PlayTakeDamageAnimation;
        }

        //SetNewPatrolTarget();
    }

    void PlayTakeDamageAnimation()
    {
        if (currentState == EnemyState.Dead) return;
        if (animator != null)
            animator.SetTrigger("TakeDamage");
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
            // Player detected - reset lost timer and return to normal behavior
            lostPlayerTimer = 0f;
            isReturningToStart = false;
            agent.speed = moveSpeed; // Restore normal speed

            // Check line of sight
            RaycastHit hit;
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            Vector3 rayStart = transform.position + Vector3.up * 0.5f;

            Debug.DrawRay(rayStart, directionToPlayer * detectionRange, Color.red, 0.1f);

            if (Physics.Raycast(rayStart, directionToPlayer, out hit, detectionRange))
            {
                Debug.Log($"Raycast hit: {hit.collider.name} with tag: {hit.collider.tag}");

                if (hit.collider.CompareTag("Player"))
                {
                    Debug.Log("Player detected!");

                    if (distanceToPlayer <= attackRange)
                    {
                        Debug.Log("Switching to attack state");
                        currentState = EnemyState.Attacking;
                    }
                    else
                    {
                        Debug.Log("Switching to chase state");
                        currentState = EnemyState.Chasing;
                    }
                }
                else
                {
                    Debug.Log($"Line of sight blocked by: {hit.collider.name}");
                }
            }
            else
            {
                Debug.Log("Raycast hit nothing");
            }
        }
        else if (currentState == EnemyState.Chasing || currentState == EnemyState.Attacking)
        {
            // Player out of range - start grace period
            lostPlayerTimer += Time.deltaTime;

            if (lostPlayerTimer >= lostPlayerGracePeriod)
            {
                Debug.Log("Lost player for too long, starting return to patrol area");
                StartReturnToStart();
            }
            else
            {
                Debug.Log($"Player out of range, grace period: {lostPlayerTimer:F1}/{lostPlayerGracePeriod:F1}");
                // Stay in current state during grace period
            }
        }
    }
void StartReturnToStart()
{
    isReturningToStart = true;
    currentState = EnemyState.Patrolling;
    agent.speed = returnToStartSpeed; // Use slower speed
    
    // Set destination to start position instead of random patrol point
    agent.SetDestination(startPosition);
    
    Debug.Log("Enemy returning to start position");
}

    void HandlePatrolling()
{
    if (!agent.pathPending && agent.remainingDistance < 0.5f)
    {
        if (isReturningToStart)
        {
            // Reached start position, now resume normal patrol
            isReturningToStart = false;
            agent.speed = moveSpeed; // Restore normal speed
            Debug.Log("Reached start position, resuming normal patrol");
        }
        
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
        if (player == null) return;

        if (agent.isStopped)
        {
            agent.isStopped = false;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Check attack range first
        if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attacking;
            return;
        }

        // Force movement toward player with manual approach if too far
        if (distanceToPlayer > attackRange + 2f)
        {
            // Use NavMesh for long distance
            agent.SetDestination(player.position);
        }
        else
        {
            // Manual movement for close approach
            Vector3 direction = (player.position - transform.position).normalized;
            Vector3 targetPos = transform.position + direction * agent.speed * Time.deltaTime;

            // Check if target position is on NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, 1f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
        }

        Debug.Log($"Chasing - Distance: {distanceToPlayer:F1}, Velocity: {agent.velocity.magnitude:F1}");
    }

    void HandleAttacking()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Check if player moved out of attack range FIRST
        if (distanceToPlayer > attackRange)
        {
            Debug.Log("Player moved out of attack range, resuming chase");
            agent.isStopped = false;  // Resume movement
            currentState = EnemyState.Chasing;
            return; // Exit early to start chasing
        }

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
    }

    void Attack()
    {
        Debug.Log($"{gameObject.name} attacks player!");

        // Trigger attack animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        if (player != null)
    {
        HealthSystem playerHealth = player.GetComponent<HealthSystem>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, gameObject);
            Debug.Log("Enemy attacked player for " + damage + " damage!");
        }
    }

        // Deal damage to player
        /*HealthSystem playerHealth = player.GetComponent<HealthSystem>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, gameObject);
        }*/
    }

    void SetNewPatrolTarget()
{
    Vector3 targetDirection;
    
    if (isReturningToStart)
    {
        // If returning to start, just set destination to start position
        patrolTarget = startPosition;
        agent.SetDestination(patrolTarget);
        return;
    }
    
    // Normal patrol behavior - but stay closer to start
    float actualPatrolRadius = Mathf.Min(patrolRadius, 10f); // Limit patrol radius
    Vector3 randomDirection = Random.insideUnitSphere * actualPatrolRadius;
    randomDirection += startPosition;

    NavMeshHit hit;
    if (NavMesh.SamplePosition(randomDirection, out hit, actualPatrolRadius, NavMesh.AllAreas))
    {
        patrolTarget = hit.position;
        agent.SetDestination(patrolTarget);
        Debug.Log($"New patrol target set: {patrolTarget}");
    }
    else
    {
        // Fallback to start position if no valid patrol point found
        patrolTarget = startPosition;
        agent.SetDestination(patrolTarget);
        Debug.Log("No valid patrol point found, returning to start");
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

         StartCoroutine(DisappearAfterDeath());
    }
    
    public void TakeDamage(float amount)
    {
        if (currentState == EnemyState.Dead) return;

        // Play take damage animation
        if (animator != null)
        {
            animator.SetTrigger("TakeDamage");
        }

        // Apply damage to health system
        if (healthSystem != null)
        {
            healthSystem.TakeDamage(amount, null); // Or pass attacker if needed
        }
    }

    private IEnumerator DisappearAfterDeath()
    {
        // Wait for death animation (adjust time as needed)
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }

    void OnDrawGizmos()  // Remove "Selected" from the name
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
        
        // Optional: Draw path to patrol target
        if (Application.isPlaying && patrolTarget != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, patrolTarget);
        }
    }

    [ContextMenu("Debug Attack Distance")]
    void DebugAttackDistance()
    {
        if (player != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            Debug.Log($"=== ATTACK DISTANCE DEBUG ===");
            Debug.Log($"Current distance to player: {dist:F2}");
            Debug.Log($"Attack range setting: {attackRange}");
            Debug.Log($"Agent stopping distance: {agent.stoppingDistance}");
            Debug.Log($"Agent remaining distance: {agent.remainingDistance:F2}");
            Debug.Log($"Can attack? {dist <= attackRange}");
            Debug.Log($"Current state: {currentState}");
            
            if (dist > attackRange)
            {
                Debug.Log($"TOO FAR: Need to be {attackRange - dist:F2} units closer");
            }
        }
    }
}