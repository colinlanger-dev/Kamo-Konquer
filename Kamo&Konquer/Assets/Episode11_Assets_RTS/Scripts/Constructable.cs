using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Constructable : MonoBehaviour
{
    [SerializeField] private Sprite spriteNormal;
    [SerializeField] private Sprite spriteBeschaedigt;
    private float constHealth;
    public float constMaxHealth;

    NavMeshObstacle obstacle;
    private SpriteRenderer sr;

    private void Start()
    {
        constHealth = constMaxHealth;
        UpdateHealthUI();
    }

    private void Update()
    {
        Console.WriteLine(constHealth);
        
    }

    private void UpdateHealthUI()
    {
        Console.WriteLine(constHealth);
        float prozent = constHealth / constMaxHealth;

        if (prozent <= 0)
        {
            Destroy(gameObject);
        }

        else if (prozent < 0.5f)
        {
            sr.sprite = spriteBeschaedigt;
        }

        Console.WriteLine(prozent);
    }

    public void TakeDamage(int damage)
    {
        constHealth -= damage;
        UpdateHealthUI();
    }

    public void ConstructableWasPlaced()
    {
         ActivateObstacle();
    }

    private void ActivateObstacle()
    {
        obstacle = GetComponentInChildren<NavMeshObstacle>();
        obstacle.enabled = true;
    }
}
