using UnityEngine;

public class LootItem : MonoBehaviour
{
    public int ammoAmount = 5;
    public int thrownAmount = 1;
    public float pickupRange = 2f;
    
    private bool isPickedUp = false;
    private Transform player;
    private bool isNear = false;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }
    
    void Update()
    {
        if (isPickedUp) return;
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        isNear = distance <= pickupRange;
        
        if (isNear && Input.GetKeyDown(KeyCode.E))
        {
            PickUp();
        }
    }
    
    void PickUp()
    {
        if (isPickedUp) return;
        isPickedUp = true;
        
        UniversalWeapon weapon = player.GetComponentInChildren<UniversalWeapon>();
        
        if (weapon != null)
        {
            weapon.AddBolts(ammoAmount);
            weapon.AddThrownWeapons(thrownAmount);
            Debug.Log($"Подобрано: болты +{ammoAmount}, метательное +{thrownAmount}");
        }
        else
        {
            Debug.LogWarning("UniversalWeapon не найден на оружии игрока!");
        }
        
        Destroy(gameObject, 0.1f);
    }
    
    // Подсказка "E" над лутом
    void OnGUI()
    {
        if (!isNear || isPickedUp) return;
        if (Camera.main == null) return;
        
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1f);
        
        // Если объект за камерой — не показываем
        if (screenPos.z < 0) return;
        
        GUI.Label(
            new Rect(screenPos.x - 50, Screen.height - screenPos.y - 20, 100, 30),
            "[E] Подобрать",
            new GUIStyle()
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            }
        );
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}