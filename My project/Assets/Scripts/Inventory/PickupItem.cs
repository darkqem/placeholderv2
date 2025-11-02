using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public float interactionRange = 3f; // Дистанция, на которой игрок может взаимодействовать с предметом.
    public KeyCode interactKey = KeyCode.E; // Кнопка для взаимодействия (E)
    private Inventory playerInventory;
    private Camera playerCamera;

    void Start()
    {
        playerCamera = Camera.main; // Получаем камеру игрока
        playerInventory = FindObjectOfType<Inventory>(); // Находим объект инвентаря
    }

    void Update()
    {
        // Проверяем, находится ли предмет в зоне видимости и в радиусе взаимодействия
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, interactionRange))
        {
            Item item = hit.collider.GetComponent<Item>();
            if (item != null)
            {
                // Отображаем, что предмет можно поднять
                if (Input.GetKeyDown(interactKey))
                {
                    playerInventory.AddItem(item); // Добавляем предмет в инвентарь
                    Destroy(item.gameObject); // Удаляем предмет из мира
                }
            }
        }
    }
}
