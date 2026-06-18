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
        if (boss == null)
            boss = FindObjectOfType<BossEnemy>();
        
        if (uiObject != null)
        {
            uiObject.SetActive(false);
            
            canvasGroup = uiObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = uiObject.AddComponent<CanvasGroup>();
            
            canvasGroup.alpha = 0f;
        }
    }
    
    void Update()
    {
        if (isTriggered) return;
        if (boss == null) return;
        
        if (boss.GetCurrentHealth() <= 0)
        {
            isTriggered = true;
            Debug.Log($"Boss умер! Через {delayAfterDeath} секунд появится UI");
            StartCoroutine(ShowUIFade());
        }
    }
    
    IEnumerator ShowUIFade()
    {
        yield return new WaitForSeconds(delayAfterDeath);
        
        if (uiObject != null)
        {
            uiObject.SetActive(true);
            
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                yield return null;
            }
            
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
            
            Debug.Log("UI появился!");
        }
    }
}