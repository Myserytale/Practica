using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public Text npcNameText;
    public Text dialogueText;
    public Button continueButton;

    public Button buildCommandButton;

    [Header("Settings")]
    public float maxInteractionDistance = 5f;

    private Queue<string> sentences;
    private NPCController currentNPC;
    private Transform playerTransform;

    public bool IsDialogueActive { get; private set; }

    private void Awake()
    {
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
        if (buildCommandButton != null)
        {
            buildCommandButton.gameObject.SetActive(false);
        }
        IsDialogueActive = false;

        // Find the player GameObject by its tag
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
        // If dialogue is active, check if the player has moved too far away
        if (IsDialogueActive && currentNPC != null)
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

        // Show and unlock the cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        npcNameText.text = npc.npcName;
        sentences.Clear();

        foreach (string sentence in npc.dialogueLines)
        {
            sentences.Enqueue(sentence);
        }

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(DisplayNextSentence);

        if (buildCommandButton != null && npc.GetComponent<AI_Movement>() != null)
        {
            buildCommandButton.gameObject.SetActive(true);
            buildCommandButton.onClick.RemoveAllListeners();
            buildCommandButton.onClick.AddListener(OnBuildCommandClicked);
        }


        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }
        string sentence = sentences.Dequeue();
        dialogueText.text = sentence;
    }

    void EndDialogue()
    {
        IsDialogueActive = false;
        currentNPC = null;
        dialoguePanel.SetActive(false);

        // Hide and lock the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void OnBuildCommandClicked()
    {
        if (currentNPC != null && playerTransform != null)
        {
            // 1. Calculate a target point in front of the player
            Vector3 targetPoint = playerTransform.position + playerTransform.forward * 15f;
            
            Vector3 buildPosition = targetPoint; // Default to the original point if no ground is found

            // 2. Raycast down from high above the target point to find the ground
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(targetPoint.x, 1000f, targetPoint.z), Vector3.down, out hit, 2000f))
            {
                // We hit something. Use this point as the build position.
                // For better accuracy, you could check if hit.collider.CompareTag("Ground")
                buildPosition = hit.point;
                Debug.Log($"Build command ground position found at: {buildPosition}");
            }
            else
            {
                Debug.LogWarning("Could not find a ground position for the build command. Building at default position.");
            }
            
            AI_Movement npcMovement = currentNPC.GetComponent<AI_Movement>();
            if (npcMovement != null)
            {
                npcMovement.GiveBuildCommand(buildPosition);
            }
        }
        EndDialogue(); // Close dialogue after giving command
    }
}