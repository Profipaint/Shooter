using UnityEngine;
using System.Collections;

public class BossDeathUI : MonoBehaviour
{
    [Header("Boss Reference")]
    public BossEnemy boss;
    
    [Header("UI Object")]
    public GameObject uiObject;
    public float delayAfterDeath = 12f;
    public float fadeDuration = 1.5f;
    
    private CanvasGroup canvasGroup;
    private bool isTriggered = false;
    
    void Start()
    {
        Debug.Log("=== BossDeathUI Start ===");
        
        if (boss == null)
        {
            boss = FindObjectOfType<BossEnemy>();
            Debug.Log($"Boss найден: {(boss != null ? boss.name : "НЕ НАЙДЕН!")}");
        }
        
        if (uiObject != null)
        {
            Debug.Log($"UI Object: {uiObject.name}, активен: {uiObject.activeSelf}");
            uiObject.SetActive(false);
            
            canvasGroup = uiObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = uiObject.AddComponent<CanvasGroup>();
            
            canvasGroup.alpha = 0f;
        }
        else
        {
            Debug.LogError("UI Object не назначен в инспекторе!");
        }
    }
    
    void Update()
    {
        if (isTriggered) return;
        if (boss == null) return;
        
        float health = boss.GetCurrentHealth();
        Debug.Log($"Boss health: {health}");
        
        if (health <= 0)
        {
            isTriggered = true;
            Debug.Log($"Boss умер! Через {delayAfterDeath} секунд появится UI");
            StartCoroutine(ShowUIFade());
        }
    }
    
    IEnumerator ShowUIFade()
    {
        Debug.Log("=== ShowUIFade START ===");
        
        yield return new WaitForSeconds(delayAfterDeath);
        
        Debug.Log($"Через {delayAfterDeath} секунд. Показываю UI!");
        
        if (uiObject != null)
        {
            uiObject.SetActive(true);
            Debug.Log($"UI Object активен: {uiObject.activeSelf}");
            
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                    Debug.Log($"Alpha: {canvasGroup.alpha}");
                }
                yield return null;
            }
            
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
            
            Debug.Log("UI плавно появился!");
        }
        else
        {
            Debug.LogError("uiObject == null в ShowUIFade!");
        }
    }
}