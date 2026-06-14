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
    
    [Header("Aiming Settings (Прицеливание)")]
    public float aimingSpeedMultiplier = 0.5f;  // Множитель скорости при прицеливании
    public bool enableAimingShake = true;       // Включить тряску при прицеливании
    public float aimingShakeAmount = 0.02f;     // Сила тряски при прицеливании
    public float aimingShakeSpeed = 8f;         // Скорость тряски при прицеливании
    private float aimingShakeTimer = 0f;
    private Vector3 originalCameraPos;
    
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
        
        // Сохраняем оригинальную позицию камеры
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
        
        UpdateUI();
        UpdateCrosshair();
    }
    
    void Update()
    {
        UpdateMovementAnimations();
        
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
            
            // ТРЯСКА ПРИ ПРИЦЕЛИВАНИИ
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
    
    void HandleAimingShake()
    {
        // Плавная тряска камеры (эффект дыхания)
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