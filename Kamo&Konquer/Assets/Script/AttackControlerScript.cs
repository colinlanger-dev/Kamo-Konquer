using System;
using UnityEngine;

public class AttackControlerScript : MonoBehaviour
{
    public Transform targetToAttack;

    public float health = 10f;
    public float unitDamage = 2f;
    public float attackSpeed = 20f;

    private void OnTriggerEnter(Collider other)
    {
       
            AttackControlerScript otherController = other.GetComponentInParent<AttackControlerScript>();
            bool isAttackableTarget = other.CompareTag("Enemy") || other.CompareTag("Building") ||
                                      (otherController != null && (otherController.CompareTag("Enemy") || otherController.CompareTag("Building")));
            if (isAttackableTarget && targetToAttack == null)
            {
                targetToAttack = otherController != null ? otherController.transform : other.transform;
            }
        
        
    }

    public void ReceiveDamage(float damage)
    {
        health -= damage;
        Debug.Log(health.ToString());
    }

}
