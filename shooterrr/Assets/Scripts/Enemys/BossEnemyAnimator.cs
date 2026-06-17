using UnityEngine;
using UnityEngine.AI;

public class BossEnemyAnimator : MonoBehaviour
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
            animator.SetTrigger("attack");
            Debug.Log("Boss Attack Trigger вызван!");
        }
    }
    
    public void TriggerTopAttack()
    {
        if (animator != null)
        {
            animator.SetTrigger("TopAttack");
            Debug.Log("Boss TopAttack Trigger вызван!");
        }
    }
    
    public void TriggerHit()
    {
        if (animator != null)
        {
            animator.SetTrigger("hit");
            Debug.Log("Boss Hit Trigger вызван!");
        }
    }
    
    public void TriggerDeath()
    {
        if (animator != null)
        {
            animator.ResetTrigger("attack");
            animator.ResetTrigger("TopAttack");
            animator.ResetTrigger("hit");
            animator.SetTrigger("dying");
            Debug.Log("Boss Death Trigger вызван!");
        }
    }
}