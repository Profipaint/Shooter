using UnityEngine;

public class HideUI : MonoBehaviour
{
    public GameObject targetUI;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Левая кнопка мыши
        {
            if (targetUI != null)
                targetUI.SetActive(!targetUI.activeSelf);
        }
    }
}