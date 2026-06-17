using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimator : MonoBehaviour
{
    public Animator animator;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator не найден на " + gameObject.name);
        }
        else
        {
            Debug.Log("Animator найден на " + gameObject.name);
        }
    }
    
    void Update()
    {
        if (animator == null) return;
        
        NavMeshAgent agent = GetComponentInParent<NavMeshAgent>();
        float speed = agent != null ? agent.velocity.magnitude : 0;
        
        animator.SetFloat("Speed", speed);
        animator.SetBool("walking", speed > 0.1f);
    }
    
    public void TriggerAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
            Debug.Log("Attack Trigger вызван!");
        }
    }
    
    public void TriggerHit()
    {
        if (animator != null)
        {
            animator.SetTrigger("hit");
            Debug.Log("Hit Trigger вызван!");
        }
    }
    
    public void TriggerDeath()
    {
        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("hit");
            animator.SetTrigger("dying");
            Debug.Log("Death Trigger вызван!");
        }
    }
}