using UnityEngine;

public class UnitScript : MonoBehaviour
{
    
    void Start()
    {
        UnitSelectionManager.Instance.allUnitsList.Add(gameObject);

    }

    private void OnDestroy()
    {
        UnitSelectionManager.Instance.allUnitsList.Remove(gameObject);
    }


}
