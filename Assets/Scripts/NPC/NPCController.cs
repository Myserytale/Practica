using UnityEngine;
using System.Collections.Generic;

public class NPCController : MonoBehaviour
{
    public string npcName = "Villager";
    [TextArea(3, 10)]
    public List<string> dialogueLines;

    public void Interact()
    {
        DialogueManager.Instance.StartDialogue(this);
    }

    public string GetInteractionText()
    {
        return $"Talk to {npcName}";
    }
}