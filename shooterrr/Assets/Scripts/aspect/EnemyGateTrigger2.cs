using UnityEngine;

public class EnemyGateTrigger2 : MonoBehaviour
{
    [Header("Gate Settings")]
    public GateController2 gateToOpen;    // Вторая калитка
    public float openDelay = 0.5f;
    
    private bool isTriggered = false;
    private Enemy enemyScript;
    
    void Start()
    {
        enemyScript = GetComponent<Enemy>();
        if (enemyScript == null)
            Debug.LogWarning("EnemyGateTrigger2: Enemy script not found on " + gameObject.name);
        
        if (gateToOpen == null)
            Debug.LogWarning("GateController2 not assigned on " + gameObject.name);
    }
    
    void Update()
    {
        if (isTriggered) return;
        if (enemyScript == null) return;
        
        // Если враг мертв (скрипт отключен)
        if (!enemyScript.enabled)
        {
            TriggerGate();
        }
    }
    
    void TriggerGate()
    {
        if (isTriggered) return;
        if (gateToOpen == null) return;
        
        isTriggered = true;
        Debug.Log($"Враг {gameObject.name} убит! Открываем вторую калитку через {openDelay} секунд");
        
        Invoke(nameof(OpenGate), openDelay);
    }
    
    void OpenGate()
    {
        if (gateToOpen != null)
            gateToOpen.OpenGate();
    }
}