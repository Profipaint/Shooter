using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    
    [Header("UI")]
    public Slider healthSlider;
    public Image fillImage;
    public Gradient healthGradient;
    public Text healthText;
    
    [Header("Damage Effects")]
    public float invincibilityDuration = 1f;
    private bool isInvincible = false;
    public GameObject damageFlashEffect;
    public AudioClip damageSound;
    private AudioSource audioSource;
    
    [Header("Camera Hit Shake (Качок камеры при ударе)")]
    public bool enableHitShake = true;
    public float hitShakeAmount = 0.3f;        // Сила тряски
    public float hitShakeDuration = 0.2f;      // Длительность тряски
    public float hitShakeSpeed = 30f;          // Скорость тряски
    public float hitShakeReturnSpeed = 15f;    // Скорость возврата
    public float hitShakeDelay = 0.4f;         // ЗАДЕРЖКА ПЕРЕД КАЧКОМ (совпадает с анимацией удара врага)
    
    [Header("Camera Hit Punch (Рывок камеры)")]
    public bool enableHitPunch = true;
    public float hitPunchAmount = 0.5f;        // Сила рывка
    public float hitPunchDuration = 0.15f;     // Длительность рывка
    public float hitPunchDelay = 0.4f;         // ЗАДЕРЖКА ПЕРЕД РЫВКОМ
    
    [Header("Death")]
    public GameObject deathEffect;
    public string reloadSceneName = "GameScene";
    public float deathDelay = 2f;
    private bool isDead = false;
    
    [Header("Health Regeneration")]
    public bool enableRegeneration = false;
    public float regenDelay = 3f;
    public float regenRate = 5f;
    private float regenTimer = 0f;
    private Coroutine regenCoroutine;
    
    private FirstPersonMovement movement;
    private Camera playerCamera;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private bool isShaking = false;
    private float shakeTimer = 0f;
    private Vector3 shakeOffset;
    private Coroutine currentHitCoroutine;
    
    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        
        movement = GetComponent<FirstPersonMovement>();
        
        if (Camera.main != null)
        {
            playerCamera = Camera.main;
            originalCameraPosition = playerCamera.transform.localPosition;
            originalCameraRotation = playerCamera.transform.localRotation;
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && damageSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        if (damageFlashEffect != null)
            damageFlashEffect.SetActive(false);
    }
    
    void Update()
    {
        if (isShaking)
        {
            UpdateHitShake();
        }
        
        if (enableRegeneration && !isDead && currentHealth < maxHealth && !isInvincible)
        {
            if (regenTimer > 0)
            {
                regenTimer -= Time.deltaTime;
            }
            else
            {
                if (regenCoroutine == null)
                    regenCoroutine = StartCoroutine(RegenerateHealth());
            }
        }
    }
    
    public void TakeDamage(float damage, Vector3? hitPoint = null)
    {
        if (isDead || isInvincible) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        UpdateHealthUI();
        
        // === КАЧОК КАМЕРЫ ПРИ УДАРЕ (С ЗАДЕРЖКОЙ) ===
        TriggerHitShakeWithDelay();
        
        StartCoroutine(InvincibilityFrames());
        StartCoroutine(DamageFlash());
        
        if (damageSound != null && audioSource != null)
            audioSource.PlayOneShot(damageSound);
        
        if (enableRegeneration)
        {
            regenTimer = regenDelay;
            if (regenCoroutine != null)
            {
                StopCoroutine(regenCoroutine);
                regenCoroutine = null;
            }
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void TriggerHitShakeWithDelay()
    {
        if (!enableHitShake && !enableHitPunch) return;
        if (playerCamera == null) return;
        
        // Останавливаем текущую корутину, если она есть
        if (currentHitCoroutine != null)
        {
            StopCoroutine(currentHitCoroutine);
        }
        
        // Запускаем эффекты с задержкой
        currentHitCoroutine = StartCoroutine(HitEffectsWithDelay());
    }
    
    IEnumerator HitEffectsWithDelay()
    {
        // === РЫВОК (Punch) с задержкой ===
        if (enableHitPunch)
        {
            yield return new WaitForSeconds(hitPunchDelay);
            StartCoroutine(HitPunchCoroutine());
        }
        
        // === ТРЯСКА (Shake) с задержкой ===
        if (enableHitShake)
        {
            // Ждем немного больше для тряски (чтобы они не перекрывались)
            yield return new WaitForSeconds(hitShakeDelay - hitPunchDelay);
            if (hitShakeDelay < hitPunchDelay)
            {
                yield return new WaitForSeconds(hitPunchDelay - hitShakeDelay);
            }
            StartCoroutine(HitShakeCoroutine());
        }
        
        currentHitCoroutine = null;
    }
    
    IEnumerator HitPunchCoroutine()
    {
        // Резкий рывок камеры в сторону удара
        Vector3 punchDirection = new Vector3(
            Random.Range(-1f, 1f) * hitPunchAmount,
            Random.Range(-0.5f, 0.5f) * hitPunchAmount * 0.5f,
            Random.Range(-0.3f, 0.3f) * hitPunchAmount * 0.3f
        );
        
        float elapsed = 0f;
        Vector3 startPos = playerCamera.transform.localPosition;
        Vector3 targetPos = startPos + punchDirection;
        
        // Рывок вперед (быстрый)
        while (elapsed < hitPunchDuration * 0.4f)
        {
            float t = elapsed / (hitPunchDuration * 0.4f);
            playerCamera.transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Возврат обратно (плавный)
        elapsed = 0f;
        Vector3 currentPos = playerCamera.transform.localPosition;
        
        while (elapsed < hitPunchDuration * 0.6f)
        {
            float t = elapsed / (hitPunchDuration * 0.6f);
            playerCamera.transform.localPosition = Vector3.Lerp(currentPos, originalCameraPosition, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        playerCamera.transform.localPosition = originalCameraPosition;
    }
    
    IEnumerator HitShakeCoroutine()
    {
        isShaking = true;
        shakeTimer = hitShakeDuration;
        
        while (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            
            float intensity = shakeTimer / hitShakeDuration;
            float currentAmount = hitShakeAmount * intensity;
            
            float x = Random.Range(-currentAmount, currentAmount);
            float y = Random.Range(-currentAmount, currentAmount) * 0.5f;
            float z = Random.Range(-currentAmount, currentAmount) * 0.3f;
            
            Vector3 shakePos = originalCameraPosition + new Vector3(x, y, z);
            playerCamera.transform.localPosition = Vector3.Lerp(
                playerCamera.transform.localPosition,
                shakePos,
                Time.deltaTime * hitShakeSpeed
            );
            
            yield return null;
        }
        
        ResetCamera();
        isShaking = false;
    }
    
    void UpdateHitShake()
    {
        if (playerCamera == null || !isShaking) return;
        
        if (shakeTimer <= 0)
        {
            ResetCamera();
            isShaking = false;
        }
    }
    
    void ResetCamera()
    {
        if (playerCamera == null) return;
        StartCoroutine(SmoothResetCamera());
    }
    
    IEnumerator SmoothResetCamera()
    {
        float elapsed = 0f;
        float duration = 0.1f;
        Vector3 startPos = playerCamera.transform.localPosition;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            playerCamera.transform.localPosition = Vector3.Lerp(startPos, originalCameraPosition, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        playerCamera.transform.localPosition = originalCameraPosition;
        isShaking = false;
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();
        
        if (enableRegeneration)
        {
            regenTimer = regenDelay;
            if (regenCoroutine != null)
            {
                StopCoroutine(regenCoroutine);
                regenCoroutine = null;
            }
        }
    }
    
    void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth;
            
            if (fillImage != null && healthGradient != null)
            {
                fillImage.color = healthGradient.Evaluate(healthSlider.value);
            }
        }
        
        if (healthText != null)
        {
            healthText.text = $"{Mathf.Round(currentHealth)} / {maxHealth}";
        }
    }
    
    IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }
    
    IEnumerator DamageFlash()
    {
        if (damageFlashEffect != null)
        {
            damageFlashEffect.SetActive(true);
            yield return new WaitForSeconds(0.1f);
            damageFlashEffect.SetActive(false);
        }
    }
    
    IEnumerator RegenerateHealth()
    {
        while (currentHealth < maxHealth && !isInvincible && !isDead)
        {
            Heal(regenRate * Time.deltaTime);
            yield return null;
        }
        regenCoroutine = null;
    }
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log("Игрок умер!");
        
        if (movement != null)
            movement.SetMovementEnabled(false);
            
        enabled = false;
        
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);
            
        StartCoroutine(ReloadSceneAfterDelay());
    }
    
    IEnumerator ReloadSceneAfterDelay()
    {
        yield return new WaitForSeconds(deathDelay);
        SceneManager.LoadScene(reloadSceneName);
    }
    
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
}