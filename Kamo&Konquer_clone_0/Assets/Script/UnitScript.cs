using UnityEngine;
using UnityEngine.AI;

public class UnitScript : MonoBehaviour
{
    private NavMeshAgent agent;
    private bool isRegistered;

    private void Start()
    {
        TryRegisterWithSelectionManager();
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.updateRotation = false;
    }

    private void Update()
    {
        if (!isRegistered)
            TryRegisterWithSelectionManager();
    }

    private void TryRegisterWithSelectionManager()
    {
        if (UnitSelectionManager.Instance == null)
            return;

        if (!UnitSelectionManager.Instance.allUnitsList.Contains(gameObject))
            UnitSelectionManager.Instance.allUnitsList.Add(gameObject);
        isRegistered = true;
    }

    private void OnDestroy()
    {
        if (UnitSelectionManager.Instance != null)
            UnitSelectionManager.Instance.allUnitsList.Remove(gameObject);
    }
}
