using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5;

    [Header("Running (disabled - for compatibility only)")]
    public bool canRun = false;
    public bool IsRunning { get; private set; }
    public float runSpeed = 10;
    public KeyCode runningKey = KeyCode.LeftShift;

    [Header("Animation")]
    public Animator animator;
    private string movementSpeedParam = "MovementSpeed";
    private string isWalkingParam = "IsWalking";

    [Header("Start Animation")]
    public string startAnimationTrigger = "Start";
    public float startAnimationDuration = 1.5f;
    private bool hasStarted = false;

    [Header("Movement Settings")]
    public float movementThreshold = 0.05f;

    [Header("Aiming Settings")]
    public string aimingBoolParam = "IsAiming";
    private bool isAiming = false;

    [Header("UI")]
    public GameObject crosshairUI; // Перетащи сюда объект прицела (Image, Canvas и т.д.)

    private Rigidbody rigidbody;
    private bool isWalking = false;

    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponent<Animator>();

        IsRunning = false;
    }

    void Start()
    {
        if (animator != null && !string.IsNullOrEmpty(startAnimationTrigger))
        {
            animator.SetTrigger(startAnimationTrigger);
        }

        StartCoroutine(EnableMovementAfterStart());
    }

    System.Collections.IEnumerator EnableMovementAfterStart()
    {
        yield return new WaitForSeconds(startAnimationDuration);
        hasStarted = true;

        if (animator != null)
        {
            animator.SetFloat(movementSpeedParam, 0f);
            animator.SetBool(isWalkingParam, false);
            animator.SetBool(aimingBoolParam, false);
        }
    }

    void Update()
    {
        IsRunning = false;
        HandleAiming();
    }

    void HandleAiming()
    {
        if (!hasStarted) return;
        if (animator == null) return;

        // Состояние правой кнопки мыши
        bool isRightMousePressed = Input.GetMouseButton(1);

        // Устанавливаем параметр анимации
        animator.SetBool(aimingBoolParam, isRightMousePressed);
        isAiming = isRightMousePressed;

        // Скрываем или показываем прицел
        if (crosshairUI != null)
        {
            crosshairUI.SetActive(!isRightMousePressed); // Скрываем при прицеливании
        }

        // Отладка (можно закомментировать)
        if (Input.GetMouseButtonDown(1))
            Debug.Log("Прицеливание включено, прицел скрыт");
        if (Input.GetMouseButtonUp(1))
            Debug.Log("Прицеливание выключено, прицел показан");
    }

    void FixedUpdate()
    {
        if (!hasStarted) return;

        IsRunning = false;

        float targetSpeed = walkSpeed;
        if (speedOverrides.Count > 0)
        {
            targetSpeed = speedOverrides[speedOverrides.Count - 1]();
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        bool isMoving = Mathf.Abs(horizontal) > movementThreshold || Mathf.Abs(vertical) > movementThreshold;

        if (isMoving)
        {
            Vector3 movement = transform.right * horizontal + transform.forward * vertical;
            movement = movement.normalized * targetSpeed;
            movement.y = rigidbody.velocity.y;
            rigidbody.velocity = movement;
        }
        else
        {
            rigidbody.velocity = new Vector3(0, rigidbody.velocity.y, 0);
        }

        UpdateAnimations(isMoving);
    }

    void UpdateAnimations(bool isMoving)
    {
        if (animator == null) return;

        if (!isMoving)
        {
            animator.SetFloat(movementSpeedParam, 0f);
            animator.SetBool(isWalkingParam, false);
        }
        else
        {
            animator.SetFloat(movementSpeedParam, 1f);
            animator.SetBool(isWalkingParam, true);
        }
    }

    public void PlayStartAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger(startAnimationTrigger);
            hasStarted = false;
            StartCoroutine(EnableMovementAfterStart());
        }
    }

    public void SetMovementEnabled(bool enabled)
    {
        hasStarted = enabled;
        if (!enabled && animator != null)
        {
            animator.SetFloat(movementSpeedParam, 0);
            animator.SetBool(isWalkingParam, false);
            animator.SetBool(aimingBoolParam, false);
        }
    }

    public bool IsAiming()
    {
        return isAiming;
    }
}