using UnityEngine;

public class Trigger : MonoBehaviour
{
    [SerializeField] private GameObject Tri;
    [SerializeField] private GameObject Crossbow;

    void Start()
    {
        if (Tri != null) Tri.SetActive(true);
        if (Crossbow != null) Crossbow.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (Tri != null) Tri.SetActive(false);
        if (Crossbow != null) Crossbow.SetActive(true);
        Debug.Log("Сработало!");
    }
}