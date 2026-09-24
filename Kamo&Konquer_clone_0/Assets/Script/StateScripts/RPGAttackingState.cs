using UnityEngine;
using UnityEngine.AI;

public class RPGAttackingState : StateMachineBehaviour
{
    NavMeshAgent agent;
    AttackControlerScript attackControlerScript;
    float nextAttackTime;

    public float stopAttackingDistance = 12f;
    

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponent<NavMeshAgent>();
        attackControlerScript = animator.GetComponent<AttackControlerScript>();
        Debug.Log($"Attack state entered: {animator.name}");
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
        if (Time.time >= nextAttackTime)
        {
            float damage = attackControlerScript.unitDamage;
            AttackControlerScript targetController = attackControlerScript.targetToAttack.GetComponentInParent<AttackControlerScript>();
            if (targetController != null) targetController.ReceiveDamage(damage);
            Debug.Log("Damage sollte kommen");
            nextAttackTime = Time.time + attackControlerScript.attackSpeed;
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
