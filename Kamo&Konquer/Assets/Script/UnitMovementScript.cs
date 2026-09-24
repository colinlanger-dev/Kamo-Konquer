using UnityEngine;
using UnityEngine.AI;

public class UnitMovementScript : MonoBehaviour
{
    Camera cam;
    NavMeshAgent agent;
    public LayerMask ground;
    AttackControlerScript attackControler;
    Animator animator;
    public bool CommandedToMove;
    

    void Start()
    {
        cam = Camera.main;
        agent = GetComponent<NavMeshAgent>();
        attackControler = GetComponent<AttackControlerScript>();
        animator = GetComponent<Animator>();
    }

    
    void Update()
    {
        if(Input.GetMouseButtonDown(1))
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            
            if (UnitSelectionManager.Instance != null && Physics.Raycast(ray, out hit, Mathf.Infinity, UnitSelectionManager.Instance.attackable))
                return;

            if(Physics.Raycast(ray, out hit, Mathf.Infinity, ground))
            {
                
                attackControler.targetToAttack = null;
                animator.SetBool("IsAttacking", false);
                animator.SetBool("IsWalking", true);
                CommandedToMove = true;
                agent.SetDestination(hit.point);
                
            }
        }

        if (CommandedToMove && !agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance)
        {
            CommandedToMove = false;
            animator.SetBool("IsWalking", false);
        }
    }
}
