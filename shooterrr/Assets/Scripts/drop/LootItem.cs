using UnityEngine;

public class LootItem : MonoBehaviour
{
    [Header("Loot Settings")]
    public string itemName = "Bolts";
    public int amount = 5;
    public float pickupRange = 2f;
    public KeyCode pickupKey = KeyCode.E;
    
    [Header("Effects")]
    public GameObject pickupEffect;
    public AudioClip pickupSound;
    
    private Transform player;
    private AudioSource audioSource;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && pickupSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        // Опускаем лут на пол
        PlaceOnGround();
    }
    
    void PlaceOnGround()
    {
        // Бросаем луч вниз, чтобы найти пол
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 2f))
        {
            // Ставим объект прямо на пол
            transform.position = hit.point + Vector3.up * 0.05f;
        }
        
        // Выравниваем поворот (чтобы лежал ровно)
        transform.rotation = Quaternion.identity;
    }
    
    void Update()
    {
        // НИКАКОГО ВРАЩЕНИЯ - просто проверка на подбор
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            
            if (distance <= pickupRange && Input.GetKeyDown(pickupKey))
            {
                Pickup();
            }
        }
    }
    
    void Pickup()
    {
        // Находим скрипт оружия и добавляем болты
        if (player != null)
        {
            MedievalWeapon weapon = player.GetComponentInChildren<MedievalWeapon>();
            if (weapon != null)
            {
                weapon.AddBolts(amount);
                Debug.Log($"Подобрано {amount} болтов!");
            }
        }
        
        // Эффекты
        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        
        if (pickupSound != null && audioSource != null)
            audioSource.PlayOneShot(pickupSound);
        
        // Уничтожаем лут
        Destroy(gameObject, 0.1f);
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}