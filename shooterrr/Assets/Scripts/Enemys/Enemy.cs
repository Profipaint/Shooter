using UnityEngine;
using UnityEngine.AI;
using System.Collections;

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
    public float attackDelay = 0.4f;
    public float attackDuration = 0.8f;
    public float missChance = 0.2f;
    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    
    [Header("Animation")]
    public EnemyAnimator enemyAnimator;
    
    [Header("Effects")]
    public GameObject deathEffect;
    public GameObject hitEffect;
    public GameObject missEffect;
    public AudioClip deathSound;
    public AudioClip attackSound;
    public AudioClip hurtSound;
    public AudioClip missSound;
    private AudioSource audioSource;
    
    [Header("Loot")]
    public GameObject lootPrefab;
    public int lootAmount = 5;
    public float lootDropChance = 1f;
    
    [Header("States")]
    public float detectionRange = 10f;
    public bool enableDebugLogs = true;
    private bool isDead = false;
    private bool isTakingHit = false;
    private bool blockAttackAfterHit = false;
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
        
        if (enemyAnimator == null)
            enemyAnimator = GetComponentInChildren<EnemyAnimator>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        if (enableDebugLogs)
        {
            Debug.Log($"=== СТРУКТУРА ВРАГА ===");
            Debug.Log($"Корневой объект: {transform.root.name}");
            Debug.Log($"Дочерний объект (модель): {transform.name}");
        }
    }
    
    void Update()
    {
        if (isDead || player == null) return;
        if (currentState == EnemyState.Dead) return;
        
        if (isTakingHit || isAttacking)
        {
            if (agent != null) agent.isStopped = true;
            return;
        }
        
        if (blockAttackAfterHit)
        {
            if (agent != null) agent.isStopped = false;
            ChasePlayer();
            return;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        switch (currentState)
        {
            case EnemyState.Idle:
                if (distanceToPlayer <= detectionRange)
                    currentState = EnemyState.Chasing;
                break;
                
            case EnemyState.Chasing:
                if (distanceToPlayer <= attackRange && !isAttacking && !blockAttackAfterHit)
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
                if (distanceToPlayer > attackRange && !isAttacking)
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
        if (isAttacking || isTakingHit) 
        {
            if (agent != null) agent.isStopped = true;
            return;
        }
        
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
        if (blockAttackAfterHit) return;
        
        if (Time.time >= nextAttackTime && !isAttacking && !isTakingHit)
        {
            nextAttackTime = Time.time + attackCooldown;
            isAttacking = true;
            
            if (agent != null) agent.isStopped = true;
            
            if (enemyAnimator != null) enemyAnimator.TriggerAttack();
            if (attackSound != null && audioSource != null) audioSource.PlayOneShot(attackSound);
            
            bool isMiss = Random.value < missChance;
            
            if (isMiss)
            {
                if (missSound != null && audioSource != null) audioSource.PlayOneShot(missSound);
                if (missEffect != null && player != null)
                {
                    Vector3 missPosition = player.position + Random.insideUnitSphere * 1f;
                    Instantiate(missEffect, missPosition, Quaternion.identity);
                }
                StartCoroutine(EndAttackAfterDelay(attackDuration));
            }
            else
            {
                Invoke(nameof(DelayedDamage), attackDelay);
                StartCoroutine(EndAttackAfterDelay(attackDuration));
            }
        }
    }
    
    void DelayedDamage()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= attackRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(attackDamage);
        }
        else
        {
            if (missEffect != null)
            {
                Vector3 groundPos = transform.position + transform.forward * 1.5f;
                groundPos.y = 0;
                Instantiate(missEffect, groundPos, Quaternion.identity);
            }
        }
    }
    
    IEnumerator EndAttackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndAttack();
    }
    
    void EndAttack()
    {
        isAttacking = false;
        if (agent != null && !isDead && !isTakingHit && currentState == EnemyState.Attacking)
        {
            agent.isStopped = false;
            currentState = EnemyState.Chasing;
        }
    }
    
    public void TakeDamage(float damage, Vector3? hitPoint = null)
    {
        if (isDead) return;
        if (isTakingHit) return;
        
        currentHealth -= damage;
        
        if (enableDebugLogs) Debug.Log($"Враг: получил урон {damage}. Осталось: {currentHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
            return;
        }
        
        if (isAttacking)
        {
            StopAllCoroutines();
            EndAttack();
        }
        
        blockAttackAfterHit = true;
        StartCoroutine(HitAnimationSequence());
        
        if (enemyAnimator != null) enemyAnimator.TriggerHit();
        
        if (hitEffect != null && hitPoint.HasValue)
            Instantiate(hitEffect, hitPoint.Value, Quaternion.identity);
        
        if (hurtSound != null && audioSource != null)
            audioSource.PlayOneShot(hurtSound);
        
        StartCoroutine(ReturnToChaseAfterHit());
    }
    
    IEnumerator HitAnimationSequence()
    {
        isTakingHit = true;
        if (agent != null) agent.isStopped = true;
        yield return new WaitForSeconds(0.6f);
        isTakingHit = false;
    }
    
    IEnumerator ReturnToChaseAfterHit()
    {
        yield return new WaitForSeconds(0.3f);
        blockAttackAfterHit = false;
        
        if (!isDead && !isAttacking)
        {
            currentState = EnemyState.Chasing;
            if (agent != null) agent.isStopped = false;
        }
    }
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        currentState = EnemyState.Dead;
        
        Debug.Log($"Враг: УМИРАЮ!");
        
        // 1. ОСТАНАВЛИВАЕМ ВСЕ КОРОУТИНЫ
        StopAllCoroutines();
        
        // 2. ОТКЛЮЧАЕМ КОМПОНЕНТЫ НА КОРНЕ
        Transform root = transform.root;
        
        NavMeshAgent rootAgent = root.GetComponent<NavMeshAgent>();
        if (rootAgent != null)
        {
            rootAgent.isStopped = true;
            rootAgent.enabled = false;
        }
        
        Collider rootCollider = root.GetComponent<Collider>();
        if (rootCollider != null) rootCollider.enabled = false;
        
        // 3. СТАВИМ КОРЕНЬ НА y = 0
        Vector3 groundPos = root.position;
        groundPos.y = 0f;
        root.position = groundPos;
        
        Debug.Log($"Корень на y = 0: {root.position}");
        
        // 4. СБРАСЫВАЕМ ФЛАГИ
        isTakingHit = false;
        isAttacking = false;
        blockAttackAfterHit = false;
        
        // 5. АНИМАЦИЯ СМЕРТИ
        if (enemyAnimator != null)
        {
            enemyAnimator.animator.ResetTrigger("Attack");
            enemyAnimator.animator.ResetTrigger("Hit");
            enemyAnimator.TriggerDeath();
            Debug.Log("Анимация смерти запущена");
        }
        
        // 6. ЭФФЕКТЫ
        if (deathEffect != null)
            Instantiate(deathEffect, root.position, Quaternion.identity);
        
        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound);
        
        // 7. ЛУТ - НЕ СПАВНИМ СРАЗУ, А ЧЕРЕЗ КОРОУТИНУ
        StartCoroutine(DestroyAndDropLoot());
    }
    
    IEnumerator DestroyAndDropLoot()
    {
        // Ждем 3 секунды (пока идет анимация смерти)
        yield return new WaitForSeconds(3f);
        
        // 1. СПАВНИМ ЛУТ
        if (lootPrefab != null && Random.value <= lootDropChance)
        {
            Vector3 rootPos = transform.root.position;
            Vector3 lootPosition = new Vector3(rootPos.x, 0.05f, rootPos.z);
            Instantiate(lootPrefab, lootPosition, Quaternion.identity);
            
            if (enableDebugLogs) Debug.Log($"Лут спавнен на: {lootPosition}");
        }
        
        // 2. УНИЧТОЖАЕМ КОРЕНЬ
        Transform root = transform.root;
        Destroy(root.gameObject);
        
        if (enableDebugLogs) Debug.Log("Враг уничтожен");
    }
    
    void DropLoot()
    {
        // Метод больше не используется, весь код перенесен в DestroyAndDropLoot
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