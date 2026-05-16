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
    public float movementThreshold = 0.05f; // Порог чувствительности движения

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
        }
    }

    void Update()
    {
        // Обновляем IsRunning в Update для совместимости
        IsRunning = false;
    }

    void FixedUpdate()
    {
        if (!hasStarted) return;

        IsRunning = false;

        // Получаем целевые параметры движения
        float targetSpeed = walkSpeed;
        if (speedOverrides.Count > 0)
        {
            targetSpeed = speedOverrides[speedOverrides.Count - 1]();
        }

        // Получаем ввод
        float horizontal = Input.GetAxisRaw("Horizontal"); // Используем GetAxisRaw для мгновенной реакции
        float vertical = Input.GetAxisRaw("Vertical");

        // Проверяем движение с порогом чувствительности
        bool isMoving = Mathf.Abs(horizontal) > movementThreshold || Mathf.Abs(vertical) > movementThreshold;

        // Применяем движение
        if (isMoving)
        {
            Vector3 movement = transform.right * horizontal + transform.forward * vertical;
            movement = movement.normalized * targetSpeed; // Нормализуем для диагонального движения
            movement.y = rigidbody.velocity.y;
            rigidbody.velocity = movement;
        }
        else
        {
            // Останавливаем движение по горизонтали, сохраняем вертикальную скорость (гравитация)
            rigidbody.velocity = new Vector3(0, rigidbody.velocity.y, 0);
        }

        // Обновляем анимации
        UpdateAnimations(isMoving);

        // Для отладки (можно убрать после проверки)
        // Debug.Log($"Moving: {isMoving}, Horizontal: {horizontal}, Vertical: {vertical}");
    }

    void UpdateAnimations(bool isMoving)
    {
        if (animator == null) return;

        // Анимация стояния
        if (!isMoving)
        {
            animator.SetFloat(movementSpeedParam, 0f);
            animator.SetBool(isWalkingParam, false);
        }
        // Анимация ходьбы
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
        }
    }
}