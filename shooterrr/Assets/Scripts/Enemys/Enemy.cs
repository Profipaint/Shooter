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
            Debug.Log($"Враг {name} появился!");
    }
    
    void Update()
    {
        if (isDead || player == null) return;
        
        if (isTakingHit) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        switch (currentState)
        {
            case EnemyState.Idle:
                if (distanceToPlayer <= detectionRange)
                {
                    currentState = EnemyState.Chasing;
                    if (enableDebugLogs) Debug.Log($"Враг {name}: перешел в режим ПРЕСЛЕДОВАНИЯ");
                }
                break;
                
            case EnemyState.Chasing:
                if (distanceToPlayer <= attackRange && !isAttacking)
                {
                    currentState = EnemyState.Attacking;
                    if (enableDebugLogs) Debug.Log($"Враг {name}: перешел в режим АТАКИ");
                }
                else if (distanceToPlayer > chaseRange)
                {
                    currentState = EnemyState.Idle;
                    if (agent != null) agent.isStopped = true;
                    if (enableDebugLogs) Debug.Log($"Враг {name}: перешел в режим ОЖИДАНИЯ");
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
                    if (enableDebugLogs) Debug.Log($"Враг {name}: игрок убежал, снова ПРЕСЛЕДУЮ");
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
        if (Time.time >= nextAttackTime && !isAttacking && !isTakingHit)
        {
            nextAttackTime = Time.time + attackCooldown;
            isAttacking = true;
            
            if (agent != null)
                agent.isStopped = true;
            
            if (enemyAnimator != null)
                enemyAnimator.TriggerAttack();
            
            if (attackSound != null && audioSource != null)
                audioSource.PlayOneShot(attackSound);
            
            bool isMiss = Random.value < missChance;
            
            if (isMiss)
            {
                if (enableDebugLogs) Debug.Log($"Враг {name}: ПРОМАХ!");
                
                if (missSound != null && audioSource != null)
                    audioSource.PlayOneShot(missSound);
                
                if (missEffect != null && player != null)
                {
                    Vector3 missPosition = player.position + Random.insideUnitSphere * 1f;
                    Instantiate(missEffect, missPosition, Quaternion.identity);
                }
                
                StartCoroutine(EndAttackAfterDelay(attackDuration));
            }
            else
            {
                if (enableDebugLogs) Debug.Log($"Враг {name}: АТАКУЮ!");
                
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
            {
                playerHealth.TakeDamage(attackDamage);
                if (enableDebugLogs) Debug.Log($"Враг {name}: НАНЕСЕН УРОН {attackDamage}!");
            }
        }
        else
        {
            if (enableDebugLogs) Debug.Log($"Враг {name}: ПРОМАХ! Игрок ушел");
            
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
        
        if (enableDebugLogs) Debug.Log($"Враг {name}: получил урон {damage}. Осталось: {currentHealth}");
        
        if (isAttacking)
        {
            StopAllCoroutines();
            EndAttack();
        }
        
        StartCoroutine(HitAnimationSequence());
        
        if (enemyAnimator != null)
            enemyAnimator.TriggerHit();
        
        if (hitEffect != null && hitPoint.HasValue)
        {
            Instantiate(hitEffect, hitPoint.Value, Quaternion.identity);
        }
        
        if (hurtSound != null && audioSource != null)
            audioSource.PlayOneShot(hurtSound);
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(ReturnToChaseAfterHit());
        }
    }
    
    IEnumerator HitAnimationSequence()
    {
        isTakingHit = true;
        
        if (agent != null)
            agent.isStopped = true;
        
        yield return new WaitForSeconds(0.3f);
        
        isTakingHit = false;
    }
    
    IEnumerator ReturnToChaseAfterHit()
    {
        yield return new WaitForSeconds(0.3f);
        
        if (!isDead && !isAttacking)
        {
            currentState = EnemyState.Chasing;
            if (agent != null)
                agent.isStopped = false;
        }
    }
    
    void Die()
    {
        isDead = true;
        currentState = EnemyState.Dead;
        
        if (enableDebugLogs) Debug.Log($"Враг {name}: УМИРАЮ!");
        
        if (agent != null)
            agent.isStopped = true;
        
        if (enemyAnimator != null)
            enemyAnimator.TriggerDeath();
        
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        
        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound);
        
        DropLoot();
        
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        StartCoroutine(DestroyAfterDeath());
    }
    
    IEnumerator DestroyAfterDeath()
    {
        // Ждем 4 секунды перед удалением врага
        float deathAnimationLength = 4f;
        yield return new WaitForSeconds(deathAnimationLength);
        
        Destroy(gameObject);
    }
    
    void DropLoot()
    {
        if (lootPrefab == null) return;
        if (Random.value > lootDropChance) return;
        
        Vector3 lootPosition = new Vector3(transform.position.x, 0.05f, transform.position.z);
        Instantiate(lootPrefab, lootPosition, Quaternion.identity);
        
        if (enableDebugLogs) Debug.Log($"Выпало {lootAmount} болтов");
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