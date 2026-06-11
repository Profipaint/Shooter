using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType
{
    Crossbow,     // Арбалет (дальнобойное)
    Thrown,       // Метательное (ножи, топоры, сюрикены)
    Melee         // Ближнее (меч, топор, копье)
}

public class MedievalWeapon : MonoBehaviour
{
    [Header("Weapon Type")]
    public WeaponType weaponType = WeaponType.Crossbow;
    
    [Header("Damage Settings")]
    public float damage = 35f;
    public float crossbowRange = 80f;      // Дальность арбалета
    public float thrownRange = 30f;        // Дальность метательного
    public float meleeRange = 2.5f;        // Дальность ближнего боя
    
    [Header("Fire Rate / Attack Speed")]
    public float crossbowReloadTime = 1.5f;  // Время перезарядки арбалета
    public float thrownCooldown = 0.8f;      // Задержка между метаниями
    public float meleeAttackSpeed = 0.6f;    // Скорость ближней атаки
    private float nextAttackTime = 0f;
    
    [Header("Crossbow Settings (Арбалет)")]
    public GameObject boltPrefab;            // Префаб болта (снаряда)
    public Transform shootPoint;             // Точка вылета болта
    public GameObject crossbowMuzzleFlash;   // Эффект вспышки/дыма при выстреле
    public float boltSpeed = 50f;            // Скорость полета болта
    public float crossbowRecoil = 1f;        // Отдача камеры
    public int boltsPerShot = 1;             // Сколько болтов за раз (для дробового арбалета)
    public float spreadAngle = 2f;           // Разброс (не точность)
    
    [Header("Thrown Weapons (Метательное)")]
    public GameObject thrownPrefab;           // Префаб метательного ножа/топора
    public int thrownCount = 5;              // Количество метательного оружия
    private int currentThrown;               // Текущее количество
    public float thrownArcHeight = 2f;       // Высота дуги полета
    public GameObject thrownEffect;          // Эффект при метании
    
    [Header("Melee Settings (Ближнее)")]
    public GameObject slashEffect;            // Эффект взмаха
    public float cameraShakeAmount = 0.08f;   // Тряска камеры при ударе
    public float cameraShakeDuration = 0.1f;
    public LayerMask meleeLayers;
    public bool heavyWeapon = false;          // Тяжелое оружие (замедляет игрока?)
    
    [Header("Effects")]
    public GameObject hitEffect;              // Эффект попадания (кровь, искры)
    public GameObject impactEffect;           // Эффект попадания в стену
    public AudioClip shootSound;              // Звук выстрела арбалета
    public AudioClip thrownSound;             // Звук метания
    public AudioClip meleeSound;              // Звук удара
    public AudioClip hitSound;                // Звук попадания
    public AudioClip reloadSound;             // Звук перезарядки арбалета
    
    [Header("Ammo System (Только для арбалета)")]
    public bool useAmmo = true;
    public int maxBolts = 1;                  // У арбалета обычно 1 болт
    public int currentBolts;
    public int reserveBolts = 30;             // Запас болтов
    public float reloadDuration = 1.5f;       // Длительность перезарядки
    
    [Header("UI")]
    public UnityEngine.UI.Text ammoText;      // Текст с боеприпасами
    public UnityEngine.UI.Slider ammoSlider;
    public UnityEngine.UI.Text thrownText;    // Текст для метательного оружия
    
    [Header("Camera")]
    public Camera playerCamera;
    
    [Header("Animation")]
    public Animator animator;
    public string attackTrigger = "Attack";
    public string reloadTrigger = "Reload";
    public string thrownTrigger = "Throw";
    
    [Header("Crosshair")]
    public GameObject crossbowCrosshair;      // Прицел арбалета
    public GameObject thrownCrosshair;        // Прицел метательного
    public GameObject meleeCrosshair;         // Прицел ближнего боя
    
    private bool isReloading = false;
    private bool isAiming = false;
    
    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
            
        // Инициализация боеприпасов
        if (useAmmo && weaponType == WeaponType.Crossbow)
        {
            currentBolts = maxBolts;
        }
        
        if (weaponType == WeaponType.Thrown)
        {
            currentThrown = thrownCount;
        }
        
        UpdateUI();
        UpdateCrosshair();
    }
    
    void Update()
    {
        // Переключение оружия
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SwitchWeapon(WeaponType.Crossbow);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SwitchWeapon(WeaponType.Melee);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            SwitchWeapon(WeaponType.Thrown);
            
        // Прицеливание для арбалета (правый клик)
        if (weaponType == WeaponType.Crossbow)
        {
            HandleAiming();
        }
        
        // Атака
        switch (weaponType)
        {
            case WeaponType.Crossbow:
                if (Input.GetButtonDown("Fire1") && Time.time >= nextAttackTime && !isReloading)
                    ShootCrossbow();
                if (Input.GetKeyDown(KeyCode.R) && useAmmo && !isReloading)
                    StartCoroutine(ReloadCrossbow());
                break;
                
            case WeaponType.Thrown:
                if (Input.GetButtonDown("Fire1") && Time.time >= nextAttackTime && currentThrown > 0)
                    ThrowWeapon();
                break;
                
            case WeaponType.Melee:
                if (Input.GetButtonDown("Fire1") && Time.time >= nextAttackTime)
                    MeleeAttack();
                break;
        }
        
        UpdateUI();
    }
    
    void HandleAiming()
    {
        if (Input.GetMouseButton(1))
        {
            isAiming = true;
            // Уменьшаем разброс при прицеливании
            spreadAngle = 0.5f;
            
            // Приближение камеры (опционально)
            if (playerCamera != null && playerCamera.fieldOfView > 40)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, 45, Time.deltaTime * 10f);
            }
        }
        else
        {
            isAiming = false;
            spreadAngle = 2f;
            
            // Возврат камеры
            if (playerCamera != null && playerCamera.fieldOfView < 60)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, 60, Time.deltaTime * 10f);
            }
        }
    }
    
    void ShootCrossbow()
    {
        // Проверка болтов
        if (useAmmo && currentBolts <= 0)
        {
            Debug.Log("Нет болтов! Нажми R для перезарядки");
            return;
        }
        
        nextAttackTime = Time.time + crossbowReloadTime;
        
        if (useAmmo)
            currentBolts--;
            
        // Эффекты выстрела
        if (crossbowMuzzleFlash != null)
            StartCoroutine(ShowMuzzleFlash());
            
        if (shootSound != null)
            PlaySound(shootSound);
            
        ApplyRecoil();
        
        // Выстрел несколькими болтами
        for (int i = 0; i < boltsPerShot; i++)
        {
            Vector3 direction = GetSpreadDirection();
            
            if (boltPrefab != null && shootPoint != null)
            {
                // Физический снаряд
                GameObject bolt = Instantiate(boltPrefab, shootPoint.position, Quaternion.LookRotation(direction));
                Rigidbody rb = bolt.GetComponent<Rigidbody>();
                
                if (rb != null)
                {
                    rb.velocity = direction * boltSpeed;
                }
                
                Destroy(bolt, 5f); // Автоудаление через 5 сек
            }
            else
            {
                // Raycast версия (проще)
                RaycastHit hit;
                if (Physics.Raycast(playerCamera.transform.position, direction, out hit, crossbowRange))
                {
                    HandleHit(hit.transform, hit.point, hit.normal);
                }
            }
        }
        
        // Анимация выстрела
        if (animator != null)
            animator.SetTrigger(attackTrigger);
            
        // Автоматическая перезарядка если болтов больше нет
        if (useAmmo && currentBolts == 0 && reserveBolts > 0)
        {
            StartCoroutine(ReloadCrossbow());
        }
    }
    
    void ThrowWeapon()
    {
        nextAttackTime = Time.time + thrownCooldown;
        currentThrown--;
        
        // Эффект метания
        if (thrownEffect != null)
            Instantiate(thrownEffect, shootPoint.position, Quaternion.identity);
            
        if (thrownSound != null)
            PlaySound(thrownSound);
            
        // Создание метательного снаряда
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
        
        // Анимация
        if (animator != null)
            animator.SetTrigger(thrownTrigger);
            
        // Анимация руки
        StartCoroutine(ThrowAnimation());
    }
    
    void MeleeAttack()
    {
        nextAttackTime = Time.time + meleeAttackSpeed;
        
        // Эффекты
        if (slashEffect != null)
            StartCoroutine(ShowSlashEffect());
            
        if (meleeSound != null)
            PlaySound(meleeSound);
            
        StartCoroutine(CameraShake());
        
        // Анимация
        if (animator != null)
            animator.SetTrigger(attackTrigger);
            
        // Попадание (луч)
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, meleeRange, meleeLayers))
        {
            HandleHit(hit.transform, hit.point, hit.normal);
            
            if (hitSound != null)
                PlaySound(hitSound);
        }
        
        // Дополнительная проверка сферой для Area of Effect
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, meleeRange, meleeLayers);
        foreach (var collider in hitColliders)
        {
            if (collider.transform != transform && collider.transform != transform.root)
            {
                HandleHit(collider.transform, collider.transform.position, Vector3.up);
            }
        }
        
        // Замедление игрока для тяжелого оружия
        if (heavyWeapon)
        {
            StartCoroutine(SlowMovement());
        }
    }
    
    void HandleHit(Transform target, Vector3 hitPoint, Vector3 normal)
    {
        // Эффект попадания
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, hitPoint, Quaternion.LookRotation(normal));
            Destroy(effect, 2f);
        }
        
        // Нанесение урона
        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage, hitPoint);
            Debug.Log($"Попадание! Урон: {damage}");
        }
        else if (target.GetComponent<DestructibleObject>() != null)
        {
            target.GetComponent<DestructibleObject>().TakeDamage(damage);
        }
        else
        {
            // Эффект попадания в стену
            if (impactEffect != null)
            {
                GameObject impact = Instantiate(impactEffect, hitPoint, Quaternion.LookRotation(normal));
                Destroy(impact, 1f);
            }
        }
    }
    
    IEnumerator ReloadCrossbow()
    {
        isReloading = true;
        
        if (reloadSound != null)
            PlaySound(reloadSound);
            
        if (animator != null)
            animator.SetTrigger(reloadTrigger);
            
        yield return new WaitForSeconds(reloadDuration);
        
        // Перезарядка
        int neededBolts = maxBolts - currentBolts;
        int boltsToReload = Mathf.Min(neededBolts, reserveBolts);
        
        currentBolts += boltsToReload;
        reserveBolts -= boltsToReload;
        
        Debug.Log($"Перезарядка: {currentBolts}/{maxBolts}, болтов в запасе: {reserveBolts}");
        
        isReloading = false;
    }
    
    Vector3 GetSpreadDirection()
    {
        Vector3 direction = playerCamera.transform.forward;
        
        if (spreadAngle > 0 && !isAiming)
        {
            float x = Random.Range(-spreadAngle, spreadAngle);
            float y = Random.Range(-spreadAngle, spreadAngle);
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
        Debug.Log($"Найдено {amount} болтов. Всего: {reserveBolts}");
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
        string weaponName = "";
        
        switch (weaponType)
        {
            case WeaponType.Crossbow:
                weaponName = "Арбалет";
                break;
            case WeaponType.Thrown:
                weaponName = "Метательное оружие";
                break;
            case WeaponType.Melee:
                weaponName = "Ближнее оружие";
                break;
        }
        
        Debug.Log($"Переключено на: {weaponName}");
        UpdateCrosshair();
        UpdateUI();
    }
    
    void UpdateCrosshair()
    {
        if (crossbowCrosshair != null)
            crossbowCrosshair.SetActive(weaponType == WeaponType.Crossbow);
            
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
        crossbowMuzzleFlash.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        crossbowMuzzleFlash.SetActive(false);
    }
    
    IEnumerator ShowSlashEffect()
    {
        slashEffect.SetActive(true);
        yield return new WaitForSeconds(0.2f);
        slashEffect.SetActive(false);
    }
    
    IEnumerator ThrowAnimation()
    {
        // Анимация руки (можно через аниматор)
        yield return new WaitForSeconds(0.3f);
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
        FirstPersonMovement movement = GetComponent<FirstPersonMovement>();
        if (movement != null)
        {
            float originalSpeed = movement.walkSpeed;
            movement.walkSpeed *= 0.5f;
            yield return new WaitForSeconds(0.5f);
            movement.walkSpeed = originalSpeed;
        }
    }
}