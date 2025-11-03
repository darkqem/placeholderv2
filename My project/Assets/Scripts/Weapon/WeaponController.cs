using System.Collections;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public GameObject weaponModel;    // Модель оружия
    public Transform shootPoint;      // Точка выстрела (например, перед стволом оружия)
    public float fireRate = 1f;       // Частота выстрелов (время между выстрелами)
    public int maxAmmo = 6;           // Максимальное количество патронов в обойме
    private int currentAmmo;          // Текущее количество патронов
    private bool canFire = true;      // Можно ли стрелять (для контроля спам-лока)
    private bool isReloading = false; // Перезарядка

    public float rayDistance = 100f;  // Дистанция, на которой луч будет искать столкновения

    // Префаб для эффекта попадания (например, вспышка или дым)
    public GameObject hitEffect;      // Префаб эффекта попадания
    public AudioClip hitSound;        // Звук попадания

    private bool isWeaponDrawn = false; // Состояние оружия (достано ли оно)
    private Inventory playerInventory; // Ссылка на инвентарь игрока

    [Header("Weapon Requirements")]
    [Tooltip("Требуется ли кольт в инвентаре для доставания оружия")]
    [SerializeField] private bool requireColtInInventory = true;

    void Start()
    {
        currentAmmo = maxAmmo;  // Инициализируем количество патронов
        weaponModel.SetActive(isWeaponDrawn);  // Устанавливаем начальное состояние оружия
        DisplayAmmo();  // Выводим начальное количество патронов в консоль
        
        // Находим инвентарь игрока
        playerInventory = GetComponent<Inventory>();
        if (playerInventory == null)
        {
            playerInventory = FindObjectOfType<Inventory>();
        }
        
        if (playerInventory == null)
        {
            Debug.LogWarning("[WeaponController] Inventory не найден! Оружие можно будет достать без проверки инвентаря.");
        }
    }

    void Update()
    {
        HandleWeaponInput();
    }

    // Обработка ввода для использования оружия
    void HandleWeaponInput()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentAmmo < maxAmmo)
        {
            StartCoroutine(Reload());  // Перезарядка оружия
        }

        if (Input.GetMouseButton(0) && canFire && currentAmmo > 0 && !isReloading && isWeaponDrawn) 
        {
            // Проверяем, что оружие достано и есть кольт в инвентаре
            if (CanUseWeapon())
            {
                FireWeapon();  // Стрельба из оружия
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) // Клавиша "1" для достания оружия
        {
            if (CanDrawWeapon())
            {
                DrawWeapon();  // Достаем оружие
            }
            else
            {
                Debug.Log("Нельзя достать оружие: кольт не найден в инвентаре.");
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha2)) // Клавиша "2" для убирания оружия
        {
            HideWeapon();  // Убираем оружие
        }
    }

    /// <summary>
    /// Проверяет, можно ли достать оружие (есть ли кольт в инвентаре)
    /// </summary>
    bool CanDrawWeapon()
    {
        // Если проверка не требуется, всегда можно достать
        if (!requireColtInInventory)
        {
            return true;
        }

        // Если инвентарь не найден, разрешаем (для обратной совместимости)
        if (playerInventory == null)
        {
            return true;
        }

        // Проверяем наличие кольта в инвентаре
        return playerInventory.HasColt();
    }

    /// <summary>
    /// Проверяет, можно ли использовать оружие (стрелять) - есть ли кольт в инвентаре
    /// </summary>
    bool CanUseWeapon()
    {
        // Если проверка не требуется, всегда можно использовать
        if (!requireColtInInventory)
        {
            return true;
        }

        // Если инвентарь не найден, разрешаем (для обратной совместимости)
        if (playerInventory == null)
        {
            return true;
        }

        // Проверяем наличие кольта в инвентаре
        return playerInventory.HasColt();
    }

    /// <summary>
    /// Безопасная проверка тега (не вызывает ошибку, если тег не определен)
    /// </summary>
    bool HasTag(GameObject obj, string tag)
    {
        try
        {
            return obj.CompareTag(tag);
        }
        catch (UnityException)
        {
            // Тег не определен, возвращаем false
            return false;
        }
    }

    // Метод для достания оружия
    void DrawWeapon()
    {
        if (!isWeaponDrawn) // Если оружие не достано
        {
            // Дополнительная проверка перед доставанием
            if (!CanDrawWeapon())
            {
                Debug.LogWarning("[WeaponController] Попытка достать оружие без кольта в инвентаре.");
                return;
            }

            isWeaponDrawn = true;
            weaponModel.SetActive(true);  // Показываем модель оружия
            Debug.Log("Оружие достано");
            
            // Триггерим событие о доставании оружия
            if (Systems.EventManager.Instance != null)
            {
                Systems.EventManager.Instance.TriggerWeaponDrawn();
            }
        }
    }

    // Метод для убирания оружия
    void HideWeapon()
    {
        if (isWeaponDrawn) // Если оружие достано
        {
            isWeaponDrawn = false;
            weaponModel.SetActive(false);  // Скрываем модель оружия
            Debug.Log("Оружие убрано");
        }
    }

    // Стрельба из оружия
    void FireWeapon()
    {
        canFire = false;       // Блокируем дальнейшие выстрелы
        currentAmmo--;         // Уменьшаем количество патронов

        // Создаем луч (Raycast) и проверяем столкновение
        RaycastHit hit;
        if (Physics.Raycast(shootPoint.position, shootPoint.forward, out hit, rayDistance))
        {
            HandleHit(hit);  // Обработка попадания в объект
        }
        else
        {
            HandleMiss();  // Обработка промаха
        }

        // Включаем задержку между выстрелами
        Invoke("ResetFire", fireRate);

        DisplayAmmo();  // Выводим количество патронов в консоль после выстрела
    }

    // Метод для обработки попадания в объект
    private void HandleHit(RaycastHit hit)
    {
        Debug.Log("Выстрел достиг поверхности: " + hit.collider.name);

        // Визуальный эффект попадания
        if (hitEffect != null)
        {
            Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));  // Эффект попадания
        }

        // Звуковой эффект попадания (если есть)
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, hit.point);  // Воспроизведение звука
        }

        // Проверяем, можно ли разрушить объект
        GameObject hitObject = hit.collider.gameObject;
        
        // Проверяем наличие компонента разрушаемого объекта или специального тега
        DestructibleObject destructible = hitObject.GetComponent<DestructibleObject>();
        bool isDestructible = destructible != null || HasTag(hitObject, "Destructible") || HasTag(hitObject, "Target");

        if (isDestructible)
        {
            // Уничтожаем объект
            if (destructible != null)
            {
                destructible.Destroy();
            }
            else
            {
                Destroy(hitObject);
            }

            // Триггерим событие о разрушении объекта
            if (Systems.EventManager.Instance != null)
            {
                Systems.EventManager.Instance.TriggerObjectDestroyedByShot(hitObject.name);
            }
        }

        // Триггерим событие о выстреле
        if (Systems.EventManager.Instance != null)
        {
            Systems.EventManager.Instance.TriggerWeaponFired();
            
            // Если это мишень, также триггерим событие попадания в мишень
            if (HasTag(hitObject, "Target"))
            {
                Systems.EventManager.Instance.TriggerTargetHit();
            }
        }

        // Логика для нанесения урона, изменения материалов и т.д.
    }

    // Метод для обработки промаха
    private void HandleMiss()
    {
        Debug.Log("Выстрел не попал в цель.");
    }

    // Метод для сброса флага canFire после задержки
    void ResetFire()
    {
        canFire = true;  // Разрешаем новый выстрел
    }

    // Перезарядка оружия
    IEnumerator Reload()
    {
        isReloading = true;
        yield return new WaitForSeconds(2f);  // Время на перезарядку (2 секунды)
        currentAmmo = maxAmmo;  // Восстанавливаем патроны
        isReloading = false;  // Заканчиваем перезарядку

        DisplayAmmo();  // Выводим обновленное количество патронов в консоль
    }

    // Метод для вывода текущего количества патронов в консоль
    void DisplayAmmo()
    {
        Debug.Log("Текущее количество патронов: " + currentAmmo + "/" + maxAmmo);
    }
}
