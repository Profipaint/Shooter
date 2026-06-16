using System.Collections;
using UnityEngine;

public class GateController : MonoBehaviour
{
    [Header("Gate Settings")]
    public string gateKeeperTag = "GateKeeper";
    public float openSpeed = 2f;
    public float openAngle = 90f;
    
    [Header("Door Hinges (Петли)")]
    public Transform leftHinge;   // Родительский объект левой створки (на месте петли)
    public Transform rightHinge;  // Родительский объект правой створки (на месте петли)
    
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
        // Запоминаем начальные повороты (закрытое состояние) для петель
        if (leftHinge != null)
        {
            leftClosedRotation = leftHinge.localRotation;
            // Левая створка открывается влево
            leftOpenRotation = leftClosedRotation * Quaternion.Euler(0, -openAngle, 0);
        }
        
        if (rightHinge != null)
        {
            rightClosedRotation = rightHinge.localRotation;
            // Правая створка открывается вправо
            rightOpenRotation = rightClosedRotation * Quaternion.Euler(0, openAngle, 0);
        }
        
        Debug.Log($"GateController: калитка {gameObject.name} готова");
        InvokeRepeating(nameof(CheckGateKeeper), 2f, 2f);
    }
    
    void Update()
    {
        if (!isMoving) return;
        if (leftHinge == null && rightHinge == null) return;
        
        // Плавно вращаем петли
        if (leftHinge != null)
        {
            Quaternion leftTarget = isOpen ? leftOpenRotation : leftClosedRotation;
            leftHinge.localRotation = Quaternion.Slerp(leftHinge.localRotation, leftTarget, Time.deltaTime * openSpeed);
        }
        
        if (rightHinge != null)
        {
            Quaternion rightTarget = isOpen ? rightOpenRotation : rightClosedRotation;
            rightHinge.localRotation = Quaternion.Slerp(rightHinge.localRotation, rightTarget, Time.deltaTime * openSpeed);
        }
        
        // Проверяем, достигли ли цели
        float leftAngle = leftHinge != null ? Quaternion.Angle(leftHinge.localRotation, isOpen ? leftOpenRotation : leftClosedRotation) : 0;
        float rightAngle = rightHinge != null ? Quaternion.Angle(rightHinge.localRotation, isOpen ? rightOpenRotation : rightClosedRotation) : 0;
        
        if (leftAngle < 0.5f && rightAngle < 0.5f)
        {
            if (leftHinge != null)
                leftHinge.localRotation = isOpen ? leftOpenRotation : leftClosedRotation;
            if (rightHinge != null)
                rightHinge.localRotation = isOpen ? rightOpenRotation : rightClosedRotation;
            
            isMoving = false;
            Debug.Log($"Калитка {(isOpen ? "ОТКРЫТА" : "ЗАКРЫТА")}!");
        }
    }
    
    void CheckGateKeeper()
    {
        if (isTriggered) return;
        if (isOpen) return;
        
        GameObject gateKeeper = GameObject.FindGameObjectWithTag(gateKeeperTag);
        
        if (gateKeeper == null)
        {
            Debug.Log($"GateKeeper не найден! Открываем калитку!");
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
        
        Debug.Log("Калитка открывается...");
    }
    
    public void CloseGate()
    {
        if (!isOpen) return;
        isOpen = false;
        isMoving = true;
        Debug.Log("Калитка закрывается...");
    }
    
    void OnDestroy()
    {
        CancelInvoke(nameof(CheckGateKeeper));
    }
}