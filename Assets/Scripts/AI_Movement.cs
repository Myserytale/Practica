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
        public GameObject objectToPlacePrefab; // New: For placing objects like chests
    }

    // --- Public Fields ---
    [Header("AI State")]
    public AIState currentState = AIState.Idle;
    public AIState GetCurrentState()
    {
        return currentState;
    }
    private AITask currentTask;

    [Header("NPC Connections")]
    public ChestController assignedChest;
    public Item heldItem;

    [Header("Building")]
    public GameObject objectToBuildPrefab;
    public CraftingRecipe buildingRecipe;
    public LayerMask obstructionLayers;
    public float maxGroundIncline = 5f;
    public int placementSearchRadius = 10;

    // --- Private Fields ---
    private NavMeshAgent agent;
    private Animator animator;
    private List<InventorySlot> npcBackpack = new List<InventorySlot>();
    private const int BACKPACK_SIZE = 5;
    private float taskTimer;

    void Start()
{
    // Initialize the agent reference first!
    agent = GetComponent<NavMeshAgent>();
    animator = GetComponent<Animator>();  // You should also initialize this here
    
    // Now check if it was found
    if (agent != null)
    {
        agent.isStopped = false;
        agent.updatePosition = true;
        agent.updateRotation = true;
    }
    else
    {
        Debug.LogError("NavMeshAgent component missing!");
    }
    
    for (int i = 0; i < BACKPACK_SIZE; i++) { npcBackpack.Add(new InventorySlot()); }
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

    // --- Task Assignment & Checks ---

    // New: Called by DialogueManager when player gives the NPC a chest item
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

    // New: Called by DialogueManager to check if building is possible
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
            // Material check is now done in DialogueManager before calling this
            assignedChest.ConsumeRecipeIngredients(newTask.buildingRecipe);
            Debug.Log("Materials consumed from chest.");
            agent.SetDestination(newTask.targetPosition);
            currentState = AIState.MovingToTarget;
        }
        // In the AssignTask method, modify this section:
        else if (newTask.taskType == AITaskType.Gather)
        {
            if (newTask.resourceTarget == null)
            {
                // No resource target, just use the provided position or create a random one
                Vector3 moveTarget = newTask.targetPosition;
                if (moveTarget == Vector3.zero)
                {
                    // Generate a random position if none was provided
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

            // Normal case with a resource target
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
                // Use the explicitly provided position if available, otherwise calculate one
                Vector3 targetPos = newTask.targetPosition;
                if (targetPos == Vector3.zero)
                {
                    // Calculate a position 2 units away from the resource
                    Vector3 dirToResource = (newTask.resourceTarget.transform.position - transform.position).normalized;
                    targetPos = newTask.resourceTarget.transform.position - dirToResource * 2f;
                }

                // Ensure the position is on the NavMesh
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
        // Instantiate the chest, then assign it to self
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

    // ... (HandleMovingToTarget, HandleMovingToChest, HandlePerformingTask are mostly unchanged)
    // ... (Helper methods are unchanged)
    // ... existing code from AI_Movement.cs ...
    private void HandleMovingToTarget()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            Debug.Log("Arrived at target.");
            currentState = AIState.PerformingTask;
            taskTimer = 0f;
        }
    }

    private void HandleMovingToChest()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (currentTask.taskType == AITaskType.Gather && heldItem != currentTask.resourceTarget.requiredTool)
            {
                Item toolToFetch = currentTask.resourceTarget.requiredTool;
                if (assignedChest.RemoveItem(toolToFetch, 1))
                {
                    heldItem = toolToFetch;
                    Debug.Log($"Fetched {toolToFetch.itemName} from chest.");
                    agent.SetDestination(currentTask.resourceTarget.transform.position);
                    currentState = AIState.MovingToTarget;
                }
                else
                {
                    Debug.LogError($"Tool {toolToFetch.itemName} was not in chest upon arrival. Aborting task.");
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
            FindValidPlacementAndBuild(currentTask.targetPosition);
            currentState = AIState.FinishedTask;
        }
        else if (currentTask.taskType == AITaskType.Gather)
        {
            if (taskTimer >= 3f) // 3 seconds to gather
            {
                if (currentTask.resourceTarget != null)
                {
                    currentTask.resourceTarget.Interact(heldItem);
                    AddItemToBackpack(currentTask.resourceTarget.itemToDrop, 1);
                    Debug.Log($"Gathered {currentTask.resourceTarget.itemToDrop.itemName}.");
                }

                if (assignedChest != null)
                {
                    Debug.Log("Moving to chest to deposit items.");
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
    private void FindValidPlacementAndBuild(Vector3 position)
    {
        if (IsPlacementValid(position, out Vector3 groundPos))
        {
            Instantiate(objectToBuildPrefab, groundPos - new Vector3(0, 0.5f, 0), Quaternion.identity);
            Debug.Log("Build complete!");
        }
        else
        {
            Debug.LogWarning("Could not find valid build spot at target.");
        }
    }
    private Vector3 FindValidPlacement(Vector3 center)
    {
        // Get a position in front of the NPC
        Vector3 desiredPos = center + transform.forward * 2f;

        // Check if position is on NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(desiredPos, out hit, 5f, NavMesh.AllAreas))
        {
            Debug.Log($"Found valid NavMesh position at {hit.position}");
            return hit.position;
        }

        // If not on NavMesh, try the current position
        if (NavMesh.SamplePosition(center, out hit, 1f, NavMesh.AllAreas))
        {
            Debug.Log($"Using current position as placement point");
            return hit.position;
        }

        Debug.LogError($"No valid NavMesh position found near {center}!");
        return center; // Last resort
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
    private bool IsPlacementValid(Vector3 position, out Vector3 groundPos)
    {
        groundPos = position;
        if (!Physics.Raycast(position + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f)) return false;
        groundPos = hit.point;
        Collider col = objectToBuildPrefab.GetComponent<Collider>();
        if (!col) return false;
        Vector3 bounds = col.bounds.size / 2f;
        if (Physics.CheckBox(groundPos + col.bounds.center, bounds, Quaternion.identity, obstructionLayers)) return false;
        Vector3[] samplePoints = new Vector3[4] { groundPos + new Vector3(bounds.x, 0, bounds.z), groundPos + new Vector3(-bounds.x, 0, bounds.z), groundPos + new Vector3(bounds.x, 0, -bounds.z), groundPos + new Vector3(-bounds.x, 0, -bounds.z) };
        float highest = float.MinValue, lowest = float.MaxValue;
        foreach (var p in samplePoints) { if (Physics.Raycast(p + Vector3.up * 10f, Vector3.down, out RaycastHit h, 20f)) { highest = Mathf.Max(highest, h.point.y); lowest = Mathf.Min(lowest, h.point.y); } }
        if (highest - lowest > maxGroundIncline) return false;
        return true;
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
}

