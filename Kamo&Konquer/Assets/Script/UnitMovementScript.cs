using UnityEngine;
using UnityEngine.AI;

public class UnitMovementScript : MonoBehaviour
{
    Camera cam;
    NavMeshAgent agent;
    public LayerMask ground;
    public LayerMask Enemy;
    AttackControlerScript attackControler;
    public bool CommandedToMove;
    

    void Start()
    {
        cam = Camera.main;
        agent = GetComponent<NavMeshAgent>();
        attackControler = GetComponent<AttackControlerScript>();
    }

    
    void Update()
    {
        if(Input.GetMouseButton(1))
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(ray, out hit, Mathf.Infinity, ground))
            {
                CommandedToMove = true;
                agent.SetDestination(hit.point);
                
            }
        }

        if (agent.hasPath == false || agent.remainingDistance <= agent.stoppingDistance)
        {
            CommandedToMove = false;
        }
    }
}
