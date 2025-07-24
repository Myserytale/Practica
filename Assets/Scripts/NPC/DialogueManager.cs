using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public Text npcNameText;
    public Text dialogueText;
    public Button continueButton;
    public Button buildCommandButton;
    public Button gatherWoodButton;
    public Button gatherStoneButton;
    public Button gatherStickButton;
    public Button assignToChestButton;
    public Button giveChestButton; // New button for giving a chest

    [Header("Settings")]
    public float maxInteractionDistance = 5f;

    private Queue<string> sentences;
    private NPCController currentNPC;
    private Transform playerTransform;

    public bool IsDialogueActive { get; private set; }

    private void Awake()
    {
        // ... existing Awake code ...
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        sentences = new Queue<string>();
        dialoguePanel.SetActive(false);
        // Deactivate all buttons initially
        if (buildCommandButton != null) buildCommandButton.gameObject.SetActive(false);
        if (gatherWoodButton != null) gatherWoodButton.gameObject.SetActive(false);
        if (gatherStoneButton != null) gatherStoneButton.gameObject.SetActive(false);
        if (gatherStickButton != null) gatherStickButton.gameObject.SetActive(false);
        if (assignToChestButton != null) assignToChestButton.gameObject.SetActive(false);
        if (giveChestButton != null) giveChestButton.gameObject.SetActive(false); // New
        IsDialogueActive = false;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("DialogueManager: Player GameObject not found! Make sure your player has the 'Player' tag.");
        }
    }

    private void Update()
    {
        if (IsDialogueActive && currentNPC != null && playerTransform != null)
        {
            float distance = Vector3.Distance(playerTransform.position, currentNPC.transform.position);
            if (distance > maxInteractionDistance)
            {
                EndDialogue();
            }
        }
    }

    public void StartDialogue(NPCController npc)
    {
        currentNPC = npc;
        IsDialogueActive = true;
        dialoguePanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        npcNameText.text = npc.npcName;
        sentences.Clear();
        continueButton.gameObject.SetActive(true);
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(DisplayNextSentence);

        AI_Movement npcAI = npc.GetComponent<AI_Movement>();
        if (npcAI != null)
        {
            // Stage 1: NPC has no chest.
            if (npcAI.assignedChest == null)
            {
                // Check if NPC is currently placing a chest
                var state = npcAI.GetCurrentState();
                if (state == AI_Movement.AIState.MovingToPlacement || state == AI_Movement.AIState.PlacingObject)
                {
                    sentences.Enqueue("I'm placing the chest you gave me. Please wait!");
                    HideAllTaskButtons();
                }
                else
                {
                    sentences.Enqueue("I can't work without a place to store things.");
                    sentences.Enqueue("Could you give me a chest?");
                    SetupGiveChestButton(npcAI);
                }
            }
            // Stage 2: NPC has a chest, ready for commands.
            else
            {
                sentences.Enqueue("I'm ready for my next task.");
                SetupStandardButtons(npcAI);
            }
        }
        else
        {
            foreach (string sentence in npc.dialogueLines) { sentences.Enqueue(sentence); }
        }

        DisplayNextSentence();

        if (CameraManager.Instance.isFirstPerson)
            FindFirstObjectByType<newCameraMovement>()?.SetCameraActive(false);
        else
            FindFirstObjectByType<FollowPlayer>()?.SetCameraActive(false);

    }

    private void SetupGiveChestButton(AI_Movement npcAI)
    {
        HideAllTaskButtons();
        giveChestButton.gameObject.SetActive(true);
        giveChestButton.onClick.RemoveAllListeners();
        giveChestButton.onClick.AddListener(() => OnGiveChestClicked(npcAI));
    }

    private void SetupStandardButtons(AI_Movement npcAI)
    {
        HideAllTaskButtons();
        buildCommandButton.gameObject.SetActive(true);
        buildCommandButton.onClick.RemoveAllListeners();
        buildCommandButton.onClick.AddListener(OnBuildCommandClicked);
        // You can add logic here to show gather buttons immediately if you want
    }

    private void OnGiveChestClicked(AI_Movement npcAI)
    {
        if (InventoryManager.Instance.HasItem("Chest", 1))
        {
            Item chestItem = InventoryManager.Instance.GetItemByName("Chest");
            InventoryManager.Instance.RemoveItem("Chest", 1);
            npcAI.StartChestPlacement(chestItem);
            EndDialogue();
        }
        else
        {
            dialogueText.text = "It seems you don't have a chest to give me.";
            continueButton.gameObject.SetActive(false);
        }
    }

    private void OnBuildCommandClicked()
    {
        if (currentNPC == null) return;
        AI_Movement npcMovement = currentNPC.GetComponent<AI_Movement>();
        if (npcMovement == null) return;

        // Stage 2a: Check for materials
        if (npcMovement.CanBuild())
        {
            // We have materials, assign the build task
            AI_Movement.AITask buildTask = new AI_Movement.AITask
            {
                taskType = AI_Movement.AITaskType.Build,
                targetPosition = playerTransform.position + playerTransform.forward * 15f,
                buildingRecipe = npcMovement.buildingRecipe
            };
            npcMovement.AssignTask(buildTask);
            EndDialogue();
        }
        // Stage 2b: Materials needed
        else
        {
            dialogueText.text = "I need materials for that. Please tell me what to gather.";
            continueButton.gameObject.SetActive(false);
            HideAllTaskButtons();
            gatherWoodButton.gameObject.SetActive(true);
            gatherStoneButton.gameObject.SetActive(true);
            gatherStickButton.gameObject.SetActive(true);
            // Add listeners for the gather buttons
            gatherWoodButton.onClick.AddListener(OnGatherWoodClicked);
            // ... add for stone and stick ...
        }
    }

    private void HideAllTaskButtons()
    {
        if (buildCommandButton != null) buildCommandButton.gameObject.SetActive(false);
        if (gatherWoodButton != null) gatherWoodButton.gameObject.SetActive(false);
        if (gatherStoneButton != null) gatherStoneButton.gameObject.SetActive(false);
        if (gatherStickButton != null) gatherStickButton.gameObject.SetActive(false);
        if (assignToChestButton != null) assignToChestButton.gameObject.SetActive(false);
        if (giveChestButton != null) giveChestButton.gameObject.SetActive(false);
    }

    // ... (DisplayNextSentence, EndDialogue, OnGather...Clicked, Find... methods are mostly unchanged)
// ... existing code from DialogueManager.cs ...
    public void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            continueButton.gameObject.SetActive(false);
            return;
        }
        string sentence = sentences.Dequeue();
        dialogueText.text = sentence;
    }

    void EndDialogue()
    {
        IsDialogueActive = false;
        if (currentNPC != null)
        {
            currentNPC.EndInteraction();
            currentNPC = null;
        }
        dialoguePanel.SetActive(false);
        HideAllTaskButtons();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if (CameraManager.Instance.isFirstPerson)
        FindFirstObjectByType<newCameraMovement>()?.SetCameraActive(true);
    else
        FindFirstObjectByType<FollowPlayer>()?.SetCameraActive(true);
    }

    private void OnAssignToChestClicked()
    {
        if (currentNPC == null) return;
        AI_Movement npcMovement = currentNPC.GetComponent<AI_Movement>();
        if (npcMovement == null) return;

        ChestController closestChest = FindClosestUnassignedChest(currentNPC.transform.position);

        if (closestChest != null)
        {
            npcMovement.assignedChest = closestChest;
            closestChest.assignedNPC = npcMovement;
            Debug.Log($"{currentNPC.npcName} has been assigned to a chest.");
        }
        else
        {
            Debug.LogWarning("No unassigned chests found nearby.");
        }

        EndDialogue();
    }

    private void OnGatherWoodClicked()
{
    if (currentNPC == null) return;
    AI_Movement npcMovement = currentNPC.GetComponent<AI_Movement>();
    if (npcMovement == null) return;

    InteractableObject closestTree = FindClosestResource("Wood");
    if (closestTree == null)
    {
        Debug.LogWarning("No trees found to gather from.");
        
        // Create a random movement destination for testing
        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        Vector3 testDestination = currentNPC.transform.position + randomDirection * 10f;
        
        Debug.Log($"No resources found. Moving to test position: {testDestination}");
        
        AI_Movement.AITask moveTask = new AI_Movement.AITask
        {
            taskType = AI_Movement.AITaskType.Gather,
            targetPosition = testDestination
        };
        
        npcMovement.AssignTask(moveTask);
        EndDialogue();
        return;
    }

    // Create a destination point that's 2 units away from the resource
    Vector3 directionToTree = (closestTree.transform.position - currentNPC.transform.position).normalized;
    Vector3 gatherPosition = closestTree.transform.position - directionToTree * 2f;

    AI_Movement.AITask gatherTask = new AI_Movement.AITask
    {
        taskType = AI_Movement.AITaskType.Gather,
        resourceTarget = closestTree,
        targetPosition = gatherPosition  // Set an explicit position that's offset from the resource
    };

    npcMovement.AssignTask(gatherTask);
    EndDialogue();
}

    private ChestController FindClosestUnassignedChest(Vector3 fromPosition)
    {
        ChestController[] allChests = FindObjectsByType<ChestController>(FindObjectsSortMode.None);
        ChestController closest = null;
        float minDistance = float.MaxValue;

        foreach (var chest in allChests)
        {
            if (chest.assignedNPC == null)
            {
                float distance = Vector3.Distance(fromPosition, chest.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = chest;
                }
            }
        }
        return closest;
    }

    private InteractableObject FindClosestResource(string itemName)
    {
        InteractableObject[] allResources = FindObjectsByType<InteractableObject>(FindObjectsSortMode.None);
        InteractableObject closest = null;
        float minDistance = float.MaxValue;

        foreach (var resource in allResources)
        {
            if (resource.itemToDrop != null && resource.itemToDrop.itemName == itemName)
            {
                float distance = Vector3.Distance(currentNPC.transform.position, resource.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = resource;
                }
            }
        }
        return closest;
    }
}