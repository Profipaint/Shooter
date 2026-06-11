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
    
    private FirstPersonMovement movement; // Добавляем ссылку
    
    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        
        movement = GetComponent<FirstPersonMovement>(); // Получаем компонент
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && damageSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        if (damageFlashEffect != null)
            damageFlashEffect.SetActive(false);
    }
    
    void Update()
    {
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
        
        // Используем SetMovementEnabled из FirstPersonMovement
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