using UnityEngine;
using UnityEngine.AI;

public class RPGAttackingState : StateMachineBehaviour
{
    NavMeshAgent agent;
    AttackControlerScript attackControlerScript;

    public float stopAttackingDistance = 10.2f;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponent<NavMeshAgent>();
        attackControlerScript = animator.GetComponent<AttackControlerScript>();
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
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

        agent.SetDestination(attackControlerScript.targetToAttack.position);
        float distanceFromTarget = Vector3.Distance(attackControlerScript.targetToAttack.position, animator.transform.position);
        if (distanceFromTarget > stopAttackingDistance)
        {
            animator.SetBool("IsAttacking", false);
            animator.SetBool("IsWalking", true);
        }
    }
}
