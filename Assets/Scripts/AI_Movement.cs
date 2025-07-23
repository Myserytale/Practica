using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
public class AI_Movement : MonoBehaviour
{
    // --- Enums and Structs for Task Management ---
    public enum AITaskType { Idle, PlaceObject, Build, Gather }
    public enum AIState { Idle, WaitingForInput, MovingToPlacement, PlacingObject, MovingToTarget, PerformingTask, MovingToChest, FinishedTask }

    [System.Serializable]
    public struct AITask
    {
        public AITaskType taskType;
        public Vector3 targetPosition;
        public InteractableObject resourceTarget;
        public CraftingRecipe buildingRecipe;
        public GameObject objectToPlacePrefab;
    }

    // --- Public Fields ---
    [Header("AI State")]
    public AIState currentState = AIState.Idle;
    public AIState GetCurrentState() => currentState;
    private AITask currentTask;

    [Header("NPC Connections")]
    public ChestController assignedChest;
    public Item heldItem;

    [Header("Building")]
    public GameObject objectToBuildPrefab;
    public CraftingRecipe buildingRecipe;
    public LayerMask obstructionLayers;
    public float maxGroundIncline = 5f;
    [Tooltip("Maximum distance from original position to search for build spots")]
    public int placementSearchRadius = 10;
    [Tooltip("Number of points to try per search radius ring")]
    public int pointsPerRing = 8;

    [Header("Obstacle Handling")]
    public bool obstaclesDetected = false;
    public List<GameObject> detectedObstacles = new List<GameObject>();
    public float obstacleNotificationRange = 15f; // Distance to alert player
    private bool hasAlertedPlayer = false;

    // --- Private Fields ---
    private NavMeshAgent agent;
    private Animator animator;
    private List<InventorySlot> npcBackpack = new List<InventorySlot>();
    private const int BACKPACK_SIZE = 5;
    private float taskTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component missing!");
            enabled = false;
            return;
        }

        agent.isStopped = false;
        for (int i = 0; i < BACKPACK_SIZE; i++)
        {
            npcBackpack.Add(new InventorySlot());
        }
    }

    void Update()
    {
        if (animator) animator.SetBool("isRunning", agent.velocity.magnitude > 0.1f);

        switch (currentState)
        {
            case AIState.Idle:
            case AIState.WaitingForInput:
                break;
            case AIState.MovingToPlacement:
                HandleMovingToPlacement();
                break;
            case AIState.PlacingObject:
                HandlePlacingObject();
                break;
            case AIState.MovingToTarget:
                HandleMovingToTarget();
                break;
            case AIState.PerformingTask:
                HandlePerformingTask();
                break;
            case AIState.MovingToChest:
                HandleMovingToChest();
                break;
            case AIState.FinishedTask:
                Debug.Log("Task finished, returning to Idle.");
                currentState = AIState.Idle;
                break;
        }
    }
    private void OnDrawGizmos()
    {
        if (currentState == AIState.MovingToTarget && currentTask.taskType == AITaskType.Build)
        {
            // Draw a yellow sphere at the target position
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(currentTask.targetPosition, 0.5f);

            // Draw a line from NPC to target
            Gizmos.color = Color.cyan;
            if (agent != null && agent.hasPath)
            {
                Gizmos.DrawLine(transform.position, agent.destination);
            }
        }
    }

    // --- Task Assignment & Checks ---

    public void StartChestPlacement(Item chestItem)
    {
        if (chestItem == null || chestItem.objectPrefab == null) return;

        Vector3 placementPos = FindValidPlacement(transform.position);
        AssignTask(new AITask
        {
            taskType = AITaskType.PlaceObject,
            objectToPlacePrefab = chestItem.objectPrefab,
            targetPosition = placementPos
        });
    }

    public bool CanBuild()
    {
        if (assignedChest == null) return false;
        return assignedChest.HasRecipeIngredients(buildingRecipe);
    }

    public void AssignTask(AITask newTask)
    {
        ResetAgent();
        currentTask = newTask;
        Debug.Log($"New task assigned: {newTask.taskType}");

        if (newTask.taskType == AITaskType.PlaceObject)
        {
            agent.SetDestination(newTask.targetPosition);
            currentState = AIState.MovingToPlacement;
        }
        else if (newTask.taskType == AITaskType.Build)
        {
            assignedChest.ConsumeRecipeIngredients(newTask.buildingRecipe);
            Debug.Log("Materials consumed from chest.");
            if (objectToBuildPrefab == null)
            {
                Debug.LogError("No building prefab assigned! Cannot build.");
                RefundMaterials();
                currentState = AIState.Idle;
                return;
            }
            agent.SetDestination(newTask.targetPosition);
            currentState = AIState.MovingToTarget;
        }
        else if (newTask.taskType == AITaskType.Gather)
        {
            if (newTask.resourceTarget == null)
            {
                Vector3 moveTarget = newTask.targetPosition;
                if (moveTarget == Vector3.zero)
                {
                    Vector3 randomDirection = Random.insideUnitSphere * 10f;
                    randomDirection.y = 0;
                    moveTarget = transform.position + randomDirection;
                }

                NavMeshHit hit;
                if (NavMesh.SamplePosition(moveTarget, out hit, 5f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                    currentState = AIState.MovingToTarget;
                }
                else
                {
                    Debug.LogWarning("Could not find valid NavMesh position for movement.");
                    currentState = AIState.Idle;
                }
                return;
            }
                    
        Debug.Log($"Setting build destination to: {newTask.targetPosition}");
        Debug.Log($"Current NPC position: {transform.position}");
        Debug.Log($"Distance to target: {Vector3.Distance(transform.position, newTask.targetPosition)}");
        
        agent.SetDestination(newTask.targetPosition);
        currentState = AIState.MovingToTarget;
        
        Debug.Log($"NavMesh agent path status: {agent.pathStatus}");
    

            Item requiredTool = newTask.resourceTarget.requiredTool;
            if (requiredTool != null && heldItem != requiredTool)
            {
                if (assignedChest != null && assignedChest.HasItem(requiredTool, 1))
                {
                    agent.SetDestination(assignedChest.transform.position);
                    currentState = AIState.MovingToChest;
                }
                else
                {
                    currentState = AIState.WaitingForInput;
                    Debug.LogWarning($"Cannot gather, missing tool: {requiredTool.itemName}");
                }
            }
            else
            {
                Vector3 targetPos = newTask.targetPosition;
                if (targetPos == Vector3.zero)
                {
                    Vector3 dirToResource = (newTask.resourceTarget.transform.position - transform.position).normalized;
                    targetPos = newTask.resourceTarget.transform.position - dirToResource * 2f;
                }

                NavMeshHit hit;
                if (NavMesh.SamplePosition(targetPos, out hit, 5f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                    currentState = AIState.MovingToTarget;
                }
                else
                {
                    Debug.LogWarning("Could not find valid NavMesh position near resource.");
                    currentState = AIState.Idle;
                }
            }
        }
    }

    // --- State Handlers ---

    private void HandleMovingToPlacement()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            Debug.Log("Arrived at placement location.");
            currentState = AIState.PlacingObject;
        }
    }

    private void HandlePlacingObject()
    {
        GameObject placedObj = Instantiate(currentTask.objectToPlacePrefab, transform.position, Quaternion.identity);
        ChestController newChest = placedObj.GetComponent<ChestController>();
        if (newChest != null)
        {
            assignedChest = newChest;
            newChest.assignedNPC = this;
            Debug.Log("Placed and assigned new chest.");
        }
        currentState = AIState.FinishedTask;
    }

private void HandleMovingToTarget()
{
    bool hasReachedDestination = false;
    
    if (currentTask.taskType == AITaskType.Build)
    {
        Debug.Log($"HandleMovingToTarget - Build task. Remaining distance: {agent.remainingDistance}, Has path: {agent.hasPath}");
        
        // Simple check: if we're close to the target position
        float distanceToTarget = Vector3.Distance(transform.position, currentTask.targetPosition);
        if (distanceToTarget <= 2f || (!agent.hasPath && agent.remainingDistance <= 1f))
        {
            hasReachedDestination = true;
            Debug.Log($"Build destination reached! Distance to target: {distanceToTarget}");
        }
    }
    else
    {
        // Your existing logic for other task types
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.5f))
            {
                hasReachedDestination = true;
            }
            else if (currentTask.resourceTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, currentTask.resourceTarget.transform.position);
                if (distanceToTarget <= 3f)
                {
                    hasReachedDestination = true;
                }
            }
            else if (!agent.hasPath && agent.remainingDistance < 1f)
            {
                hasReachedDestination = true;
            }
        }
    }

    if (hasReachedDestination)
    {
        Debug.Log($"Arrived at target for task: {currentTask.taskType}");
        currentState = AIState.PerformingTask;
        taskTimer = 0f;
        agent.isStopped = true;
        Debug.Log("State changed to PerformingTask - Building should start now!");
    }
}

    private void HandleMovingToChest()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            // ...existing code...
            if (currentTask.taskType == AITaskType.Gather && currentTask.resourceTarget != null && heldItem != currentTask.resourceTarget.requiredTool)
            {
                Item toolToFetch = currentTask.resourceTarget.requiredTool;
                if (assignedChest != null && assignedChest.RemoveItem(toolToFetch, 1))
                {
                    heldItem = toolToFetch;
                    Debug.Log($"Fetched {toolToFetch.itemName} from chest.");
                    agent.SetDestination(currentTask.resourceTarget.transform.position);
                    currentState = AIState.MovingToTarget;
                }
                else
                {
                    Debug.LogError($"Tool {toolToFetch?.itemName ?? "NULL"} was not in chest upon arrival. Aborting task.");
                    currentState = AIState.Idle;
                }
            }
            else
            {
                DepositItemsToChest();
                currentState = AIState.FinishedTask;
            }
        }
    }

    private void HandlePerformingTask()
    {
        taskTimer += Time.deltaTime;

        if (currentTask.taskType == AITaskType.Build)
        {
            Debug.Log("Starting building placement...");
            FindValidPlacementAndBuild(currentTask.targetPosition);
            Debug.Log("Building placement complete!");

            currentState = AIState.FinishedTask;
        }
        else if (currentTask.taskType == AITaskType.Gather)
        {
            if (currentTask.resourceTarget != null)
            {
                Vector3 direction = (currentTask.resourceTarget.transform.position - transform.position).normalized;
                direction.y = 0;
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            }

            if (animator != null)
            {
                animator.SetBool("isGathering", true);
            }

            if (taskTimer >= 3f)
            {
                if (animator != null)
                {
                    animator.SetBool("isGathering", false);
                }

                if (currentTask.resourceTarget != null)
                {
                    currentTask.resourceTarget.Interact(heldItem);
                    AddItemToBackpack(currentTask.resourceTarget.itemToDrop, 1);
                    Debug.Log($"Gathered {currentTask.resourceTarget.itemToDrop.itemName}.");
                }

                if (assignedChest != null)
                {
                    Debug.Log("Moving to chest to deposit items.");
                    agent.isStopped = false;
                    agent.SetDestination(assignedChest.transform.position);
                    currentState = AIState.MovingToChest;
                }
                else
                {
                    currentState = AIState.FinishedTask;
                }
            }
        }
    }

    // --- Improved Build Placement Logic ---

    private void FindValidPlacementAndBuild(Vector3 position)
    {
        // Clear previous obstacle data
        obstaclesDetected = false;
        detectedObstacles.Clear();
        hasAlertedPlayer = false;

        Vector3 buildPosition;
        bool found = TryFindBuildPosition(position, out buildPosition);

        if (found)
        {
            Instantiate(objectToBuildPrefab, buildPosition, Quaternion.identity);
            Debug.Log($"Building placed at: {buildPosition}");
            currentState = AIState.FinishedTask;
        }
        else if (obstaclesDetected)
        {
            // Don't refund yet - wait for player to clear obstacles
            Debug.LogWarning($"Obstacles detected ({detectedObstacles.Count}). Waiting for player to clear area.");

            // Display alert to player and change to waiting state
            AlertPlayerAboutObstacles();
            currentState = AIState.WaitingForInput;

            // Check periodically if obstacles are cleared
            StartCoroutine(CheckObstaclesClearedRoutine(position));
        }
        else
        {
            RefundMaterials();
            Debug.LogWarning("No valid build spot found. Materials refunded.");
            currentState = AIState.Idle;
        }
    }

    private bool DetectObstacles(Vector3 position, float checkRadius)
    {
        detectedObstacles.Clear();

        Collider col = objectToBuildPrefab.GetComponent<Collider>();
        if (col == null) return false;

        Vector3 bounds = col.bounds.size / 2f;

        // Find all colliders in the area
        Collider[] hitColliders = Physics.OverlapBox(
            position + new Vector3(0, bounds.y, 0),
            bounds * 1.1f,
            Quaternion.identity,
            obstructionLayers
        );

        if (hitColliders.Length > 0)
        {
            foreach (var hitCol in hitColliders)
            {
                if (hitCol.gameObject != this.gameObject &&
                    !detectedObstacles.Contains(hitCol.gameObject))
                {
                    detectedObstacles.Add(hitCol.gameObject);
                }
            }

            obstaclesDetected = detectedObstacles.Count > 0;
            return obstaclesDetected;
        }

        return false;
    }

    private void AlertPlayerAboutObstacles()
    {
        if (hasAlertedPlayer) return;

        // Find player within notification range
        Collider[] colliders = Physics.OverlapSphere(transform.position, obstacleNotificationRange);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Player"))
            {
                // You'll need to implement a UI notification system or dialogue system
                // This is just an example placeholder
                Debug.Log("<color=yellow>NPC says: I can't build here! There are obstacles in the way.</color>");

                // You could trigger UI elements, animations, or sounds here
                // Example: UIManager.Instance.ShowNotification("Clear the obstacles so I can build!");

                hasAlertedPlayer = true;
                break;
            }
        }
    }
    private System.Collections.IEnumerator CheckObstaclesClearedRoutine(Vector3 position)
    {
        while (obstaclesDetected && currentState == AIState.WaitingForInput)
        {
            yield return new WaitForSeconds(3f); // Check every 3 seconds

            // Re-check for obstacles
            if (!DetectObstacles(position, 1f))
            {
                Debug.Log("Obstacles have been cleared! Resuming building...");
                currentState = AIState.PerformingTask;

                // Try to build again
                FindValidPlacementAndBuild(position);
                yield break;
            }
        }
    }

    private bool TryFindBuildPosition(Vector3 center, out Vector3 buildPosition)
    {
        // First try the requested position
        if (IsPlacementValid(center, out buildPosition))
        {
            Debug.Log("Building placed at original position");
            return true;
        }

        // Spiral search pattern around center point
        float radius = 1f;
        int attempts = 0;
        const int maxAttempts = 40;

        while (attempts < maxAttempts)
        {
            float angleStep = 2 * Mathf.PI / pointsPerRing;

            for (int i = 0; i < pointsPerRing; i++)
            {
                attempts++;
                if (attempts >= maxAttempts) break;

                float angle = angleStep * i;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Vector3 tryPos = center + offset;

                if (IsPlacementValid(tryPos, out buildPosition))
                {
                    Debug.Log($"Found valid position after {attempts} attempts");
                    return true;
                }
            }

            radius += 1.5f;
            if (radius > placementSearchRadius) break;
        }

        Debug.LogError($"Failed to find build position after {attempts} attempts");
        buildPosition = Vector3.zero;
        return false;
    }

    private bool IsPlacementValid(Vector3 position, out Vector3 adjustedPosition)
    {
        adjustedPosition = position;

        // Ground detection
        if (!Physics.Raycast(position + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f))
        {
            Debug.Log("Placement failed: No ground found");
            return false;
        }

        adjustedPosition = hit.point;

        // Check for obstacles and update the obstaclesDetected flag
        if (DetectObstacles(adjustedPosition, 1f))
        {
            Debug.Log($"Placement failed: {detectedObstacles.Count} obstacles detected");
            return false;
        }

        // Incline check
        Collider col = objectToBuildPrefab.GetComponent<Collider>();
        if (col == null)
        {
            Debug.Log("Placement failed: No collider on prefab");
            return false;
        }

        Vector3 bounds = col.bounds.size / 2f;

        Vector3[] samplePoints = new Vector3[4] {
        adjustedPosition + new Vector3(bounds.x, 0, bounds.z),
        adjustedPosition + new Vector3(-bounds.x, 0, bounds.z),
        adjustedPosition + new Vector3(bounds.x, 0, -bounds.z),
        adjustedPosition + new Vector3(-bounds.x, 0, -bounds.z)
    };

        float highest = float.MinValue;
        float lowest = float.MaxValue;

        foreach (var p in samplePoints)
        {
            if (Physics.Raycast(p + Vector3.up * 10f, Vector3.down, out RaycastHit h, 20f))
            {
                highest = Mathf.Max(highest, h.point.y);
                lowest = Mathf.Min(lowest, h.point.y);
            }
        }

        if (highest - lowest > maxGroundIncline)
        {
            Debug.Log($"Placement failed: Incline too steep ({highest - lowest:F1} > {maxGroundIncline})");
            return false;
        }

        // NavMesh check
        if (!NavMesh.SamplePosition(adjustedPosition, out NavMeshHit navHit, 1f, NavMesh.AllAreas))
        {
            Debug.Log("Placement failed: Not on NavMesh");
            return false;
        }

        return true;
    }

    private void RefundMaterials()
    {
        if (assignedChest != null && currentTask.buildingRecipe != null)
        {
            foreach (var ingredient in currentTask.buildingRecipe.ingredients)
            {
                assignedChest.AddItem(ingredient.item, ingredient.quantity);
                Debug.Log($"Refunded {ingredient.quantity} {ingredient.item.itemName}");
            }
        }
    }

    // --- Utility Methods ---

    private Vector3 FindValidPlacement(Vector3 center)
    {
        Vector3 desiredPos = center + transform.forward * 2f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(desiredPos, out hit, 5f, NavMesh.AllAreas))
        {
            Debug.Log($"Found valid NavMesh position at {hit.position}");
            return hit.position;
        }

        if (NavMesh.SamplePosition(center, out hit, 1f, NavMesh.AllAreas))
        {
            Debug.Log($"Using current position as placement point");
            return hit.position;
        }

        Debug.LogError($"No valid NavMesh position found near {center}!");
        return center;
    }

    private void AddItemToBackpack(Item item, int quantity)
    {
        foreach (var slot in npcBackpack) { if (slot.item == item) { slot.AddQuantity(quantity); return; } }
        foreach (var slot in npcBackpack) { if (slot.item == null) { slot.item = item; slot.quantity = quantity; return; } }
    }

    private void DepositItemsToChest()
    {
        foreach (var slot in npcBackpack)
        {
            if (slot.item != null && slot.quantity > 0)
            {
                assignedChest.AddItem(slot.item, slot.quantity);
                Debug.Log($"Deposited {slot.quantity} {slot.item.itemName} to chest.");
                slot.item = null;
                slot.quantity = 0;
            }
        }
    }

    private void ResetAgent()
    {
        if (agent == null) return;

        agent.ResetPath();
        agent.isStopped = false;
        agent.updatePosition = true;
        agent.updateRotation = true;

        Debug.Log("Agent reset and ready for new path");
    }

    [ContextMenu("Debug Agent State")]
    public void DebugAgentState()
    {
        if (agent == null) return;

        Debug.Log($"=== AGENT DEBUG INFO ===");
        Debug.Log($"Current State: {currentState}");
        Debug.Log($"Has Path: {agent.hasPath}");
        Debug.Log($"Path Pending: {agent.pathPending}");
        Debug.Log($"Remaining Distance: {agent.remainingDistance}");
        Debug.Log($"Stopping Distance: {agent.stoppingDistance}");
        Debug.Log($"Is Stopped: {agent.isStopped}");
        Debug.Log($"Velocity: {agent.velocity.magnitude}");

        if (currentTask.resourceTarget != null)
        {
            float distToResource = Vector3.Distance(transform.position, currentTask.resourceTarget.transform.position);
            Debug.Log($"Distance to Resource: {distToResource}");
            Debug.Log($"Resource Position: {currentTask.resourceTarget.transform.position}");
            Debug.Log($"Agent Position: {transform.position}");
        }
    }
    [ContextMenu("Force Build House")]
public void ForceBuild()
{
    Debug.Log("Force building house...");
    
    if (objectToBuildPrefab == null)
    {
        Debug.LogError("No building prefab assigned!");
        return;
    }
    
    Vector3 buildPos = transform.position + transform.forward * 3f;
    Instantiate(objectToBuildPrefab, buildPos, Quaternion.identity);
    Debug.Log("House built by force command!");
}
}