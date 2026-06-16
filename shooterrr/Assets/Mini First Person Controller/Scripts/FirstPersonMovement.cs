using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5;
    public float runSpeed = 10;

    [Header("Stamina System")]
    public bool enableStamina = true;
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaDrainRate = 20f;      // Скорость траты стамины при беге (в секунду)
    public float staminaRegenRate = 15f;      // Скорость восстановления стамины (в секунду)
    public float staminaRegenDelay = 1f;      // Задержка перед восстановлением после бега
    private float staminaRegenTimer = 0f;
    private bool isRunning = false;
    
    [Header("Stamina UI")]
    public UnityEngine.UI.Slider staminaSlider;
    public UnityEngine.UI.Text staminaText;
    
    [Header("Running (disabled - for compatibility only)")]
    public bool canRun = true;
    public bool IsRunning { get; private set; }
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
    public string aimingWalkBoolParam = "IsAimingWalk";
    public float aimingWalkSpeedMultiplier = 0.5f;

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
    
    [Header("Idle Camera Sway")]
    public bool enableIdleSway = true;
    public float idleSwaySpeed = 3f;
    public float idleSwayAmount = 0.015f;
    public float idleSwayHorizontal = 0.01f;
    
    [Header("Jump Camera Effect")]
    public bool enableJumpEffect = true;
    public float jumpDownOffset = 0.08f;
    public float jumpEffectDuration = 0.15f;
    
    private Transform cameraTransform;
    private float defaultCameraY;
    private float defaultCameraX;
    private float bobTimer = 0;
    private float idleSwayTimer = 0;

    private Rigidbody rigidbody;

    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponent<Animator>();

        IsRunning = false;
        currentStamina = maxStamina;
    }

    void Start()
    {
        if (animator != null && !string.IsNullOrEmpty(startAnimationTrigger))
        {
            animator.SetTrigger(startAnimationTrigger);
        }

        StartCoroutine(EnableMovementAfterStart());
        
        if (enableCameraBob)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
                defaultCameraY = cameraTransform.localPosition.y;
                defaultCameraX = cameraTransform.localPosition.x;
            }
            else
            {
                Debug.LogWarning("Camera.main not found! Camera bobbing disabled.");
                enableCameraBob = false;
            }
        }
        
        UpdateStaminaUI();
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
        HandleJumpEffect();
        HandleStamina();
        
        bool isMoving = IsPlayerMoving();
        
        if (enableCameraBob && hasStarted)
        {
            if (isMoving && !isAiming)
            {
                HandleCameraBob();
            }
            else if (isMoving && isAiming && enableAimingWalkAnimation)
            {
                HandleAimingCameraBob();
            }
            else if (!isMoving && enableIdleSway)
            {
                HandleIdleSway();
            }
            else
            {
                ResetCameraBob();
            }
        }
    }

    void HandleStamina()
    {
        if (!enableStamina) return;
        
        bool isShiftPressed = Input.GetKey(runningKey);
        bool isMoving = IsPlayerMoving();
        
        // Проверяем, можно ли бежать
        bool canRunNow = canRun && isShiftPressed && isMoving && !isAiming && currentStamina > 0;
        
        if (canRunNow)
        {
            // Тратим стамину
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(0, currentStamina);
            isRunning = true;
            IsRunning = true;
            
            // Сбрасываем таймер регенерации
            staminaRegenTimer = staminaRegenDelay;
        }
        else
        {
            isRunning = false;
            IsRunning = false;
            
            // Восстанавливаем стамину с задержкой
            if (staminaRegenTimer > 0)
            {
                staminaRegenTimer -= Time.deltaTime;
            }
            else if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(maxStamina, currentStamina);
            }
        }
        
        UpdateStaminaUI();
    }

    void UpdateStaminaUI()
    {
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }
        
        if (staminaText != null)
        {
            staminaText.text = $"{Mathf.Round(currentStamina)}%";
        }
    }

    public bool HasStamina()
    {
        return currentStamina > 0;
    }

    public float GetStaminaPercentage()
    {
        return currentStamina / maxStamina;
    }

    public void AddStamina(float amount)
    {
        currentStamina += amount;
        currentStamina = Mathf.Min(maxStamina, currentStamina);
        UpdateStaminaUI();
    }

    void HandleJumpEffect()
    {
        if (!hasStarted) return;
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (enableJumpEffect && cameraTransform != null)
            {
                StartCoroutine(JumpCameraCoroutine());
            }
        }
    }

    IEnumerator JumpCameraCoroutine()
    {
        float elapsed = 0f;
        Vector3 originalPos = cameraTransform.localPosition;
        Vector3 downPos = originalPos;
        downPos.y -= jumpDownOffset;
        
        while (elapsed < jumpEffectDuration / 2)
        {
            float t = elapsed / (jumpEffectDuration / 2);
            cameraTransform.localPosition = Vector3.Lerp(originalPos, downPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        cameraTransform.localPosition = downPos;
        
        elapsed = 0f;
        while (elapsed < jumpEffectDuration / 2)
        {
            float t = elapsed / (jumpEffectDuration / 2);
            cameraTransform.localPosition = Vector3.Lerp(downPos, originalPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        cameraTransform.localPosition = originalPos;
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
        
        // Проверяем, можем ли бежать
        bool canRunNow = canRun && isRunning && !isAiming && currentStamina > 0;
        
        if (canRunNow)
        {
            targetSpeed = runSpeed;
            IsRunning = true;
        }
        
        if (speedOverrides.Count > 0)
        {
            targetSpeed = speedOverrides[speedOverrides.Count - 1]();
        }
        
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
            
            if (enableAimingWalkAnimation)
            {
                animator.SetBool(aimingWalkBoolParam, false);
            }
        }
        else
        {
            // Если бежим - скорость анимации выше
            float speedValue = isRunning ? 2f : 1f;
            animator.SetFloat(movementSpeedParam, speedValue);
            animator.SetBool(isWalkingParam, true);
            
            if (enableAimingWalkAnimation)
            {
                UpdateAimingWalkAnimation(true);
            }
        }
    }
    
    void UpdateAimingWalkAnimation(bool isMoving)
    {
        if (animator == null) return;
        
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
    
    public bool IsPlayerMoving()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        return Mathf.Abs(horizontal) > movementThreshold || Mathf.Abs(vertical) > movementThreshold;
    }
    
    private void HandleCameraBob()
    {
        if (cameraTransform == null) return;
        
        // Увеличиваем скорость бобинга при беге
        float currentBobSpeed = isRunning ? bobSpeed * 1.5f : bobSpeed;
        float currentBobAmount = isRunning ? bobAmount * 1.2f : bobAmount;
        
        bobTimer += Time.deltaTime * currentBobSpeed;
        
        Vector3 newPos = cameraTransform.localPosition;
        newPos.y = defaultCameraY + Mathf.Sin(bobTimer) * currentBobAmount;
        
        cameraTransform.localPosition = newPos;
    }
    
    private void HandleAimingCameraBob()
    {
        if (cameraTransform == null) return;
        
        float aimingBobAmount = bobAmount * 0.5f;
        bobTimer += Time.deltaTime * bobSpeed;
        
        Vector3 newPos = cameraTransform.localPosition;
        newPos.y = defaultCameraY + Mathf.Sin(bobTimer) * aimingBobAmount;
        
        cameraTransform.localPosition = newPos;
    }
    
    private void HandleIdleSway()
    {
        if (cameraTransform == null) return;
        
        idleSwayTimer += Time.deltaTime * idleSwaySpeed;
        
        Vector3 newPos = cameraTransform.localPosition;
        
        newPos.y = defaultCameraY + Mathf.Sin(idleSwayTimer) * idleSwayAmount;
        newPos.x = defaultCameraX + Mathf.Sin(idleSwayTimer * 0.7f) * idleSwayHorizontal;
        
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, newPos, Time.deltaTime * 5f);
    }
    
    private void ResetCameraBob()
    {
        if (cameraTransform == null) return;
        
        bobTimer = 0;
        idleSwayTimer = 0;
        
        Vector3 newPos = cameraTransform.localPosition;
        newPos.y = Mathf.Lerp(newPos.y, defaultCameraY, Time.deltaTime * 10f);
        newPos.x = Mathf.Lerp(newPos.x, defaultCameraX, Time.deltaTime * 10f);
        
        cameraTransform.localPosition = newPos;
    }
}