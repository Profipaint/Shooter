using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public float maxHealth = 50f;
    private float currentHealth;
    
    public float attackDamage = 10f;
    public float attackCooldown = 1f;
    public float attackRange = 2f;
    public float chaseRange = 15f;
    
    private float nextAttackTime = 0f;
    private Transform player;
    private NavMeshAgent agent;
    
    public EnemyAnimator enemyAnimator;
    
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
    
    void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        agent = GetComponent<NavMeshAgent>();
        
        if (agent != null)
        {
            agent.speed = 3f;
            agent.stoppingDistance = attackRange - 0.5f;
        }
        
        if (enemyAnimator == null)
            enemyAnimator = GetComponentInChildren<EnemyAnimator>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    void Update()
    {
        if (isDead || player == null) return;
        if (isTakingHit) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= attackRange)
        {
            if (agent != null) agent.isStopped = true;
            
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackCooldown;
                
                if (enemyAnimator != null)
                    enemyAnimator.TriggerAttack();
                
                if (attackSound != null && audioSource != null)
                    audioSource.PlayOneShot(attackSound);
                
                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                    playerHealth.TakeDamage(attackDamage);
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
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        if (isTakingHit) return;
        
        currentHealth -= damage;
        Debug.Log($"Enemy health: {currentHealth}");
        
        StartCoroutine(HitAnimation());
        
        if (enemyAnimator != null)
            enemyAnimator.TriggerHit();
        
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
        if (agent != null) agent.isStopped = true;
        yield return new WaitForSeconds(0.5f);
        isTakingHit = false;
        if (agent != null) agent.isStopped = false;
    }
    
    void Die()
    {
        if (isDead) return;
        isDead = true;
        
        Debug.Log("Enemy died!");
        
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
        
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        if (enemyAnimator != null)
            enemyAnimator.TriggerDeath();
        
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        
        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound);
        
        DropLoot();
        
        Destroy(gameObject, 6.3f);
    }
    
    void DropLoot()
    {
        if (lootPrefab == null) return;
        if (Random.value > lootDropChance) return;
        
        Vector3 pos = transform.position;
        pos.y = 0.05f;
        Instantiate(lootPrefab, pos, Quaternion.identity);
    }
    
    // ===== GIZMOS ДЛЯ ВИЗУАЛИЗАЦИИ =====
    void OnDrawGizmosSelected()
    {
        // Красный - зона атаки
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Желтый - зона обнаружения
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}