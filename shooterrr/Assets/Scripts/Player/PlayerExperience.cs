using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerExperience : MonoBehaviour
{
    public int currentLevel = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100;
    
    public Slider xpSlider;
    public Text levelText;
    
    void Start()
    {
        UpdateUI();
    }
    
    public void AddExperience(int xp)
    {
        currentXP += xp;
        Debug.Log($"Получено {xp} опыта! Всего: {currentXP}");
        
        if (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }
        
        UpdateUI();
    }
    
    void LevelUp()
    {
        currentLevel++;
        currentXP -= xpToNextLevel;
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.2f);
        
        Debug.Log($"Уровень повышен! Текущий уровень: {currentLevel}");
        
        // Лечение при повышении уровня
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.Heal(50);
        }
        
        UpdateUI();
    }
    
    void UpdateUI()
    {
        if (xpSlider != null)
            xpSlider.value = (float)currentXP / xpToNextLevel;
            
        if (levelText != null)
            levelText.text = $"Level {currentLevel}";
    }
}