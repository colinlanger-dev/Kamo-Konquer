using UnityEngine;
using UnityEngine.AI;

public class UnitScript : MonoBehaviour
{
    NavMeshAgent agent;
    
    void Start()
    {
        UnitSelectionManager.Instance.allUnitsList.Add(gameObject);
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;


    }

    private void OnDestroy()
    {
        UnitSelectionManager.Instance.allUnitsList.Remove(gameObject);
    }


}
