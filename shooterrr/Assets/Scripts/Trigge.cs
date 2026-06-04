using UnityEngine;

public class Trigge : MonoBehaviour
{
    [SerializeField] private GameObject Tri;
    [SerializeField] private GameObject Crossbow;
    [SerializeField] private GameObject Ui;
    

    void Start()
    {
        if (Tri != null) Tri.SetActive(true);
        if (Crossbow != null) Crossbow.SetActive(false);
        if (Ui != null) Ui.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (Tri != null) Tri.SetActive(false);
        if (Crossbow != null) Crossbow.SetActive(true);
        if (Ui != null) Ui.SetActive(true);
        Debug.Log("Сработало!");
    }
}