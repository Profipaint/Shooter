using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BossEnemy : MonoBehaviour
{
    [Header("=== BOSS SETTINGS ===")]
    
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
    
    [Header("=== SPECIAL ATTACK (TopAttack) ===")]
    public bool enableSpecialAttack = true;
    public float specialDamage = 40f;
    public float specialAttackCooldown = 5f;
    public float specialAttackDelay = 0.8f;
    public float specialAttackDuration = 1.5f;
    public float specialAttackRange = 3.5f;
    public float specialAttackChance = 0.3f;
    public float specialAttackWindup = 0.5f;
    public float specialAttackSpeedMultiplier = 4f;  // Увеличение скорости в 4 раза!
    private float nextSpecialAttackTime = 0f;
    private bool isSpecialAttacking = false;
    private bool isSpecialCharging = false;           // Фаза зарядки перед супер атакой
    
    [Header("Special Attack Effects")]
    public GameObject specialAttackEffect;
    public GameObject specialAttackImpactEffect;
    public AudioClip specialAttackSound;
    public AudioClip specialChargeSound;
    
    [Header("Animation")]
    public Animator animator;
    public string speedParam = "Speed";
    public string walkingParam = "walking";
    public string attackParam = "attack";
    public string topAttackParam = "TopAttack";
    public string hitParam = "hit";
    public string dyingParam = "dying";
    
    [Header("Effects")]
    public GameObject deathEffect;
    public GameObject hitEffect;
    public AudioClip deathSound;
    public AudioClip attackSound;
    public AudioClip hurtSound;
    private AudioSource audioSource;
    
    [Header("Loot")]
    public GameObject lootPrefab;
    public int lootAmount = 20;
    public float lootDropChance = 1f;
    
    [Header("States")]
    public float detectionRange = 15f;
    public bool enableDebugLogs = true;
    private bool isDead = false;
    private bool isTakingHit = false;
    private bool blockAttackAfterHit = false;
    private BossState currentState = BossState.Idle;
    private float originalMoveSpeed;  // Сохраняем оригинальную скорость
    
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
        
        // Сохраняем оригинальную скорость
        originalMoveSpeed = moveSpeed;
        
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        
        if (animator == null)
            Debug.LogWarning("Animator не найден на дочерней модели!");
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        nextSpecialAttackTime = specialAttackCooldown * 0.5f;
        
        if (enableDebugLogs)
        {
            Debug.Log($"=== БОСС {name} ПОЯВИЛСЯ! ===");
            Debug.Log($"Здоровье: {maxHealth}, Урон: {normalDamage}, Особый урон: {specialDamage}");
        }
    }
    
    void Update()
    {
        if (isDead || player == null) return;
        if (currentState == BossState.Dead) return;
        
        // ВО ВРЕМЯ АНИМАЦИЙ БОСС НЕ ДВИГАЕТСЯ
        if (isTakingHit || isSpecialAttacking || isAttacking() || isSpecialCharging)
        {
            if (agent != null) agent.isStopped = true;
            UpdateMovementAnimation(0f, false);
            
            // Если закончилась анимация атаки - возвращаемся к преследованию
            if (isAttacking() && !isSpecialAttacking && !isTakingHit)
            {
                // Проверяем, закончилась ли анимация атаки
                if (animator != null)
                {
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    if (!stateInfo.IsName("attack") && !stateInfo.IsName("TopAttack"))
                    {
                        currentState = BossState.Chasing;
                        if (agent != null) agent.isStopped = false;
                    }
                }
            }
            
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
                        if (enableDebugLogs) Debug.Log($"БОСС: ОСОБАЯ АТАКА (TopAttack)!");
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
            UpdateMovementAnimation(0f, false);
            return;
        }
        
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            float speed = agent.velocity.magnitude;
            UpdateMovementAnimation(speed, speed > 0.1f);
        }
        else
        {
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
            transform.LookAt(player);
            UpdateMovementAnimation(moveSpeed, true);
        }
    }
    
    bool isAttacking()
    {
        return currentState == BossState.Attacking;
    }
    
    void UpdateMovementAnimation(float speed, bool isWalking)
    {
        if (animator == null) return;
        
        animator.SetFloat(speedParam, speed);
        animator.SetBool(walkingParam, isWalking);
    }
    
    void NormalAttack()
    {
        if (Time.time >= nextNormalAttackTime)
        {
            nextNormalAttackTime = Time.time + normalAttackCooldown;
            
            // БОСС НЕ ДВИГАЕТСЯ ВО ВРЕМЯ АТАКИ
            if (agent != null) agent.isStopped = true;
            
            if (animator != null)
                animator.SetTrigger(attackParam);
            
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
                if (enableDebugLogs) Debug.Log($"БОСС: Нанесен обычный урон {normalDamage}!");
            }
        }
    }
    
    void SpecialAttack()
    {
        if (Time.time >= nextSpecialAttackTime && !isSpecialAttacking)
        {
            isSpecialAttacking = true;
            nextSpecialAttackTime = Time.time + specialAttackCooldown;
            
            // БОСС НЕ ДВИГАЕТСЯ ВО ВРЕМЯ АТАКИ
            if (agent != null) agent.isStopped = true;
            
            // === УВЕЛИЧИВАЕМ СКОРОСТЬ В 4 РАЗА ПЕРЕД СУПЕР АТАКОЙ ===
            StartCoroutine(SpecialAttackSpeedBoost());
            
            if (animator != null)
                animator.SetTrigger(topAttackParam);
            
            if (specialChargeSound != null && audioSource != null)
                audioSource.PlayOneShot(specialChargeSound);
            
            if (enableDebugLogs) Debug.Log($"БОСС: ЗАРЯЖАЕТ ОСОБУЮ АТАКУ (TopAttack)! Скорость увеличена в {specialAttackSpeedMultiplier}x!");
            
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
        
        // Увеличиваем скорость
        float boostedSpeed = originalMoveSpeed * specialAttackSpeedMultiplier;
        moveSpeed = boostedSpeed;
        if (agent != null) agent.speed = boostedSpeed;
        
        if (enableDebugLogs) Debug.Log($"БОСС: Скорость увеличена до {boostedSpeed}");
        
        // Ждем зарядку
        yield return new WaitForSeconds(specialAttackWindup);
        
        isSpecialCharging = false;
        
        // Возвращаем скорость обратно после атаки
        yield return new WaitForSeconds(specialAttackDuration - specialAttackWindup);
        
        moveSpeed = originalMoveSpeed;
        if (agent != null) agent.speed = originalMoveSpeed;
        
        if (enableDebugLogs) Debug.Log($"БОСС: Скорость восстановлена до {originalMoveSpeed}");
    }
    
    IEnumerator SpecialAttackSequence()
    {
        // Ждем зарядку
        yield return new WaitForSeconds(specialAttackWindup);
        
        // Эффект удара
        if (specialAttackImpactEffect != null && player != null)
        {
            Instantiate(specialAttackImpactEffect, player.position + Vector3.up * 0.5f, Quaternion.identity);
        }
        
        if (specialAttackSound != null && audioSource != null)
            audioSource.PlayOneShot(specialAttackSound);
        
        // Наносим урон
        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= specialAttackRange)
            {
                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(specialDamage);
                    if (enableDebugLogs) Debug.Log($"БОСС: ОСОБАЯ АТАКА! Урон {specialDamage}!");
                }
            }
            else
            {
                if (enableDebugLogs) Debug.Log($"БОСС: Особый удар промахнулся!");
            }
        }
        
        // Ждем окончания анимации
        yield return new WaitForSeconds(specialAttackDuration - specialAttackWindup);
        
        isSpecialAttacking = false;
        currentState = BossState.Chasing;
        
        if (enableDebugLogs) Debug.Log($"БОСС: Особая атака завершена.");
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
        
        if (enableDebugLogs) Debug.Log($"БОСС: получил урон {damage}. Осталось: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
            return;
        }
        
        // Прерываем особую атаку при получении урона
        if (isSpecialAttacking)
        {
            StopAllCoroutines();
            isSpecialAttacking = false;
            isSpecialCharging = false;
            // Восстанавливаем скорость
            moveSpeed = originalMoveSpeed;
            if (agent != null) agent.speed = originalMoveSpeed;
            currentState = BossState.Chasing;
        }
        
        blockAttackAfterHit = true;
        StartCoroutine(HitAnimationSequence());
        
        // БОСС НЕ ДВИГАЕТСЯ ВО ВРЕМЯ АНИМАЦИИ ХИТА
        if (animator != null)
            animator.SetTrigger(hitParam);
        
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
        UpdateMovementAnimation(0f, false);
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
        
        Debug.Log($"=== БОСС {name} ПОВЕРЖЕН! ===");
        
        StopAllCoroutines();
        
        // Восстанавливаем скорость перед смертью
        moveSpeed = originalMoveSpeed;
        
        Transform root = transform.root;
        
        NavMeshAgent rootAgent = root.GetComponent<NavMeshAgent>();
        if (rootAgent != null)
        {
            rootAgent.isStopped = true;
            rootAgent.enabled = false;
        }
        
        Collider rootCollider = root.GetComponent<Collider>();
        if (rootCollider != null) rootCollider.enabled = false;
        
        Vector3 groundPos = root.position;
        groundPos.y = 0f;
        root.position = groundPos;
        
        isTakingHit = false;
        isSpecialAttacking = false;
        isSpecialCharging = false;
        blockAttackAfterHit = false;
        
        if (animator != null)
        {
            animator.ResetTrigger(attackParam);
            animator.ResetTrigger(topAttackParam);
            animator.ResetTrigger(hitParam);
            animator.SetTrigger(dyingParam);
        }
        
        if (deathEffect != null)
            Instantiate(deathEffect, root.position, Quaternion.identity);
        
        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound);
        
        DropLoot();
        
        StartCoroutine(DestroyAfterDelay());
    }
    
    IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(4f);
        Transform root = transform.root;
        Destroy(root.gameObject);
        Debug.Log("БОСС уничтожен");
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
        
        if (enableDebugLogs) Debug.Log($"БОСС: Выпало {lootCount} лута!");
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
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, specialAttackRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}