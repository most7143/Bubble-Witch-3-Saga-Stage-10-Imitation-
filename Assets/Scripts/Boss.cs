using UnityEngine;

public class Boss : MonoBehaviour
{

    public Animator Anim;

    public BubbleSpawner BubbleSpawner;


    public float MaxHealth = 100;


    public float CurrentHealth;
    void Start()
    {
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(float damage)
    {
        CurrentHealth -= damage;
        if(CurrentHealth <= 0)
        {
            Anim.SetTrigger("Die");
        }
        else
        {
            Anim.SetTrigger("Hit");
        }
    }


    public void SpawnBubble()
    {
        Anim.SetTrigger("Attack");
    }
    
}
