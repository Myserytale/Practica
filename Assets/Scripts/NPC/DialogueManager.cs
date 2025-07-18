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
}