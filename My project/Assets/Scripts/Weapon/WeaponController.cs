using System.Collections;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public GameObject weaponModel;  // Модель оружия
    public Transform shootPoint;    // Точка выстрела (например, перед стволом)
    public GameObject bulletPrefab; // Префаб пули
    public float fireRate = 1f;  // Частота выстрелов (время между выстрелами)
    public int maxAmmo = 6;       // Максимальное количество патронов в обойме
    private int currentAmmo;       // Текущее количество патронов
    private bool canFire = true;   // Можно ли стрелять (для контроля спам-лока)
    private bool isReloading = false; // Перезарядка

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

        // Создаем пулю в точке выстрела
        GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, shootPoint.rotation);
        Destroy(bullet, 2f);  // Пуля исчезает через 2 секунды

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
