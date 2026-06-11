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
    
    [Header("Aiming Walk Animation")]
    public bool enableAimingWalkAnimation = true;
    public string aimingWalkBoolParam = "IsAimingWalk"; // Ваш параметр для прицельной ходьбы
    public float aimingWalkSpeedMultiplier = 0.5f; // Множитель скорости при прицельной ходьбе

    [Header("Shoot Settings")]
    public string shootAnimationTrigger = "CrossbowShoot";

    [Header("Reload Settings")]
    public string reloadAnimationTrigger = "Reload";

    [Header("Melee Settings")]
    public string meleeAnimationTrigger = "MeleeAttack";

    [Header("UI")]
    public GameObject crosshairUI;
    
    [Header("Camera Bobbing")]
    public bool enableCameraBob = true;
    public float bobSpeed = 10f;
    public float bobAmount = 0.05f;
    private Transform cameraTransform;
    private float defaultCameraY;
    private float bobTimer = 0;

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
        
        // Инициализация камеры для бобинга
        if (enableCameraBob)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
                defaultCameraY = cameraTransform.localPosition.y;
            }
            else
            {
                Debug.LogWarning("Camera.main not found! Camera bobbing disabled.");
                enableCameraBob = false;
            }
        }
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
            
            // Инициализация параметра прицельной ходьбы
            if (enableAimingWalkAnimation)
            {
                animator.SetBool(aimingWalkBoolParam, false);
            }
        }
    }

    void Update()
    {
        IsRunning = false;
        HandleAiming();
        HandleShoot();
        HandleReload();
        HandleMelee();
        
        // Обработка покачивания камеры
        if (enableCameraBob && hasStarted && IsPlayerMoving() && !isAiming)
        {
            HandleCameraBob();
        }
        else if (enableCameraBob && hasStarted && IsPlayerMoving() && isAiming && enableAimingWalkAnimation)
        {
            // Лёгкое покачивание при прицельной ходьбе (опционально)
            HandleAimingCameraBob();
        }
        else if (enableCameraBob)
        {
            ResetCameraBob();
        }
    }

    void HandleAiming()
    {
        if (!hasStarted) return;
        if (animator == null) return;

        bool isRightMousePressed = Input.GetMouseButton(1);
        
        animator.SetBool(aimingBoolParam, isRightMousePressed);
        isAiming = isRightMousePressed;
        
        if (crosshairUI != null)
        {
            crosshairUI.SetActive(!isRightMousePressed);
        }
        
        // Обновляем состояние прицельной ходьбы при изменении состояния прицеливания
        if (enableAimingWalkAnimation && hasStarted)
        {
            bool isMoving = IsPlayerMoving();
            UpdateAimingWalkAnimation(isMoving);
        }
    }

    void HandleShoot()
    {
        if (!hasStarted) return;
        if (animator == null) return;
        
        if (Input.GetMouseButtonDown(0))
        {
            animator.SetTrigger(shootAnimationTrigger);
            Debug.Log("Выстрел - CrossbowShoot");
        }
    }

    void HandleReload()
    {
        if (!hasStarted) return;
        if (animator == null) return;
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            animator.SetTrigger(reloadAnimationTrigger);
            Debug.Log("Перезарядка - Reload");
        }
    }

    void HandleMelee()
    {
        if (!hasStarted) return;
        if (animator == null) return;
        
        if (Input.GetKeyDown(KeyCode.F))
        {
            animator.SetTrigger(meleeAnimationTrigger);
            Debug.Log("Удар арбалетом - MeleeAttack");
        }
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
        
        // Уменьшаем скорость при прицеливании, если включена опция
        if (isAiming && enableAimingWalkAnimation)
        {
            targetSpeed *= aimingWalkSpeedMultiplier;
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
            
            // Сбрасываем прицельную ходьбу
            if (enableAimingWalkAnimation)
            {
                animator.SetBool(aimingWalkBoolParam, false);
            }
        }
        else
        {
            // Обычная ходьба
            animator.SetFloat(movementSpeedParam, 1f);
            animator.SetBool(isWalkingParam, true);
            
            // Обновляем анимацию прицельной ходьбы
            if (enableAimingWalkAnimation)
            {
                UpdateAimingWalkAnimation(true);
            }
        }
    }
    
    // Новый метод для управления анимацией прицельной ходьбы
    void UpdateAimingWalkAnimation(bool isMoving)
    {
        if (animator == null) return;
        
        // Включаем IsAimingWalk только если: прицеливаемся И двигаемся
        bool shouldAimingWalk = isAiming && isMoving && enableAimingWalkAnimation;
        animator.SetBool(aimingWalkBoolParam, shouldAimingWalk);
        
        if (shouldAimingWalk)
        {
            Debug.Log("Прицельная ходьба активна - IsAimingWalk = true");
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
            
            if (enableAimingWalkAnimation)
            {
                animator.SetBool(aimingWalkBoolParam, false);
            }
        }
    }

    public bool IsAiming()
    {
        return isAiming;
    }
    
    // Новый метод для проверки движения
    public bool IsPlayerMoving()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        return Mathf.Abs(horizontal) > movementThreshold || Mathf.Abs(vertical) > movementThreshold;
    }
    
    // Методы для покачивания камеры
    private void HandleCameraBob()
    {
        if (cameraTransform == null) return;
        
        bobTimer += Time.deltaTime * bobSpeed;
        
        Vector3 newPos = cameraTransform.localPosition;
        newPos.y = defaultCameraY + Mathf.Sin(bobTimer) * bobAmount;
        
        cameraTransform.localPosition = newPos;
    }
    
    private void HandleAimingCameraBob()
    {
        if (cameraTransform == null) return;
        
        // Уменьшенное покачивание при прицельной ходьбе (50% от обычного)
        float aimingBobAmount = bobAmount * 0.5f;
        bobTimer += Time.deltaTime * bobSpeed;
        
        Vector3 newPos = cameraTransform.localPosition;
        newPos.y = defaultCameraY + Mathf.Sin(bobTimer) * aimingBobAmount;
        
        cameraTransform.localPosition = newPos;
    }
    
    private void ResetCameraBob()
    {
        if (cameraTransform == null) return;
        
        bobTimer = 0;
        Vector3 newPos = cameraTransform.localPosition;
        newPos.y = Mathf.Lerp(newPos.y, defaultCameraY, Time.deltaTime * 10f);
        cameraTransform.localPosition = newPos;
    }
}