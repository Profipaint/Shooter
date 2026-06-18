using System.Collections;
using UnityEngine;

public class GateController2 : MonoBehaviour
{
    [Header("Gate Settings")]
    public string gateKeeperTag = "GateKeeper2";  // СВОЙ ТЕГ
    public float openAngle = 90f;
    public float openSpeed = 2f;
    
    [Header("Door Parts")]
    public Transform leftDoor;
    public Transform rightDoor;
    
    [Header("Audio")]
    public AudioClip openSound;
    public AudioSource audioSource;
    
    private Quaternion leftClosedRotation;
    private Quaternion rightClosedRotation;
    private Quaternion leftOpenRotation;
    private Quaternion rightOpenRotation;
    private bool isOpen = false;
    private bool isMoving = false;
    private bool isTriggered = false;
    
    void Start()
    {
        if (leftDoor != null)
        {
            leftClosedRotation = leftDoor.localRotation;
            leftOpenRotation = leftClosedRotation * Quaternion.Euler(0, -openAngle, 0);
        }
        
        if (rightDoor != null)
        {
            rightClosedRotation = rightDoor.localRotation;
            rightOpenRotation = rightClosedRotation * Quaternion.Euler(0, openAngle, 0);
        }
        
        Debug.Log($"GateController2: калитка {gameObject.name} готова. Ждем врага с тегом '{gateKeeperTag}'");
        InvokeRepeating(nameof(CheckGateKeeper), 2f, 2f);
    }
    
    void Update()
    {
        if (!isMoving) return;
        if (leftDoor == null && rightDoor == null) return;
        
        if (leftDoor != null)
        {
            Quaternion leftTarget = isOpen ? leftOpenRotation : leftClosedRotation;
            leftDoor.localRotation = Quaternion.Slerp(leftDoor.localRotation, leftTarget, Time.deltaTime * openSpeed);
        }
        
        if (rightDoor != null)
        {
            Quaternion rightTarget = isOpen ? rightOpenRotation : rightClosedRotation;
            rightDoor.localRotation = Quaternion.Slerp(rightDoor.localRotation, rightTarget, Time.deltaTime * openSpeed);
        }
        
        float leftAngle = leftDoor != null ? Quaternion.Angle(leftDoor.localRotation, isOpen ? leftOpenRotation : leftClosedRotation) : 0;
        float rightAngle = rightDoor != null ? Quaternion.Angle(rightDoor.localRotation, isOpen ? rightOpenRotation : rightClosedRotation) : 0;
        
        if (leftAngle < 0.5f && rightAngle < 0.5f)
        {
            if (leftDoor != null)
                leftDoor.localRotation = isOpen ? leftOpenRotation : leftClosedRotation;
            if (rightDoor != null)
                rightDoor.localRotation = isOpen ? rightOpenRotation : rightClosedRotation;
            
            isMoving = false;
            Debug.Log($"Калитка2 {(isOpen ? "ОТКРЫТА" : "ЗАКРЫТА")}!");
        }
    }
    
    void CheckGateKeeper()
    {
        if (isTriggered) return;
        if (isOpen) return;
        
        GameObject gateKeeper = GameObject.FindGameObjectWithTag(gateKeeperTag);
        
        if (gateKeeper == null)
        {
            Debug.Log($"GateKeeper2 '{gateKeeperTag}' НЕ НАЙДЕН! Открываем калитку2!");
            OpenGate();
            isTriggered = true;
        }
    }
    
    public void OpenGate()
    {
        if (isOpen) return;
        isOpen = true;
        isMoving = true;
        
        if (openSound != null && audioSource != null)
            audioSource.PlayOneShot(openSound);
        
        Debug.Log("Калитка2 открывается...");
    }
    
    void OnDestroy()
    {
        CancelInvoke(nameof(CheckGateKeeper));
    }
}