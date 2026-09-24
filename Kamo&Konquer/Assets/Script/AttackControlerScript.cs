using UnityEngine;

public class AttackControlerScript : MonoBehaviour
{
    public Transform targetToAttack;

    public Material idleStateMaterial;
    public Material walkStateMaterial;
    public Material attackStateMaterial;


    private void OnTriggerEnter(Collider other)
    {
       
            if (other.CompareTag("Enemy") && targetToAttack == null)
            {
                targetToAttack = other.transform;
            }
        
        
    }

    private void OnTriggerExit(Collider other)
    {
        if (targetToAttack != null && (other.transform == targetToAttack || other.transform.IsChildOf(targetToAttack)))
            targetToAttack = null;
    }



}
