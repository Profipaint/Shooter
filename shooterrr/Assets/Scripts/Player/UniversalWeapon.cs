using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType
{
    Crossbow,
    Thrown,
    Melee
}

public class MedievalWeapon : MonoBehaviour
{
    [Header("Weapon Type")]
    public WeaponType weaponType = WeaponType.Crossbow;
    
    [Header("Damage Settings")]
    public float damage = 35f;
    public float meleeDamage = 50f;
    
    [Header("Range Settings")]
    public float crossbowRange = 80f;
    public float thrownRange = 30f;
    public float meleeRange = 2.5f;
    
    [Header("Fire Rate / Attack Speed")]
    public float crossbowReloadTime = 1.5f;
    public float thrownCooldown = 0.8f;
    public float meleeAttackSpeed = 0.6f;
    private float nextAttackTime = 0f;
    
    [Header("Crossbow Settings")]
    public GameObject boltPrefab;
    public Transform shootPoint;
    public GameObject crossbowMuzzleFlash;
    public float boltSpeed = 50f;
    public float crossbowRecoil = 1f;
    public int boltsPerShot = 1;
    public float spreadAngle = 2f;
    
    [Header("Aiming Settings")]
    public float aimingSpeedMultiplier = 0.5f;
    public bool enableAimingShake = true;
    public float aimingShakeAmount = 0.02f;
    public float aimingShakeSpeed = 8f;
    private float aimingShakeTimer = 0f;
    private Vector3 originalCameraPos;
    
    [Header("Weapon Position Sway (Покачивание позиции)")]
    public bool enablePositionSway = true;
    public float positionSwayAmount = 0.02f;
    public float positionSwaySmoothness = 6f;
    public float positionSwayClampX = 0.05f;
    public float positionSwayClampY = 0.05f;
    private Vector3 initialPosition;
    private Vector3 swayPosition;
    
    [Header("Weapon Rotation Sway (Покачивание поворота)")]
    public bool enableRotationSway = true;
    public float rotationSwayAmount = 2f;        // Сила поворота
    public float rotationSwaySmoothness = 8f;    // Плавность
    public float rotationSwayClampX = 3f;        // Максимальный поворот по X
    public float rotationSwayClampY = 3f;        // Максимальный поворот по Y
    public float rotationSwayClampZ = 1.5f;      // Максимальный поворот по Z (наклон)
    private Quaternion initialRotation;
    private Quaternion swayRotation;
    
    [Header("Weapon Inertia (Инерция)")]
    public bool enableInertia = true;
    public float inertiaAmount = 0.5f;           // Сила инерции
    public float inertiaSmoothness = 5f;         // Плавность инерции
    private Vector3 inertiaVelocity;
    private Vector3 lastMousePosition;
    
    [Header("Thrown Weapons")]
    public GameObject thrownPrefab;
    public int thrownCount = 5;
    private int currentThrown;
    public float thrownArcHeight = 2f;
    public GameObject thrownEffect;
    
    [Header("Melee Settings")]
    public GameObject slashEffect;
    public float cameraShakeAmount = 0.08f;
    public float cameraShakeDuration = 0.1f;
    public LayerMask meleeLayers;
    public bool heavyWeapon = false;
    
    [Header("Effects")]
    public GameObject hitEffect;
    public GameObject impactEffect;
    public AudioClip shootSound;
    public AudioClip thrownSound;
    public AudioClip meleeSound;
    public AudioClip hitSound;
    public AudioClip reloadSound;
    
    [Header("Ammo System")]
    public bool useAmmo = true;
    public int maxBolts = 1;
    public int currentBolts;
    public int reserveBolts = 30;
    public float reloadDuration = 1.5f;
    private bool isReloading = false;
    
    [Header("UI")]
    public UnityEngine.UI.Text ammoText;
    public UnityEngine.UI.Slider ammoSlider;
    public UnityEngine.UI.Text thrownText;
    
    [Header("Camera")]
    public Camera playerCamera;
    
    [Header("Animation")]
    public Animator animator;
    
    [Header("Crosshair")]
    public GameObject crossbowCrosshair;
    public GameObject thrownCrosshair;
    public GameObject meleeCrosshair;
    
    private bool isAiming = false;
    private FirstPersonMovement playerMovement;
    private float originalWalkSpeed;
    
    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        
        if (playerCamera != null)
        {
            originalCameraPos = playerCamera.transform.localPosition;
        }
        
        playerMovement = GetComponent<FirstPersonMovement>();
        
        if (playerMovement != null)
        {
            originalWalkSpeed = playerMovement.walkSpeed;
        }
        
        if (useAmmo && weaponType == WeaponType.Crossbow)
            currentBolts = maxBolts;
        
        if (weaponType == WeaponType.Thrown)
            currentThrown = thrownCount;
        
        // Сохраняем начальную позицию и поворот для Sway
        initialPosition = transform.localPosition;
        swayPosition = initialPosition;
        initialRotation = transform.localRotation;
        swayRotation = initialRotation;
        
        UpdateUI();
        UpdateCrosshair();
    }
    
    void Update()
    {
        UpdateMovementAnimations();
        HandleSway();
        HandleInertia();
        
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SwitchWeapon(WeaponType.Crossbow);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SwitchWeapon(WeaponType.Melee);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            SwitchWeapon(WeaponType.Thrown);
        
        if (weaponType == WeaponType.Crossbow)
        {
            bool isRightMousePressed = Input.GetMouseButton(1);
            if (isRightMousePressed != isAiming)
            {
                isAiming = isRightMousePressed;
                
                if (animator != null)
                    animator.SetBool("IsAiming", isAiming);
                
                if (playerMovement != null)
                {
                    if (isAiming)
                    {
                        playerMovement.walkSpeed = originalWalkSpeed * aimingSpeedMultiplier;
                        aimingShakeTimer = 0f;
                    }
                    else
                    {
                        playerMovement.walkSpeed = originalWalkSpeed;
                        if (playerCamera != null && enableAimingShake)
                        {
                            playerCamera.transform.localPosition = originalCameraPos;
                        }
                    }
                }
                
                UpdateCrosshair();
            }
            
            if (isAiming && enableAimingShake && playerCamera != null)
            {
                HandleAimingShake();
            }
            
            if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime && !isReloading && currentBolts > 0)
            {
                ShootCrossbow();
            }
            
            if (Input.GetKeyDown(KeyCode.R) && useAmmo && !isReloading && currentBolts < maxBolts && reserveBolts > 0)
            {
                StartCoroutine(ReloadCrossbow());
            }
            
            if (Input.GetKeyDown(KeyCode.F) && Time.time >= nextAttackTime)
            {
                MeleeAttack();
            }
        }
        else if (weaponType == WeaponType.Thrown)
        {
            if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime && currentThrown > 0)
            {
                ThrowWeapon();
            }
            
            if (Input.GetKeyDown(KeyCode.F) && Time.time >= nextAttackTime)
            {
                MeleeAttack();
            }
        }
        else if (weaponType == WeaponType.Melee)
        {
            if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
            {
                MeleeAttack();
            }
        }
        
        UpdateUI();
    }
    
    void HandleSway()
    {
        // === ПОЗИЦИОННЫЙ SWAY ===
        if (enablePositionSway)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            
            float currentMultiplier = isAiming ? 0.3f : 1f;
            
            Vector3 targetSway = new Vector3(
                Mathf.Clamp(-mouseX * positionSwayAmount * currentMultiplier, -positionSwayClampX, positionSwayClampX),
                Mathf.Clamp(-mouseY * positionSwayAmount * currentMultiplier, -positionSwayClampY, positionSwayClampY),
                0f
            );
            
            if (Mathf.Abs(mouseX) < 0.01f && Mathf.Abs(mouseY) < 0.01f)
            {
                targetSway = Vector3.zero;
            }
            
            swayPosition = Vector3.Lerp(swayPosition, initialPosition + targetSway, Time.deltaTime * positionSwaySmoothness);
            transform.localPosition = swayPosition;
        }
        
        // === РОТАЦИОННЫЙ SWAY (ПОВОРОТ ОРУЖИЯ) ===
        if (enableRotationSway && weaponType == WeaponType.Crossbow)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            
            float currentMultiplier = isAiming ? 0.3f : 1f;
            
            // Рассчитываем поворот
            float rotX = Mathf.Clamp(-mouseY * rotationSwayAmount * currentMultiplier, -rotationSwayClampX, rotationSwayClampX);
            float rotY = Mathf.Clamp(mouseX * rotationSwayAmount * currentMultiplier, -rotationSwayClampY, rotationSwayClampY);
            float rotZ = Mathf.Clamp(-mouseX * rotationSwayAmount * 0.3f * currentMultiplier, -rotationSwayClampZ, rotationSwayClampZ);
            
            Quaternion targetRotation = initialRotation * Quaternion.Euler(rotX, rotY, rotZ);
            
            if (Mathf.Abs(mouseX) < 0.01f && Mathf.Abs(mouseY) < 0.01f)
            {
                targetRotation = initialRotation;
            }
            
            swayRotation = Quaternion.Slerp(swayRotation, targetRotation, Time.deltaTime * rotationSwaySmoothness);
            transform.localRotation = swayRotation;
        }
        else
        {
            // Для другого оружия - возвращаем в исходное положение
            if (enableRotationSway)
            {
                swayRotation = Quaternion.Slerp(swayRotation, initialRotation, Time.deltaTime * rotationSwaySmoothness);
                transform.localRotation = swayRotation;
            }
        }
    }
    
    void HandleInertia()
    {
        if (!enableInertia || weaponType != WeaponType.Crossbow) return;
        
        // Получаем скорость движения мыши
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        
        // Добавляем инерцию к позиции
        Vector3 inertiaTarget = new Vector3(
            -mouseX * inertiaAmount * 0.01f,
            -mouseY * inertiaAmount * 0.01f,
            0f
        );
        
        inertiaVelocity = Vector3.Lerp(inertiaVelocity, inertiaTarget, Time.deltaTime * inertiaSmoothness);
        
        // Применяем инерцию к позиции (дополнительно к основному sway)
        if (enablePositionSway)
        {
            Vector3 inertiaOffset = new Vector3(inertiaVelocity.x, inertiaVelocity.y, 0f);
            Vector3 currentPos = transform.localPosition;
            transform.localPosition = currentPos + inertiaOffset * Time.deltaTime * 2f;
        }
    }
    
    void HandleAimingShake()
    {
        aimingShakeTimer += Time.deltaTime * aimingShakeSpeed;
        
        float shakeX = Mathf.Sin(aimingShakeTimer) * aimingShakeAmount;
        float shakeY = Mathf.Cos(aimingShakeTimer * 1.3f) * aimingShakeAmount;
        
        Vector3 newPos = originalCameraPos;
        newPos.x += shakeX;
        newPos.y += shakeY;
        
        playerCamera.transform.localPosition = Vector3.Lerp(
            playerCamera.transform.localPosition, 
            newPos, 
            Time.deltaTime * 10f
        );
    }
    
    void UpdateMovementAnimations()
    {
        if (animator == null) return;
        
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool isMoving = Mathf.Abs(horizontal) > 0.05f || Mathf.Abs(vertical) > 0.05f;
        
        if (weaponType == WeaponType.Crossbow)
        {
            if (isMoving && isAiming)
            {
                animator.SetFloat("MovementSpeed", 1f);
                animator.SetBool("IsWalking", true);
                animator.SetBool("IsAimingWalk", true);
            }
            else if (isMoving && !isAiming)
            {
                animator.SetFloat("MovementSpeed", 1f);
                animator.SetBool("IsWalking", true);
                animator.SetBool("IsAimingWalk", false);
            }
            else
            {
                animator.SetFloat("MovementSpeed", 0f);
                animator.SetBool("IsWalking", false);
                animator.SetBool("IsAimingWalk", false);
            }
        }
        else
        {
            if (isMoving)
            {
                animator.SetFloat("MovementSpeed", 1f);
                animator.SetBool("IsWalking", true);
            }
            else
            {
                animator.SetFloat("MovementSpeed", 0f);
                animator.SetBool("IsWalking", false);
            }
        }
    }
    
    void ShootCrossbow()
    {
        nextAttackTime = Time.time + crossbowReloadTime;
        
        if (useAmmo)
            currentBolts--;
        
        if (crossbowMuzzleFlash != null)
            StartCoroutine(ShowMuzzleFlash());
        
        if (shootSound != null)
            PlaySound(shootSound);
        
        ApplyRecoil();
        
        if (animator != null)
            animator.SetTrigger("CrossbowShoot");
        
        // Сбрасываем sway при выстреле (эффект отдачи)
        if (enablePositionSway)
        {
            StartCoroutine(ShootRecoilSway());
        }
        
        for (int i = 0; i < boltsPerShot; i++)
        {
            Vector3 direction = GetSpreadDirection();
            
            if (boltPrefab != null && shootPoint != null)
            {
                GameObject bolt = Instantiate(boltPrefab, shootPoint.position, Quaternion.LookRotation(direction));
                Rigidbody rb = bolt.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.velocity = direction * boltSpeed;
                Destroy(bolt, 5f);
            }
            else
            {
                RaycastHit hit;
                if (Physics.Raycast(playerCamera.transform.position, direction, out hit, crossbowRange))
                {
                    HandleRangedHit(hit.transform, hit.point, hit.normal);
                }
            }
        }
        
        Debug.Log($"Выстрел! Урон: {damage}, Болтов: {currentBolts}/{maxBolts}");
    }
    
    IEnumerator ShootRecoilSway()
    {
        // Эффект отдачи при выстреле (рывок вверх)
        Vector3 recoilPos = new Vector3(0, 0.01f, -0.01f);
        transform.localPosition += recoilPos;
        
        yield return new WaitForSeconds(0.05f);
        
        // Возврат
        float elapsed = 0f;
        float duration = 0.1f;
        Vector3 startPos = transform.localPosition;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localPosition = Vector3.Lerp(startPos, initialPosition, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localPosition = initialPosition;
    }
    
    IEnumerator ReloadCrossbow()
    {
        isReloading = true;
        
        if (animator != null)
            animator.SetTrigger("Reload");
        
        if (reloadSound != null)
            PlaySound(reloadSound);
        
        Debug.Log("Перезарядка...");
        
        yield return new WaitForSeconds(reloadDuration);
        
        int neededBolts = maxBolts - currentBolts;
        int boltsToReload = Mathf.Min(neededBolts, reserveBolts);
        
        currentBolts += boltsToReload;
        reserveBolts -= boltsToReload;
        
        Debug.Log($"Перезарядка завершена: {currentBolts}/{maxBolts}, в запасе: {reserveBolts}");
        
        isReloading = false;
    }
    
    void ThrowWeapon()
    {
        nextAttackTime = Time.time + thrownCooldown;
        currentThrown--;
        
        if (thrownEffect != null)
            Instantiate(thrownEffect, shootPoint != null ? shootPoint.position : transform.position, Quaternion.identity);
        
        if (thrownSound != null)
            PlaySound(thrownSound);
        
        if (animator != null)
            animator.SetTrigger("Throw");
        
        if (thrownPrefab != null && shootPoint != null)
        {
            GameObject thrown = Instantiate(thrownPrefab, shootPoint.position, Quaternion.identity);
            Rigidbody rb = thrown.GetComponent<Rigidbody>();
            
            if (rb != null)
            {
                Vector3 direction = playerCamera.transform.forward;
                Vector3 arcVelocity = direction * 25f + Vector3.up * thrownArcHeight;
                rb.velocity = arcVelocity;
            }
        }
        
        Debug.Log($"Метание! Урон: {damage}, Осталось: {currentThrown}");
    }
    
    void MeleeAttack()
    {
        float attackCooldown = (weaponType == WeaponType.Crossbow) ? crossbowReloadTime : 
                               (weaponType == WeaponType.Melee) ? meleeAttackSpeed : thrownCooldown;
        
        nextAttackTime = Time.time + attackCooldown;
        
        if (slashEffect != null)
            StartCoroutine(ShowSlashEffect());
        
        if (meleeSound != null)
            PlaySound(meleeSound);
        
        StartCoroutine(CameraShake());
        
        if (animator != null)
            animator.SetTrigger("MeleeAttack");
        
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, meleeRange, meleeLayers))
        {
            Debug.Log($"Ближняя атака попала в: {hit.transform.name}");
            
            if (hitEffect != null)
            {
                GameObject effect = Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(effect, 2f);
            }
            
            if (hitSound != null)
                PlaySound(hitSound);
            
            HandleMeleeHit(hit.transform, hit.point, hit.normal);
        }
        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, meleeRange, meleeLayers);
        foreach (var collider in hitColliders)
        {
            if (collider.transform != transform && collider.transform != transform.root)
            {
                Debug.Log($"Ближняя атака (сфера) попала в: {collider.name}");
                HandleMeleeHit(collider.transform, collider.transform.position, Vector3.up);
            }
        }
        
        if (heavyWeapon)
            StartCoroutine(SlowMovement());
    }
    
    void HandleRangedHit(Transform target, Vector3 hitPoint, Vector3 normal)
    {
        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage, hitPoint);
            return;
        }
        
        DestructibleObject destructible = target.GetComponent<DestructibleObject>();
        if (destructible != null)
        {
            destructible.TakeDamage(damage);
            return;
        }
        
        if (impactEffect != null)
        {
            GameObject impact = Instantiate(impactEffect, hitPoint, Quaternion.LookRotation(normal));
            Destroy(impact, 1f);
        }
    }
    
    void HandleMeleeHit(Transform target, Vector3 hitPoint, Vector3 normal)
    {
        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(meleeDamage, hitPoint);
            return;
        }
        
        DestructibleObject destructible = target.GetComponent<DestructibleObject>();
        if (destructible != null)
        {
            destructible.TakeDamage(meleeDamage);
            return;
        }
        
        if (impactEffect != null)
        {
            GameObject impact = Instantiate(impactEffect, hitPoint, Quaternion.LookRotation(normal));
            Destroy(impact, 1f);
        }
    }
    
    Vector3 GetSpreadDirection()
    {
        Vector3 direction = playerCamera.transform.forward;
        
        float currentSpread = isAiming ? spreadAngle * 0.5f : spreadAngle;
        
        if (currentSpread > 0)
        {
            float x = Random.Range(-currentSpread, currentSpread);
            float y = Random.Range(-currentSpread, currentSpread);
            direction = Quaternion.Euler(x, y, 0) * direction;
        }
        
        return direction;
    }
    
    void ApplyRecoil()
    {
        if (playerCamera != null)
        {
            Vector3 rotation = playerCamera.transform.localEulerAngles;
            rotation.x -= Random.Range(crossbowRecoil * 0.5f, crossbowRecoil);
            playerCamera.transform.localEulerAngles = rotation;
        }
    }
    
    public void AddBolts(int amount)
    {
        reserveBolts += amount;
        UpdateUI();
    }
    
    public void AddThrownWeapons(int amount)
    {
        thrownCount += amount;
        currentThrown += amount;
        UpdateUI();
    }
    
    void SwitchWeapon(WeaponType newType)
    {
        if (weaponType == newType) return;
        
        weaponType = newType;
        isReloading = false;
        
        if (isAiming)
        {
            isAiming = false;
            if (playerMovement != null)
                playerMovement.walkSpeed = originalWalkSpeed;
            if (animator != null)
                animator.SetBool("IsAiming", false);
            if (playerCamera != null && enableAimingShake)
            {
                playerCamera.transform.localPosition = originalCameraPos;
            }
        }
        
        // Сбрасываем позицию и поворот Sway при смене оружия
        swayPosition = initialPosition;
        swayRotation = initialRotation;
        transform.localPosition = initialPosition;
        transform.localRotation = initialRotation;
        inertiaVelocity = Vector3.zero;
        
        nextAttackTime = 0;
        
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsAimingWalk", false);
            animator.SetFloat("MovementSpeed", 0f);
        }
        
        UpdateCrosshair();
        UpdateUI();
    }
    
    void UpdateCrosshair()
    {
        if (weaponType == WeaponType.Crossbow)
        {
            if (crossbowCrosshair != null)
                crossbowCrosshair.SetActive(!isAiming);
        }
        else
        {
            if (crossbowCrosshair != null)
                crossbowCrosshair.SetActive(false);
        }
        
        if (thrownCrosshair != null)
            thrownCrosshair.SetActive(weaponType == WeaponType.Thrown);
        
        if (meleeCrosshair != null)
            meleeCrosshair.SetActive(weaponType == WeaponType.Melee);
    }
    
    void UpdateUI()
    {
        if (weaponType == WeaponType.Crossbow && ammoText != null)
        {
            ammoText.text = $"{currentBolts} / {reserveBolts}";
            
            if (ammoSlider != null)
            {
                ammoSlider.maxValue = maxBolts;
                ammoSlider.value = currentBolts;
            }
        }
        
        if (weaponType == WeaponType.Thrown && thrownText != null)
        {
            thrownText.text = $"{currentThrown}";
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        AudioSource audio = GetComponent<AudioSource>();
        if (audio == null)
            audio = gameObject.AddComponent<AudioSource>();
        audio.PlayOneShot(clip);
    }
    
    IEnumerator ShowMuzzleFlash()
    {
        if (crossbowMuzzleFlash != null)
        {
            crossbowMuzzleFlash.SetActive(true);
            yield return new WaitForSeconds(0.1f);
            crossbowMuzzleFlash.SetActive(false);
        }
    }
    
    IEnumerator ShowSlashEffect()
    {
        if (slashEffect != null)
        {
            slashEffect.SetActive(true);
            yield return new WaitForSeconds(0.2f);
            slashEffect.SetActive(false);
        }
    }
    
    IEnumerator CameraShake()
    {
        if (playerCamera == null) yield break;
        
        Vector3 originalPos = playerCamera.transform.localPosition;
        float elapsed = 0;
        
        while (elapsed < cameraShakeDuration)
        {
            float x = Random.Range(-cameraShakeAmount, cameraShakeAmount);
            float y = Random.Range(-cameraShakeAmount, cameraShakeAmount);
            playerCamera.transform.localPosition = originalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        playerCamera.transform.localPosition = originalPos;
    }
    
    IEnumerator SlowMovement()
    {
        if (playerMovement != null)
        {
            float originalSpeed = playerMovement.walkSpeed;
            playerMovement.walkSpeed *= 0.5f;
            yield return new WaitForSeconds(0.5f);
            playerMovement.walkSpeed = originalSpeed;
        }
    }
}