using UnityEngine;

public class QuitGame : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(1)) // Правая кнопка мыши
        {
            Debug.Log("Выход из игры!");
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}