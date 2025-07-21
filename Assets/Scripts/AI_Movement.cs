using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class AI_Movement : MonoBehaviour
{
    public enum AIState { Wandering, MovingToCommand, Building }

    [Header("AI Settings")]
    public AIState currentState = AIState.Wandering;
    public float wanderRadius = 10f;
    public float wanderTimer = 5f;
    public float commandTimeout = 15f;

    [Header("Building")]
    public GameObject objectToBuildPrefab;
    public LayerMask obstructionLayers;
    public float maxGroundIncline = 5f;
    public int placementSearchRadius = 10;
    public float spacing = 1f;

    private NavMeshAgent agent;
    private Animator animator;
    private float timer;
    private float commandTimer;
    private Vector3 commandPosition;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        timer = wanderTimer;
    }

    void Update()
    {
        if (animator) animator.SetBool("isRunning", agent.velocity.magnitude > 0.1f);

        switch (currentState)
        {
            case AIState.Wandering:
                HandleWandering();
                break;
            case AIState.MovingToCommand:
                HandleMovingToCommand();
                break;
        }
    }

    private void HandleWandering()
    {
        timer += Time.deltaTime;
        if (timer >= wanderTimer)
        {
            Vector3 newPos = GetRandomNavMeshLocation(transform.position, wanderRadius);
            agent.SetDestination(newPos);
            timer = 0f;
        }
    }

    private void HandleMovingToCommand()
    {
        commandTimer += Time.deltaTime;

        bool hasArrived = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
        bool pathInvalid = agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid;
        bool hasTimedOut = commandTimer >= commandTimeout;

        if (hasArrived)
        {
            StartCoroutine(FindValidPlacementAndBuild());
            currentState = AIState.Wandering;
        }
        else if (pathInvalid || hasTimedOut)
        {
            Debug.LogWarning($"{name}: Path failed or timed out. Returning to wandering.");
            currentState = AIState.Wandering;
        }
    }

    private IEnumerator FindValidPlacementAndBuild()
    {
        if (!objectToBuildPrefab) yield break;

        Collider prefabCollider = objectToBuildPrefab.GetComponent<Collider>();
        if (!prefabCollider)
        {
            Debug.LogError("Missing Collider on objectToBuildPrefab.");
            yield break;
        }

        Vector3 buildPos = Vector3.zero;
        bool found = false;

        int searchPoints = placementSearchRadius * placementSearchRadius;
        for (int r = 0; r < placementSearchRadius && !found; r++)
        {
            float angleStep = 360f / (r * 8 + 1);
            for (int i = 0; i < r * 8 + 1; i++)
            {
                float angle = i * angleStep;
                Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * r * spacing;
                Vector3 testPos = commandPosition + offset;

                if (NavMesh.SamplePosition(testPos, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                {
                    if (IsPlacementValid(hit.position, out Vector3 groundPos))
                    {
                        buildPos = groundPos - new Vector3(0, 0.5f, 0); // Sink slightly
                        found = true;
                        break;
                    }
                }
            }
            yield return null; // Avoid frame stutter on large spirals
        }

        if (found)
        {
            Debug.Log($"{name} found a build location at {buildPos}");
            Instantiate(objectToBuildPrefab, buildPos, Quaternion.identity);
            yield return new WaitForSeconds(1f); // Simulate build time
            Debug.Log($"{name} completed building at {buildPos}");
        }
        else
        {
            Debug.LogWarning($"{name} couldn't find valid build spot.");
        }
        commandTimer = 0f; // Reset command timer
        currentState = AIState.Wandering; // Return to wandering state
    }

    private bool IsPlacementValid(Vector3 position, out Vector3 groundPos)
    {
        groundPos = position;

        if (!Physics.Raycast(position + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f))
            return false;

        groundPos = hit.point;
        Collider col = objectToBuildPrefab.GetComponent<Collider>();
        if (!col) return false;

        Vector3 bounds = col.bounds.size / 2f;
        if (Physics.CheckBox(groundPos + col.bounds.center, bounds, Quaternion.identity, obstructionLayers))
            return false;

        Vector3[] samplePoints = new Vector3[4]
        {
            groundPos + new Vector3(bounds.x, 0, bounds.z),
            groundPos + new Vector3(-bounds.x, 0, bounds.z),
            groundPos + new Vector3(bounds.x, 0, -bounds.z),
            groundPos + new Vector3(-bounds.x, 0, -bounds.z)
        };

        float highest = float.MinValue, lowest = float.MaxValue;
        foreach (var p in samplePoints)
        {
            if (Physics.Raycast(p + Vector3.up * 10f, Vector3.down, out RaycastHit h, 20f))
            {
                highest = Mathf.Max(highest, h.point.y);
                lowest = Mathf.Min(lowest, h.point.y);
            }
        }

        if (highest - lowest > maxGroundIncline) return false;

        return true;
    }

    public void GiveBuildCommand(Vector3 position)
    {
        commandPosition = position;

        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            commandTimer = 0f;
            currentState = AIState.MovingToCommand;
        }
        else
        {
            Debug.LogWarning($"{name}: Cannot path to command position!");
        }
    }

    private Vector3 GetRandomNavMeshLocation(Vector3 origin, float distance)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 rand = origin + Random.insideUnitSphere * distance;
            if (NavMesh.SamplePosition(rand, out NavMeshHit hit, distance, NavMesh.AllAreas))
                return hit.position;
        }
        return origin;
    }
}
