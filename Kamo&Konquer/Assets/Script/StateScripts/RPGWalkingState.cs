using UnityEngine;
using UnityEngine.AI;

public class RPGWalkingState : StateMachineBehaviour
{

    AttackControlerScript attackControlerScript;
    NavMeshAgent agent;
    
    public float attackingDistanz = 1f;
    
    
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        attackControlerScript = animator.GetComponent<AttackControlerScript>();
        agent = animator.GetComponent<NavMeshAgent>();
        
    }

    
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

        if (attackControlerScript.targetToAttack == null)
        {
            animator.SetBool("IsWalking", false);
        }

        agent.SetDestination(attackControlerScript.targetToAttack.position);

        //float distanceFromTarget = Vector3.Distance(attackControlerScript.targetToAttack.position, animator.transform.position);
        /*if(distanceFromTarget < attackingDistanz)
        {
            animator.SetBool("IsAttacking", true);
        }*/

        
    }

    
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent.SetDestination(animator.transform.position);
    }


}
