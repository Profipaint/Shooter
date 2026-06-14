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
    
    [Header("Animation")]
    public EnemyAnimator enemyAnimator;  // Ссылка на скрипт анимаций
    
    [Header("Effects")]
    public GameObject deathEffect;
    public GameObject hitEffect;
    public AudioClip deathSound;
    public AudioClip attackSound;
    public AudioClip hurtSound;
    private AudioSource audioSource;
    
    [Header("Loot")]
    public GameObject lootItem;
    public int experienceReward = 10;
    
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
        
        // Находим аниматор врага, если не назначен
        if (enemyAnimator == null)
            enemyAnimator = GetComponentInChildren<EnemyAnimator>();
        
        if (enemyAnimator != null)
        {
            Debug.Log("EnemyAnimator найден и подключен");
        }
        else
        {
            Debug.LogWarning("EnemyAnimator не найден на дочернем объекте!");
        }
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
            
            // АНИМАЦИЯ АТАКИ
            if (enemyAnimator != null)
            {
                enemyAnimator.TriggerAttack();
                Debug.Log("TriggerAttack вызван!");
            }
            else
            {
                Debug.LogWarning("enemyAnimator == null, анимация атаки не будет проиграна");
            }
            
            // Звук атаки
            if (attackSound != null && audioSource != null)
                audioSource.PlayOneShot(attackSound);
            
            // Нанесение урона игроку
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
        
        // АНИМАЦИЯ ПОЛУЧЕНИЯ УРОНА
        if (enemyAnimator != null)
        {
            enemyAnimator.TriggerHit();
            Debug.Log("TriggerHit вызван!");
        }
        
        // Эффект попадания
        if (hitEffect != null && hitPoint.HasValue)
        {
            Instantiate(hitEffect, hitPoint.Value, Quaternion.identity);
        }
        
        // Звук получения урона
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
        
        // АНИМАЦИЯ СМЕРТИ
        if (enemyAnimator != null)
        {
            enemyAnimator.TriggerDeath();
            Debug.Log("TriggerDeath вызван!");
        }
        
        // Эффект смерти
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        
        // Звук смерти
        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound);
        
        // Выпадение лута
        if (lootItem != null)
            Instantiate(lootItem, transform.position, Quaternion.identity);
        
        // Отключаем коллайдер
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        // Отключаем NavMeshAgent
        if (agent != null) agent.isStopped = true;
        
        // Уничтожаем врага через 2 секунды (чтобы анимация смерти успела проиграться)
        Destroy(gameObject, 2f);
    }
    
    // Метод для анимационного события (можно вызвать из анимации атаки)
    public void OnAttackHit()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                Debug.Log($"Удар из Animation Event! Урон: {attackDamage}");
            }
        }
    }
    
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
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