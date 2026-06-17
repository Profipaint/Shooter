using UnityEngine;
using UnityEngine.AI;

public class BossEnemyAnimator : MonoBehaviour
{
    public Animator animator;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            Debug.LogError("Animator not found on " + gameObject.name);
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
            animator.SetTrigger("attack"); // ← МАЛЕНЬКАЯ
    }
    
    public void TriggerTopAttack()
    {
        if (animator != null)
            animator.SetTrigger("TopAttack");
    }
    
    public void TriggerHit()
    {
        if (animator != null)
            animator.SetTrigger("hit"); // ← МАЛЕНЬКАЯ
    }
    
    public void TriggerDeath()
    {
        if (animator != null)
        {
            animator.ResetTrigger("attack");
            animator.ResetTrigger("TopAttack");
            animator.ResetTrigger("hit");
            animator.SetTrigger("dying");
        }
    }
}