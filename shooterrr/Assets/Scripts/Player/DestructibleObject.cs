using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestructibleObject : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 50f;
    private float currentHealth;
    
    [Header("Effects")]
    public GameObject destroyedVersion;
    public GameObject hitEffect;
    public AudioClip destroySound;
    public AudioClip hitSound;
    
    [Header("Loot")]
    public GameObject[] lootItems;
    public float lootDropChance = 0.5f;
    
    [Header("Destruction")]
    public bool destroyOnDeath = true;
    public float destructionDelay = 0.1f;
    
    private AudioSource audioSource;
    
    void Start()
    {
        currentHealth = maxHealth;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (destroySound != null || hitSound != null))
            audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"{gameObject.name} получил {damage} урона. Осталось здоровья: {currentHealth}");
        
        // Эффект попадания
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Звук попадания
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
        
        if (currentHealth <= 0)
        {
            DestroyObject();
        }
    }
    
    void DestroyObject()
    {
        Debug.Log($"Объект {gameObject.name} разрушен!");
        
        // Звук разрушения
        if (destroySound != null && audioSource != null)
        {
            audioSource.PlayOneShot(destroySound);
        }
        
        // Спавн разрушенной версии
        if (destroyedVersion != null)
        {
            GameObject destroyed = Instantiate(destroyedVersion, transform.position, transform.rotation);
            // Копируем позицию и поворот
            destroyed.transform.position = transform.position;
            destroyed.transform.rotation = transform.rotation;
        }
        
        // Выпадение лута
        if (lootItems.Length > 0)
        {
            foreach (GameObject loot in lootItems)
            {
                if (Random.value <= lootDropChance)
                {
                    Instantiate(loot, transform.position, Quaternion.identity);
                }
            }
        }
        
        if (destroyOnDeath)
        {
            Destroy(gameObject, destructionDelay);
        }
    }
    
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        Debug.Log($"{gameObject.name} восстановил {amount} здоровья. Текущее здоровье: {currentHealth}");
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
}