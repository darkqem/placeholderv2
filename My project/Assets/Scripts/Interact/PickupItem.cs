using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public float interactionRange = 3f;
    public KeyCode interactKey = KeyCode.E;
    public float dragDistance = 2f; // Дистанция перед игроком для перетаскиваемого предмета
    public LayerMask interactionLayerMask = -1; // По умолчанию все слои

    private Inventory playerInventory;
    private Camera playerCamera;

    private Item currentTargetItem;
    private Item currentlyDraggedItem; // Текущий перетаскиваемый предмет
    private bool isHolding = false;
    private float holdTimer = 0f;
    private Vector3 originalItemPosition;
    private Transform originalItemParent;

    void Start()
    {
        playerCamera = Camera.main;
        playerInventory = FindObjectOfType<Inventory>();
    }

    void Update()
    {
        // Check if input is blocked (e.g., during dialogue)
        if (Systems.DialogueInputBlocker.IsInputBlocked)
        {
            // Сбрасываем цель, если ввод заблокирован
            if (currentTargetItem != null)
            {
                currentTargetItem = null;
                ResetHoldInteraction();
            }
            return; // Don't process interaction when input is blocked
        }

        HandleRaycast();
        HandleHoldInteraction();
        HandleDragRelease();
    }

    void HandleRaycast()
    {
        // Если уже перетаскиваем предмет, не проверяем новые цели
        if (currentlyDraggedItem != null) return;

        // Проверка наличия камеры
        if (playerCamera == null)
        {
            return;
        }

        Vector3 rayOrigin = playerCamera.transform.position;
        Vector3 rayDirection = playerCamera.transform.forward;

        // Сначала пытаемся найти предмет через raycast
        RaycastHit hit;
        bool hitSomething = Physics.Raycast(rayOrigin, rayDirection, out hit, interactionRange, interactionLayerMask);
        
        Item foundItem = null;
        
        if (hitSomething)
        {
            // Ищем Item компонент на объекте или в родительских объектах
            foundItem = hit.collider.GetComponent<Item>();
            if (foundItem == null)
            {
                foundItem = hit.collider.GetComponentInParent<Item>();
            }
        }
        
        // Если raycast не нашел предмет, пробуем найти через OverlapSphere и прямой поиск всех Item компонентов
        if (foundItem == null)
        {
            // Метод 1: Поиск через OverlapSphere (по коллайдерам)
            Collider[] nearbyColliders = Physics.OverlapSphere(rayOrigin, interactionRange, interactionLayerMask);
            
            Item closestItem = null;
            float closestDistance = float.MaxValue;
            
            // Сначала проверяем найденные коллайдеры
            foreach (Collider col in nearbyColliders)
            {
                // Пропускаем коллайдеры игрока
                bool isPlayerCollider = (col.transform == transform || col.transform.IsChildOf(transform));
                if (isPlayerCollider)
                {
                    continue;
                }
                
                // Ищем Item на объекте, в родительских и дочерних объектах
                Item item = col.GetComponent<Item>();
                if (item == null)
                {
                    item = col.GetComponentInParent<Item>();
                }
                if (item == null)
                {
                    item = col.GetComponentInChildren<Item>();
                }
                
                if (item != null)
                {
                    Vector3 itemPosition = item.transform.position;
                    Vector3 directionToItem = (itemPosition - rayOrigin).normalized;
                    float distance = Vector3.Distance(rayOrigin, itemPosition);
                    float angle = Vector3.Angle(rayDirection, directionToItem);
                    
                    // Выбираем предмет, который ближе всего к направлению взгляда и находится в пределах угла
                    if (angle < 90f && distance < closestDistance)
                    {
                        closestItem = item;
                        closestDistance = distance;
                    }
                }
            }
            
            // Метод 2: Прямой поиск всех Item компонентов в сцене (если первый метод не сработал)
            if (closestItem == null)
            {
                Item[] allItems = FindObjectsOfType<Item>();
                
                foreach (Item item in allItems)
                {
                    // Пропускаем предметы игрока
                    bool isPlayerItem = (item.transform == transform || item.transform.IsChildOf(transform));
                    if (isPlayerItem)
                    {
                        continue;
                    }
                    
                    Vector3 itemPosition = item.transform.position;
                    float distance = Vector3.Distance(rayOrigin, itemPosition);
                    Vector3 directionToItem = (itemPosition - rayOrigin).normalized;
                    float angle = Vector3.Angle(rayDirection, directionToItem);
                    
                    // Проверяем только угол обзора - если предмет виден, выбираем его независимо от расстояния
                    if (angle >= 90f)
                    {
                        continue;
                    }
                    
                    // Выбираем ближайший видимый предмет
                    // Приоритет предметам в пределах interactionRange, но если таких нет - выбираем ближайший видимый
                    bool isInRange = distance <= interactionRange;
                    bool isBetterChoice = false;
                    
                    if (closestItem == null)
                    {
                        isBetterChoice = true;
                    }
                    else if (isInRange && closestDistance > interactionRange)
                    {
                        // Предмет в радиусе, а текущий выбранный - нет
                        isBetterChoice = true;
                    }
                    else if (distance < closestDistance)
                    {
                        // Ближе, чем текущий выбранный
                        isBetterChoice = true;
                    }
                    
                    if (isBetterChoice)
                    {
                        closestItem = item;
                        closestDistance = distance;
                    }
                }
            }
            
            if (closestItem != null)
            {
                foundItem = closestItem;
            }
        }
        
        // Обновляем текущий предмет
        if (foundItem != null)
        {
            // Если это новый предмет, обновляем цель
            if (foundItem != currentTargetItem)
            {
                currentTargetItem = foundItem;
            }
        }
        else
        {
            // Не нашли предмет - сбрасываем цель только если уже не держим
            if (currentTargetItem != null && !isHolding)
            {
                ResetHoldInteraction();
                currentTargetItem = null;
            }
        }
    }

    void HandleHoldInteraction()
    {
        if (currentTargetItem == null)
        {
            return;
        }

        // Начало удержания
        if (Input.GetKeyDown(interactKey) && !isHolding)
        {
            // Проверяем расстояние до предмета перед взаимодействием
            if (playerCamera != null)
            {
                float distanceToItem = Vector3.Distance(playerCamera.transform.position, currentTargetItem.transform.position);
                float maxInteractionDistance = currentTargetItem.interactionType == InteractionType.Pickup 
                    ? 10f  // Для предметов Pickup разрешаем взаимодействие до 10 метров
                    : interactionRange * 1.5f; // Для дверей и Draggable - 1.5x от радиуса
                
                if (distanceToItem > maxInteractionDistance)
                {
                    return;
                }
            }
            
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
    }

    void CompleteHoldInteraction()
    {
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
    }

    void PickupCurrentItem()
    {
        if (currentTargetItem == null || playerInventory == null)
        {
            return;
        }
        
        string itemName = currentTargetItem.itemName;
        
        // Добавляем в инвентарь
        playerInventory.AddItem(currentTargetItem);
        
        // Триггерим событие о подборе предмета
        if (Systems.EventManager.Instance != null)
        {
            Systems.EventManager.Instance.TriggerObjectPickedUp(itemName);
        }
        
        // Уничтожаем предмет
        GameObject itemObject = currentTargetItem.gameObject;
        currentTargetItem = null; // Очищаем ссылку перед уничтожением
        Destroy(itemObject);
    }

    void OpenDoor()
    {
        // Триггерим событие об открытии двери
        if (Systems.EventManager.Instance != null)
        {
            Systems.EventManager.Instance.TriggerDoorOpened();
        }
        
        // Здесь можно добавить анимацию открытия двери
        Destroy(currentTargetItem.gameObject); // или деактивировать дверь
        currentTargetItem = null;
    }

    void StartDraggingItem()
    {
        if (playerCamera == null)
        {
            return;
        }
        
        currentlyDraggedItem = currentTargetItem;
        currentTargetItem = null;

        // Сохраняем оригинальные параметры предмета
        originalItemPosition = currentlyDraggedItem.transform.position;
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
    }

    void UpdateDraggedItemPosition()
    {
        if (currentlyDraggedItem == null || playerCamera == null) return;

        // Позиция предмета перед игроком
        Vector3 targetPosition = playerCamera.transform.position + playerCamera.transform.forward * dragDistance;
        currentlyDraggedItem.transform.position = targetPosition;

    }

    void ReleaseDraggedItem()
    {
        if (currentlyDraggedItem == null || playerCamera == null) return;

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