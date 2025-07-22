using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class NPCController : MonoBehaviour
{
    public string npcName = "Villager";
    [TextArea(3, 10)]
    public List<string> dialogueLines;

    private bool isInteracting = false;

    private Animator animator;
    private NavMeshAgent agent;

    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (!isInteracting && agent != null && animator != null)
        {
            Vector3 velocity = agent.velocity;
            Vector3 localVelocity = transform.InverseTransformDirection(velocity);

            animator.SetFloat("MoveX", localVelocity.x);
            animator.SetFloat("MoveZ", localVelocity.z);
        }
    }

    public void Interact()
    {
        isInteracting = true;

        if (agent != null)
        {
            agent.isStopped = true;
        }
        if (animator != null)
        {
            animator.SetBool("IsInteracting", true);
        }

        DialogueManager.Instance.StartDialogue(this);
    }
    
    public void EndInteraction()
    {
        isInteracting = false;

        if (agent != null)
        {
            agent.isStopped = false;
        }
        if (animator != null)
        {
            animator.SetBool("IsInteracting", false);
        }
    }
    public string GetInteractionText()
    {
        return $"Talk to {npcName}";
    }
}