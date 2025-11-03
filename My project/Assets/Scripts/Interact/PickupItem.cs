using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public float interactionRange = 3f;
    public KeyCode interactKey = KeyCode.E;
    public float dragDistance = 2f; // Дистанция перед игроком для перетаскиваемого предмета

    private Inventory playerInventory;
    private Camera playerCamera;

    private Item currentTargetItem;
    private Item currentlyDraggedItem; // Текущий перетаскиваемый предмет
    private bool isHolding = false;
    private float holdTimer = 0f;
    private Vector3 originalItemPosition;
    private Quaternion originalItemRotation;
    private Transform originalItemParent;

    void Start()
    {
        playerCamera = Camera.main;
        playerInventory = FindObjectOfType<Inventory>();
    }

    void Update()
    {
        HandleRaycast();
        HandleHoldInteraction();
        HandleDragRelease();
    }

    void HandleRaycast()
    {
        // Если уже перетаскиваем предмет, не проверяем новые цели
        if (currentlyDraggedItem != null) return;

        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, interactionRange))
        {
            Item item = hit.collider.GetComponent<Item>();

            if (item != null && item != currentTargetItem)
            {
                currentTargetItem = item;
                Debug.Log($"Обнаружен предмет: {item.itemName} (Тип: {item.interactionType})");

                if (item.interactionType == InteractionType.Pickup)
                {
                    Debug.Log("Нажмите E чтобы подобрать: " + item.itemName);
                }
                else
                {
                    Debug.Log($"Удерживайте E ({item.holdDuration}сек) для: {item.itemName}");
                }
            }
        }
        else
        {
            if (currentTargetItem != null)
            {
                ResetHoldInteraction();
                currentTargetItem = null;
            }
        }
    }

    void HandleHoldInteraction()
    {
        if (currentTargetItem == null) return;

        // Начало удержания
        if (Input.GetKeyDown(interactKey) && !isHolding)
        {
            if (currentTargetItem.interactionType == InteractionType.Pickup)
            {
                // Мгновенное взаимодействие для подбираемых предметов
                PickupCurrentItem();
            }
            else
            {
                // Начало удержания для дверей и перетаскиваемых предметов
                StartHold();
            }
        }

        // Отслеживание удержания
        if (isHolding && Input.GetKey(interactKey))
        {
            holdTimer += Time.deltaTime;

            if (holdTimer >= currentTargetItem.holdDuration)
            {
                CompleteHoldInteraction();
            }
        }

        // Прерывание удержания
        if (Input.GetKeyUp(interactKey) && isHolding && currentlyDraggedItem == null)
        {
            ResetHoldInteraction();
        }
    }

    void HandleDragRelease()
    {
        // Если перетаскиваем предмет и отпустили E - бросаем его
        if (currentlyDraggedItem != null && Input.GetKeyUp(interactKey))
        {
            ReleaseDraggedItem();
        }

        // Обновление позиции перетаскиваемого предмета
        if (currentlyDraggedItem != null)
        {
            UpdateDraggedItemPosition();
        }
    }

    void StartHold()
    {
        isHolding = true;
        holdTimer = 0f;
        Debug.Log($"Начато удержание E для: {currentTargetItem.itemName}");
    }

    void CompleteHoldInteraction()
    {
        Debug.Log($"Удержание завершено для: {currentTargetItem.itemName}");

        switch (currentTargetItem.interactionType)
        {
            case InteractionType.Door:
                OpenDoor();
                break;
            case InteractionType.Draggable:
                StartDraggingItem();
                break;
        }

        ResetHoldInteraction();
    }

    void ResetHoldInteraction()
    {
        isHolding = false;
        holdTimer = 0f;

        if (currentTargetItem != null && currentTargetItem.interactionType != InteractionType.Pickup && currentlyDraggedItem == null)
        {
            Debug.Log("Удержание прервано");
        }
    }

    void PickupCurrentItem()
    {
        playerInventory.AddItem(currentTargetItem);
        Destroy(currentTargetItem.gameObject);
        currentTargetItem = null;
    }

    void OpenDoor()
    {
        Debug.Log($"Дверь '{currentTargetItem.itemName}' открыта!");
        // Здесь можно добавить анимацию открытия двери
        Destroy(currentTargetItem.gameObject); // или деактивировать дверь
        currentTargetItem = null;
    }

    void StartDraggingItem()
    {
        currentlyDraggedItem = currentTargetItem;
        currentTargetItem = null;

        // Сохраняем оригинальные параметры предмета
        originalItemPosition = currentlyDraggedItem.transform.position;
        originalItemRotation = currentlyDraggedItem.transform.rotation;
        originalItemParent = currentlyDraggedItem.transform.parent;

        // Делаем предмет дочерним объектом камеры
        currentlyDraggedItem.transform.SetParent(playerCamera.transform);

        // Настраиваем физику
        Rigidbody rb = currentlyDraggedItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Отключаем коллайдер временно, чтобы не мешал лучу
        Collider collider = currentlyDraggedItem.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        Debug.Log($"Начато перетаскивание: {currentlyDraggedItem.itemName}. Отпустите E чтобы бросить.");
    }

    void UpdateDraggedItemPosition()
    {
        if (currentlyDraggedItem == null) return;

        // Позиция предмета перед игроком
        Vector3 targetPosition = playerCamera.transform.position + playerCamera.transform.forward * dragDistance;
        currentlyDraggedItem.transform.position = targetPosition;

    }

    void ReleaseDraggedItem()
    {
        if (currentlyDraggedItem == null) return;

        Debug.Log($"Предмет {currentlyDraggedItem.itemName} брошен.");

        // Возвращаем оригинальный parent или оставляем в мире
        currentlyDraggedItem.transform.SetParent(originalItemParent);

        // Восстанавливаем физику
        Rigidbody rb = currentlyDraggedItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            // Добавляем небольшой импульс вперед при броске
            rb.AddForce(playerCamera.transform.forward * 2f, ForceMode.Impulse);
        }

        // Восстанавливаем коллайдер
        Collider collider = currentlyDraggedItem.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = true;
        }

        currentlyDraggedItem = null;
    }

    // Визуализация луча и дистанции перетаскивания в редакторе
    void OnDrawGizmos()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.blue;
            Vector3 dragPosition = playerCamera.transform.position + playerCamera.transform.forward * dragDistance;
            Gizmos.DrawWireSphere(dragPosition, 0.1f);
            Gizmos.DrawLine(playerCamera.transform.position, dragPosition);
        }
    }
}