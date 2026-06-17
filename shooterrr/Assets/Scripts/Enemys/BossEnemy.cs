using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BossEnemy : MonoBehaviour
{
    public float maxHealth = 200f;
    private float currentHealth;
    
    public float attackDamage = 15f;
    public float attackCooldown = 1.2f;
    public float attackRange = 2.5f;
    public float chaseRange = 20f;
    
    private float nextAttackTime = 0f;
    private Transform player;
    private NavMeshAgent agent;
    
    public BossEnemyAnimator bossAnimator;
    
    // === ОСОБАЯ АТАКА ===
    public bool enableSpecialAttack = true;
    public float specialDamage = 40f;
    public float specialAttackCooldown = 8f;
    public float specialAttackRange = 4f;
    public float specialAttackSpeedMultiplier = 4f;
    public float specialAttackWindup = 1f;
    public float specialAttackDuration = 1.5f;
    private float nextSpecialAttackTime = 0f;
    private bool isSpecialAttacking = false;
    private bool isSpecialCharging = false;
    private float originalMoveSpeed;
    
    public GameObject specialAttackImpactEffect;
    public AudioClip specialAttackSound;
    public AudioClip specialChargeSound;
    
    public GameObject lootPrefab;
    public float lootDropChance = 1f;
    
    public GameObject deathEffect;
    public GameObject hitEffect;
    public AudioClip deathSound;
    public AudioClip hitSound;
    public AudioClip attackSound;
    private AudioSource audioSource;
    
    private bool isDead = false;
    private bool isTakingHit = false;
    private bool isAttacking = false;
    private bool isMovementLocked = false;
    
    void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        agent = GetComponent<NavMeshAgent>();
        
        if (agent != null)
        {
            agent.speed = 2.5f;
            agent.stoppingDistance = attackRange - 0.5f;
        }
        
        originalMoveSpeed = agent != null ? agent.speed : 2.5f;
        
        if (bossAnimator == null)
            bossAnimator = GetComponentInChildren<BossEnemyAnimator>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        nextSpecialAttackTime = specialAttackCooldown * 0.3f;
    }
    
    void Update()
    {
        if (isDead || player == null) return;
        
        if (isTakingHit || isAttacking || isSpecialAttacking || isSpecialCharging || isMovementLocked)
        {
            if (agent != null)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            return;
        }
        
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (enableSpecialAttack && Time.time >= nextSpecialAttackTime && distance <= specialAttackRange)
        {
            StartCoroutine(SpecialAttackSequence());
            return;
        }
        
        if (distance <= attackRange)
        {
            if (agent != null) agent.isStopped = true;
            
            if (Time.time >= nextAttackTime && !isAttacking)
            {
                StartCoroutine(PerformAttack());
            }
        }
        else if (distance <= chaseRange)
        {
            if (agent != null)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
        }
        else
        {
            if (agent != null)
                agent.isStopped = true;
        }
    }
    
    System.Collections.IEnumerator PerformAttack()
    {
        isAttacking = true;
        isMovementLocked = true;
        nextAttackTime = Time.time + attackCooldown;
        
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        
        if (bossAnimator != null)
            bossAnimator.TriggerAttack();
        
        if (attackSound != null && audioSource != null)
            audioSource.PlayOneShot(attackSound);
        
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null && Vector3.Distance(transform.position, player.position) <= attackRange)
            playerHealth.TakeDamage(attackDamage);
        
        yield return new WaitForSeconds(0.8f);
        
        isAttacking = false;
        isMovementLocked = false;
        if (agent != null) agent.isStopped = false;
    }
    
    System.Collections.IEnumerator SpecialAttackSequence()
    {
        if (isSpecialAttacking) yield break;
        
        isSpecialAttacking = true;
        isSpecialCharging = true;
        isMovementLocked = true;
        nextSpecialAttackTime = Time.time + specialAttackCooldown;
        
        float boostedSpeed = originalMoveSpeed * specialAttackSpeedMultiplier;
        if (agent != null)
        {
            agent.speed = boostedSpeed;
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        
        if (specialChargeSound != null && audioSource != null)
            audioSource.PlayOneShot(specialChargeSound);
        
        float windupTimer = 0f;
        while (windupTimer < specialAttackWindup && player != null)
        {
            windupTimer += Time.deltaTime;
            
            if (agent != null && player != null)
            {
                agent.SetDestination(player.position);
                if (Vector3.Distance(transform.position, player.position) <= attackRange)
                    break;
            }
            
            yield return null;
        }
        
        isSpecialCharging = false;
        isMovementLocked = true;
        
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.speed = originalMoveSpeed;
        }
        
        if (bossAnimator != null)
            bossAnimator.TriggerTopAttack();
        
        if (specialAttackSound != null && audioSource != null)
            audioSource.PlayOneShot(specialAttackSound);
        
        if (specialAttackImpactEffect != null && player != null)
        {
            Instantiate(specialAttackImpactEffect, player.position + Vector3.up * 0.5f, Quaternion.identity);
        }
        
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
        
        yield return new WaitForSeconds(specialAttackDuration);
        
        if (agent != null)
        {
            agent.speed = originalMoveSpeed;
            agent.isStopped = false;
        }
        
        isSpecialAttacking = false;
        isMovementLocked = false;
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        if (isTakingHit) return;
        
        currentHealth -= damage;
        Debug.Log($"Boss health: {currentHealth}");
        
        if (isAttacking || isSpecialAttacking || isSpecialCharging)
        {
            StopAllCoroutines();
            isAttacking = false;
            isSpecialAttacking = false;
            isSpecialCharging = false;
            
            if (agent != null)
            {
                agent.speed = originalMoveSpeed;
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }
        
        StartCoroutine(HitAnimation());
        
        if (bossAnimator != null)
            bossAnimator.TriggerHit();
        
        if (hitEffect != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
            Instantiate(hitEffect, spawnPos, Quaternion.identity);
        }
        
        if (hitSound != null && audioSource != null)
            audioSource.PlayOneShot(hitSound);
        
        if (currentHealth <= 0)
            Die();
    }
    
    System.Collections.IEnumerator HitAnimation()
    {
        isTakingHit = true;
        isMovementLocked = true;
        
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        
        yield return new WaitForSeconds(0.5f);
        
        isTakingHit = false;
        isMovementLocked = false;
        if (agent != null) agent.isStopped = false;
    }
    
    void Die()
    {
        if (isDead) return;
        isDead = true;
        
        Debug.Log("Boss died!");
        
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }
        
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        if (bossAnimator != null)
            bossAnimator.TriggerDeath();
        
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        
        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound);
        
        DropLoot();
        
        // === БОСС СКРЫВАЕТСЯ ЧЕРЕЗ 8 СЕКУНД (НЕ УДАЛЯЕТСЯ) ===
        StartCoroutine(HideAfterDelay(8f));
    }
    
    System.Collections.IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
        Debug.Log("Boss скрыт!");
    }
    
    void DropLoot()
    {
        if (lootPrefab == null) return;
        if (Random.value > lootDropChance) return;
        
        Vector3 pos = transform.position;
        pos.y = 0.05f;
        
        int lootCount = Random.Range(3, 6);
        for (int i = 0; i < lootCount; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), 0.05f, Random.Range(-0.5f, 0.5f));
            Instantiate(lootPrefab, pos + offset, Quaternion.identity);
        }
    }
    
    // === МЕТОД ДЛЯ ПРОВЕРКИ ЗДОРОВЬЯ ===
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, specialAttackRange);
    }
}