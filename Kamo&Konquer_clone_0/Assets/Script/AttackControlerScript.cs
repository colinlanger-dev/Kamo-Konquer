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
       
            if (other.CompareTag("Enemy") && targetToAttack == null)
            {
                AttackControlerScript enemyController = other.GetComponentInParent<AttackControlerScript>();
                targetToAttack = enemyController != null ? enemyController.transform : other.transform;
            }
        
        
    }

    public void ReceiveDamage(float damage)
    {
        health -= damage;
        Debug.Log(health.ToString());
    }

}
