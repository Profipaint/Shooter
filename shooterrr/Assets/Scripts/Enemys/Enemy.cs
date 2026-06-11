using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 50f;
    private float currentHealth;
    
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float stoppingDistance = 2f;
    public float chaseRange = 15f;
    public float attackRange = 2f;
    private Transform player;
    private NavMeshAgent agent;
    
    [Header("Attack")]
    public float attackDamage = 10f;
    public float attackCooldown = 1f;
    private float nextAttackTime = 0f;
    public Animator animator;
    
    [Header("Effects")]
    public GameObject deathEffect;
    public GameObject hitEffect;
    public AudioClip deathSound;
    public AudioClip attackSound;
    public AudioClip hurtSound;
    private AudioSource audioSource;
    
    [Header("Loot")]
    public GameObject lootItem;
    public int experienceReward = 10; // Можно использовать или удалить
    
    [Header("States")]
    public float detectionRange = 10f;
    private bool isDead = false;
    private EnemyState currentState = EnemyState.Idle;
    
    private enum EnemyState
    {
        Idle,
        Chasing,
        Attacking,
        Dead
    }
    
    void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = stoppingDistance;
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    void Update()
    {
        if (isDead || player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        switch (currentState)
        {
            case EnemyState.Idle:
                if (distanceToPlayer <= detectionRange)
                    currentState = EnemyState.Chasing;
                break;
                
            case EnemyState.Chasing:
                if (distanceToPlayer <= attackRange)
                {
                    currentState = EnemyState.Attacking;
                }
                else if (distanceToPlayer > chaseRange)
                {
                    currentState = EnemyState.Idle;
                    if (agent != null) agent.isStopped = true;
                }
                else
                {
                    ChasePlayer();
                }
                break;
                
            case EnemyState.Attacking:
                if (distanceToPlayer > attackRange)
                {
                    currentState = EnemyState.Chasing;
                }
                else
                {
                    AttackPlayer();
                }
                break;
        }
        
        UpdateAnimations();
    }
    
    void ChasePlayer()
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
            transform.LookAt(player);
        }
    }
    
    void AttackPlayer()
    {
        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            
            if (animator != null)
                animator.SetTrigger("Attack");
                
            if (attackSound != null && audioSource != null)
                audioSource.PlayOneShot(attackSound);
                
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                Debug.Log($"Враг атаковал! Урон: {attackDamage}");
            }
        }
    }
    
    public void TakeDamage(float damage, Vector3? hitPoint = null)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        
        if (hitEffect != null && hitPoint.HasValue)
        {
            Instantiate(hitEffect, hitPoint.Value, Quaternion.identity);
        }
        
        if (hurtSound != null && audioSource != null)
            audioSource.PlayOneShot(hurtSound);
        
        Debug.Log($"Враг получил {damage} урона. Осталось здоровья: {currentHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            currentState = EnemyState.Chasing;
        }
    }
    
    void Die()
    {
        isDead = true;
        currentState = EnemyState.Dead;
        
        Debug.Log("Враг уничтожен!");
        
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);
            
        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound);
            
        if (lootItem != null)
            Instantiate(lootItem, transform.position, Quaternion.identity);
        
        // Закомментировано, так как PlayerExperience может отсутствовать
        /*
        PlayerExperience playerExp = player?.GetComponent<PlayerExperience>();
        if (playerExp != null)
            playerExp.AddExperience(experienceReward);
        */
        
        if (animator != null)
            animator.SetTrigger("Die");
            
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        if (agent != null) agent.isStopped = true;
        
        Destroy(gameObject, 2f);
    }
    
    void UpdateAnimations()
    {
        if (animator == null) return;
        
        float speed = agent != null ? agent.velocity.magnitude : 0;
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsAttacking", currentState == EnemyState.Attacking);
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}