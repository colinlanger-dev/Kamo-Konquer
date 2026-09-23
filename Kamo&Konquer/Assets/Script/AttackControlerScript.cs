using UnityEngine;

public class AttackControlerScript : MonoBehaviour
{
    public Transform targetToAttack;
    UnitMovementScript unitMovementScript;

    private void Start()
    {
        unitMovementScript = GetComponent<UnitMovementScript>();
    }

    private void OnTriggerEnter(Collider other)
    {
       
            if (other.CompareTag("Enemy") && targetToAttack == null)
            {
                targetToAttack = other.transform;
            }
        
        
    }

    private void OnTriggerExit(Collider other)
    {
        targetToAttack = null;
    }
}
