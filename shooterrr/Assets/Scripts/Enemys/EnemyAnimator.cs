using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimator : MonoBehaviour
{
    public Animator animator;
    
    void Start()
    {
        animator = GetComponent<Animator>();
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
        }
    }
    
    public void TriggerHit()
    {
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
    }
    
    public void TriggerDeath()
    {
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
    }
}