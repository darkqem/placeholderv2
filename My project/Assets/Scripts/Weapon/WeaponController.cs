using System.Collections;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public GameObject weaponModel;  // Модель оружия
    public Transform shootPoint;    // Точка выстрела (например, перед стволом)
    public float fireRate = 1f;     // Частота выстрелов (время между выстрелами)
    public int maxAmmo = 6;         // Максимальное количество патронов в обойме
    private int currentAmmo;        // Текущее количество патронов
    private bool canFire = true;    // Можно ли стрелять (для контроля спам-лока)
    private bool isReloading = false; // Перезарядка

    public float rayDistance = 100f; // Дистанция, на которой луч будет искать столкновения

    void Start()
    {
        currentAmmo = maxAmmo;  // Инициализируем количество патронов
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

        if (Input.GetMouseButton(0) && canFire && currentAmmo > 0 && !isReloading) 
        {
            FireWeapon();
        }
    }

    // Стрельба из оружия
    void FireWeapon()
    {
        canFire = false;
        currentAmmo--;  // Уменьшаем количество патронов

        // Выстрел через Raycast
        RaycastHit hit;
        if (Physics.Raycast(shootPoint.position, shootPoint.forward, out hit, rayDistance))
        {
            // Выводим информацию о столкновении в консоль
            Debug.Log("Выстрел достиг поверхности: " + hit.collider.name);

            // Если луч попал, можно добавлять логику для урона или эффекта попадания
            // Например, можно создать вспышку, изменить материал или отобразить урон на объекте
        }
        else
        {
            Debug.Log("Выстрел не попал в цель.");
        }

        // Включаем задержку между выстрелами
        Invoke("ResetFire", fireRate);
    }

    // Сброс флага, чтобы снова можно было стрелять
    void ResetFire()
    {
        canFire = true;
    }

    // Перезарядка оружия
    IEnumerator Reload()
    {
        isReloading = true;
        yield return new WaitForSeconds(2f);  // Время перезарядки (2 секунды)
        currentAmmo = maxAmmo;  // Восстанавливаем патроны
        isReloading = false;
    }
}
