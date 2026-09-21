using UnityEngine;

public class UnitScript : MonoBehaviour
{
    
    void Start()
    {
        UnitSelectionManager.instance.allUnitsList.Add(gameObject);

    }

    private void OnDestroy()
    {
        UnitSelectionManager.instance.allUnitsList.Remove(gameObject);
    }


}
