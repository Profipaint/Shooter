using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BossEnemy : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 200f;
    private float currentHealth;
    
    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float stoppingDistance = 3f;
    public float chaseRange = 20f;
    public float attackRange = 2.5f;
    private Transform player;
    private NavMeshAgent agent;
    
    [Header("Normal Attack")]
    public float normalDamage = 15f;
    public float normalAttackCooldown = 1.2f;
    public float normalAttackDelay = 0.4f;
    public float normalAttackDuration = 0.8f;
    private float nextNormalAttackTime = 0f;
    
    [Header("Special Attack")]
    public bool enableSpecialAttack = true;
    public float specialDamage = 40f;
    public float specialAttackCooldown = 5f;
    public float specialAttackRange = 3.5f;
    public float specialAttackChance = 0.3f;
    public float specialAttackWindup = 0.5f;
    public float specialAttackDuration = 1.5f;
    public float specialAttackSpeedMultiplier = 4f;
    private float nextSpecialAttackTime = 0f;
    private bool isSpecialAttacking = false;
    private bool isSpecialCharging = false;
    
    [Header("Special Attack Effects")]
    public GameObject specialAttackImpactEffect;
    public AudioClip specialAttackSound;
    public AudioClip specialChargeSound;
    
    [Header("Animation")]
    public BossEnemyAnimator bossAnimator;
    
    [Header("Effects")]
    public GameObject deathEffect;
    public GameObject hitEffect;
    public AudioClip deathSound;
    public AudioClip attackSound;
    public AudioClip hurtSound;
    private AudioSource audioSource;
    
    [Header("Loot")]
    public GameObject lootPrefab;
    public float lootDropChance = 1f;
    
    [Header("States")]
    public float detectionRange = 15f;
    public bool enableDebugLogs = true;
    private bool isDead = false;
    private bool isTakingHit = false;
    private bool blockAttackAfterHit = false;
    private BossState currentState = BossState.Idle;
    private float originalMoveSpeed;
    
    private enum BossState
    {
        Idle,
        Chasing,
        Attacking,
        SpecialAttacking,
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
        
        originalMoveSpeed = moveSpeed;
        
        if (bossAnimator == null)
            bossAnimator = GetComponentInChildren<BossEnemyAnimator>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        nextSpecialAttackTime = specialAttackCooldown * 0.5f;
    }
    
    void Update()
    {
        if (isDead || player == null) return;
        if (currentState == BossState.Dead) return;
        
        if (isTakingHit || isSpecialAttacking || isAttacking() || isSpecialCharging)
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
            case BossState.Idle:
                if (distanceToPlayer <= detectionRange)
                    currentState = BossState.Chasing;
                break;
                
            case BossState.Chasing:
                if (distanceToPlayer <= attackRange && !isSpecialAttacking)
                {
                    if (enableSpecialAttack && 
                        Time.time >= nextSpecialAttackTime && 
                        Random.value < specialAttackChance)
                    {
                        currentState = BossState.SpecialAttacking;
                    }
                    else
                    {
                        currentState = BossState.Attacking;
                    }
                }
                else if (distanceToPlayer > chaseRange)
                {
                    currentState = BossState.Idle;
                    if (agent != null) agent.isStopped = true;
                }
                else
                {
                    ChasePlayer();
                }
                break;
                
            case BossState.Attacking:
                if (distanceToPlayer > attackRange)
                {
                    currentState = BossState.Chasing;
                }
                else
                {
                    NormalAttack();
                }
                break;
                
            case BossState.SpecialAttacking:
                if (distanceToPlayer > specialAttackRange)
                {
                    currentState = BossState.Chasing;
                }
                else
                {
                    SpecialAttack();
                }
                break;
        }
    }
    
    void ChasePlayer()
    {
        if (isAttacking() || isTakingHit || isSpecialAttacking || isSpecialCharging) 
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
    
    bool isAttacking()
    {
        return currentState == BossState.Attacking;
    }
    
    void NormalAttack()
    {
        if (Time.time >= nextNormalAttackTime)
        {
            nextNormalAttackTime = Time.time + normalAttackCooldown;
            
            if (agent != null) agent.isStopped = true;
            
            if (bossAnimator != null)
                bossAnimator.TriggerAttack();
            
            if (attackSound != null && audioSource != null)
                audioSource.PlayOneShot(attackSound);
            
            Invoke(nameof(DelayedNormalDamage), normalAttackDelay);
            StartCoroutine(EndAttackAfterDelay(normalAttackDuration));
        }
    }
    
    void DelayedNormalDamage()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= attackRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(normalDamage);
            }
        }
    }
    
    void SpecialAttack()
    {
        if (Time.time >= nextSpecialAttackTime && !isSpecialAttacking)
        {
            isSpecialAttacking = true;
            nextSpecialAttackTime = Time.time + specialAttackCooldown;
            
            if (agent != null) agent.isStopped = true;
            
            StartCoroutine(SpecialAttackSpeedBoost());
            
            if (bossAnimator != null)
                bossAnimator.TriggerTopAttack();
            
            if (specialChargeSound != null && audioSource != null)
                audioSource.PlayOneShot(specialChargeSound);
            
            StartCoroutine(SpecialAttackSequence());
        }
        else
        {
            currentState = BossState.Attacking;
        }
    }
    
    IEnumerator SpecialAttackSpeedBoost()
    {
        isSpecialCharging = true;
        
        float boostedSpeed = originalMoveSpeed * specialAttackSpeedMultiplier;
        moveSpeed = boostedSpeed;
        if (agent != null) agent.speed = boostedSpeed;
        
        yield return new WaitForSeconds(specialAttackWindup);
        
        isSpecialCharging = false;
        
        yield return new WaitForSeconds(specialAttackDuration - specialAttackWindup);
        
        moveSpeed = originalMoveSpeed;
        if (agent != null) agent.speed = originalMoveSpeed;
    }
    
    IEnumerator SpecialAttackSequence()
    {
        yield return new WaitForSeconds(specialAttackWindup);
        
        if (specialAttackImpactEffect != null && player != null)
        {
            Instantiate(specialAttackImpactEffect, player.position + Vector3.up * 0.5f, Quaternion.identity);
        }
        
        if (specialAttackSound != null && audioSource != null)
            audioSource.PlayOneShot(specialAttackSound);
        
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= specialAttackRange)
            {
                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(specialDamage);
                }
            }
        }
        
        yield return new WaitForSeconds(specialAttackDuration - specialAttackWindup);
        
        isSpecialAttacking = false;
        currentState = BossState.Chasing;
    }
    
    IEnumerator EndAttackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentState == BossState.Attacking)
        {
            currentState = BossState.Chasing;
            if (agent != null && !isDead)
                agent.isStopped = false;
        }
    }
    
    public void TakeDamage(float damage, Vector3? hitPoint = null)
    {
        if (isDead) return;
        if (isTakingHit) return;
        
        currentHealth -= damage;
        
        if (enableDebugLogs) Debug.Log($"ÁÎÑÑ: ïîëó÷èë óðîí {damage}. Îñòàëîñü: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
            return;
        }
        
        if (isSpecialAttacking)
        {
            StopAllCoroutines();
            isSpecialAttacking = false;
            isSpecialCharging = false;
            moveSpeed = originalMoveSpeed;
            if (agent != null) agent.speed = originalMoveSpeed;
            currentState = BossState.Chasing;
        }
        
        blockAttackAfterHit = true;
        StartCoroutine(HitAnimationSequence());
        
        if (bossAnimator != null)
            bossAnimator.TriggerHit();
        
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
        
        if (!isDead && !isAttacking() && !isSpecialAttacking)
        {
            currentState = BossState.Chasing;
            if (agent != null) agent.isStopped = false;
        }
    }
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        currentState = BossState.Dead;
        
        Debug.Log($"=== ÁÎÑÑ {name} ÏÎÂÅÐÆÅÍ! ===");
        
        StopAllCoroutines();
        
        Transform root = transform.root;
        
        NavMeshAgent rootAgent = root.GetComponent<NavMeshAgent>();
        if (rootAgent != null)
        {
            rootAgent.isStopped = true;
            rootAgent.enabled = false;
        }
        
        Collider rootCollider = root.GetComponent<Collider>();
        if (rootCollider != null) rootCollider.enabled = false;
        
        isTakingHit = false;
        isSpecialAttacking = false;
        isSpecialCharging = false;
        blockAttackAfterHit = false;
        
        moveSpeed = originalMoveSpeed;
        
        if (bossAnimator != null)
            bossAnimator.TriggerDeath();
        
        if (deathEffect != null)
            Instantiate(deathEffect, root.position, Quaternion.identity);
        
        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound);
        
        DropLoot();
        
        this.enabled = false;
        
        // ÁÎÑÑ ÈÑ×ÅÇÀÅÒ ×ÅÐÅÇ 8 ÑÅÊÓÍÄ
        Destroy(root.gameObject, 8f);
    }
    
    void DropLoot()
    {
        if (lootPrefab == null) return;
        if (Random.value > lootDropChance) return;
        
        Vector3 rootPos = transform.root.position;
        
        int lootCount = Random.Range(3, 6);
        for (int i = 0; i < lootCount; i++)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-1f, 1f),
                0.05f,
                Random.Range(-1f, 1f)
            );
            Instantiate(lootPrefab, rootPos + randomOffset, Quaternion.identity);
        }
    }
    
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, specialAttackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}