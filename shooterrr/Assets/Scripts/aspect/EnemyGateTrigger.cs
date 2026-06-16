using UnityEngine;

public class EnemyGateTrigger : MonoBehaviour
{
    [Header("Gate Settings")]
    public GateController gateToOpen;
    public float openDelay = 0.5f;
    
    private bool isTriggered = false;
    private Enemy enemyScript;
    
    void Start()
    {
        enemyScript = GetComponent<Enemy>();
        Debug.Log($"EnemyGateTrigger: Start на {gameObject.name}, Enemy найден: {enemyScript != null}");
        
        if (gateToOpen == null)
            Debug.LogWarning("GateToOpen не назначен в инспекторе!");
        else
            Debug.Log($"GateToOpen назначен: {gateToOpen.name}");
    }
    
    void Update()
    {
        if (isTriggered) return;
        if (enemyScript == null) return;
        
        // Проверяем, отключен ли скрипт Enemy (значит враг умер)
        if (!enemyScript.enabled)
        {
            Debug.Log($"EnemyGateTrigger: Враг {gameObject.name} умер! Скрипт отключен.");
            TriggerGate();
        }
    }
    
    void TriggerGate()
    {
        if (isTriggered) return;
        if (gateToOpen == null)
        {
            Debug.LogError("GateToOpen не назначен!");
            return;
        }
        
        isTriggered = true;
        Debug.Log($"Враг {gameObject.name} убит! Открываем калитку через {openDelay} секунд");
        
        Invoke(nameof(OpenGate), openDelay);
    }
    
    void OpenGate()
    {
        Debug.Log($"OpenGate вызван! Калитка: {gateToOpen.name}");
        if (gateToOpen != null)
        {
            gateToOpen.OpenGate();
            Debug.Log("gateToOpen.OpenGate() вызван");
        }
    }
}