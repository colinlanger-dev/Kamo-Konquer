using UnityEngine;
using UnityEngine.AI;

public class UnitMovementScript : MonoBehaviour
{
    Camera cam;
    NavMeshAgent agent;
    public LayerMask ground;
    public LayerMask Enemy;
    AttackControlerScript attackControler;
    

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
                agent.SetDestination(hit.point);
                
            }
        }
    }
}
