using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class RPGAttackingState : StateMachineBehaviour
{
    NavMeshAgent agent;
    AttackControlerScript attackControlerScript;
    public float stopAttackingDistance = 12f;
    

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponent<NavMeshAgent>();
        attackControlerScript = animator.GetComponent<AttackControlerScript>();
        Debug.Log($"Attack state entered: {animator.name}");
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        NetworkObject networkObject = animator.GetComponentInParent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            return;

        if (attackControlerScript.targetToAttack == null)
        {
            animator.SetBool("IsAttacking", false);
            animator.SetBool("IsWalking", false);
            if (agent.hasPath) agent.ResetPath();
            return;
        }

        if (animator.GetComponent<UnitMovementScript>().CommandedToMove)
        {
            animator.SetBool("IsAttacking", false);
            return;
        }
        float distanceFromTarget = Vector3.Distance(attackControlerScript.targetToAttack.position, animator.transform.position);
        if (distanceFromTarget > stopAttackingDistance)
        {
            agent.SetDestination(attackControlerScript.targetToAttack.position);
            animator.SetBool("IsAttacking", false);
            animator.SetBool("IsWalking", true);
        }
        else
        {
            // Don't keep refreshing the target destination while in attack range.
            if (agent.hasPath)
                agent.ResetPath();
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsAttacking", true);
        }
    }
}
