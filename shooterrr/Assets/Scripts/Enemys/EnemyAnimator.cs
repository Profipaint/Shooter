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
        
        // Получаем скорость с родительского NavMeshAgent
        NavMeshAgent agent = GetComponentInParent<NavMeshAgent>();
        float speed = agent != null ? agent.velocity.magnitude : 0;
        
        // Устанавливаем параметры
        animator.SetFloat("Speed", speed);
        
        if (speed > 0.1f)
            animator.SetBool("walking", true);
        else
            animator.SetBool("walking", false);
    }
    
    public void TriggerAttack()
    {
        if (animator != null)
            animator.SetTrigger("Attack");
    }
    
    // ДОБАВЛЕН МЕТОД TriggerHit
    public void TriggerHit()
    {
        if (animator != null)
        {
            if (HasParameter("Hit"))
                animator.SetTrigger("Hit");
        }
    }
    
    // ДОБАВЛЕН МЕТОД TriggerDeath
    public void TriggerDeath()
    {
        if (animator != null)
        {
            if (HasParameter("Die"))
                animator.SetTrigger("Die");
        }
    }
    
    // Проверка существования параметра
    bool HasParameter(string paramName)
    {
        foreach (var param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }
}