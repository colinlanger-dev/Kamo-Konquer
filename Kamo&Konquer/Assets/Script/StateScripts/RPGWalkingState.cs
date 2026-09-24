using UnityEngine;
using UnityEngine.AI;

public class RPGWalkingState : StateMachineBehaviour
{

    AttackControlerScript attackControlerScript;
    NavMeshAgent agent;
    
    public float attackingDistance = 1f;
    
    
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        attackControlerScript = animator.GetComponent<AttackControlerScript>();
        agent = animator.GetComponent<NavMeshAgent>();
        
    }

    
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

        UnitMovementScript movement = animator.GetComponent<UnitMovementScript>();
        if (attackControlerScript.targetToAttack == null && !movement.CommandedToMove)
        {
            if (agent.hasPath)
                agent.ResetPath();
            animator.SetBool("IsWalking", false);
        }
        else
        {
            if(!movement.CommandedToMove)
            {
                agent.SetDestination(attackControlerScript.targetToAttack.position);

                float distanceFromTarget = Vector3.Distance(attackControlerScript.targetToAttack.position, animator.transform.position);
                bool destinationReached = !agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.1f;
                if(distanceFromTarget < attackingDistance || destinationReached)
                {
                    agent.SetDestination(animator.transform.position);
                    animator.SetBool("IsAttacking", true);
                }
            }
        }

           

        
    }

    
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
    }


}
