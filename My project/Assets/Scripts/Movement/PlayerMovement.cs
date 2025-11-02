using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Speed Settings")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float slideSpeed = 10f;
    public float gravity = -20f;
    public float jumpHeight = 1.6f;

    [Header("Movement Settings")]
    public float acceleration = 50f;        // ускорение
    public float deceleration = 60f;        // замедление
    public float airControl = 0.3f;         // контроль в воздухе

    [Header("Slide Settings")]
    public float slideDuration = 0.7f;      // время скольжения
    public float slideHeight = 1.0f;        // высота при приседе
    public float standHeight = 2.0f;        // нормальная высота
    public float slideCooldown = 1.0f;      // перерыв между слайдами

    [Header("Camera Settings")]
    public Transform cameraTransform;       // ссылка на трансформ камеры
    public float cameraStandHeight = 1.6f;  // высота камеры в стоячем положении
    public float cameraSlideHeight = 0.8f;  // высота камеры при слайде
    public float cameraTransitionSpeed = 5f; // скорость изменения высоты камеры

    [Header("Ground Check")]
    public LayerMask groundMask;
    public float groundCheckDistance = 0.2f;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 currentVelocity;        // текущая горизонтальная скорость
    private bool isGrounded;
    private bool isSliding = false;
    private float slideTimer = 0f;
    private float slideCooldownTimer = 0f;

    // Добавляем переменные для отслеживания исходной позиции
    private Vector3 originalCenter;
    private float originalHeight;
    private float targetCameraHeight;
    private float currentCameraHeight;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Сохраняем исходные параметры контроллера
        originalHeight = controller.height;
        originalCenter = controller.center;

        // Инициализируем высоту камеры
        if (cameraTransform == null)
        {
            // Автоматически находим камеру, если не назначена
            cameraTransform = GetComponentInChildren<Camera>().transform;
        }

        targetCameraHeight = cameraStandHeight;
        currentCameraHeight = cameraStandHeight;

        // Устанавливаем начальную позицию камеры
        Vector3 cameraLocalPos = cameraTransform.localPosition;
        cameraLocalPos.y = currentCameraHeight;
        cameraTransform.localPosition = cameraLocalPos;
    }

    void Update()
    {
        GroundCheck();

        // Обновляем таймер кулдауна слайда
        if (slideCooldownTimer > 0f)
            slideCooldownTimer -= Time.deltaTime;

        if (isSliding)
            SlideMove();
        else
            NormalMove();

        // Обновляем позицию камеры
        UpdateCameraHeight();
    }

    void UpdateCameraHeight()
    {
        // Плавно изменяем высоту камеры
        currentCameraHeight = Mathf.Lerp(currentCameraHeight, targetCameraHeight, cameraTransitionSpeed * Time.deltaTime);

        Vector3 cameraLocalPos = cameraTransform.localPosition;
        cameraLocalPos.y = currentCameraHeight;
        cameraTransform.localPosition = cameraLocalPos;
    }

    void GroundCheck()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }
    }

    void NormalMove()
    {
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");

        // Получаем направление движения относительно камеры
        Vector3 moveDirection = (transform.right * inputX + transform.forward * inputZ).normalized;

        bool isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float targetSpeed = isRunning ? runSpeed : walkSpeed;

        // Обрабатываем горизонтальное движение с ускорением/замедлением
        HandleHorizontalMovement(moveDirection, targetSpeed);

        // Прыжок
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Гравитация
        velocity.y += gravity * Time.deltaTime;

        // Применяем движение
        Vector3 finalMove = currentVelocity * Time.deltaTime;
        finalMove.y = velocity.y * Time.deltaTime;
        controller.Move(finalMove);

        // Попытка начать слайд
        if (isRunning && Input.GetKeyDown(KeyCode.LeftControl) && isGrounded && slideCooldownTimer <= 0f)
        {
            StartSlide(moveDirection);
        }
    }

    void HandleHorizontalMovement(Vector3 moveDirection, float targetSpeed)
    {
        // Целевая скорость
        Vector3 targetVelocity = moveDirection * targetSpeed;

        // Определяем, ускоряемся или замедляемся
        bool isAccelerating = moveDirection.magnitude > 0.1f;
        float currentAcceleration = isAccelerating ? acceleration : deceleration;

        // Множитель контроля в воздухе
        float controlMultiplier = isGrounded ? 1f : airControl;

        // Плавно изменяем скорость
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity,
                                            currentAcceleration * controlMultiplier * Time.deltaTime);

        // Если на земле и нет ввода, быстро останавливаемся
        if (isGrounded && moveDirection.magnitude < 0.1f && currentVelocity.magnitude < 0.5f)
        {
            currentVelocity = Vector3.zero;
        }
    }

    void StartSlide(Vector3 direction)
    {
        if (direction.magnitude < 0.1f) return;

        isSliding = true;
        slideTimer = slideDuration;
        slideCooldownTimer = slideCooldown;

        // Сохраняем позицию перед изменением контроллера
        Vector3 positionBeforeChange = transform.position;

        // Изменяем высоту и центр контроллера
        float heightDifference = standHeight - slideHeight;
        controller.height = slideHeight;

        // Корректируем центр так, чтобы сжатие происходило сверху вниз
        // Смещаем центр вниз на половину разницы высот
        controller.center = new Vector3(0, originalCenter.y - heightDifference / 2f, 0);

        // Восстанавливаем позицию, чтобы компенсировать смещение
        transform.position = positionBeforeChange;

        // Устанавливаем целевую высоту камеры для слайда
        targetCameraHeight = cameraSlideHeight;

        // Для слайда используем текущее направление с большей скоростью
        velocity = direction.normalized * slideSpeed;
        velocity.y = 0f;

        // Также обновляем currentVelocity для плавного перехода
        currentVelocity = velocity;
    }

    void SlideMove()
    {
        // В слайде постепенно уменьшаем горизонтальную скорость
        currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, Time.deltaTime * 2f);

        // Применяем движение
        Vector3 finalMove = currentVelocity * Time.deltaTime;
        finalMove.y = velocity.y * Time.deltaTime;
        controller.Move(finalMove);

        // Гравитация
        velocity.y += gravity * Time.deltaTime;

        slideTimer -= Time.deltaTime;
        if (slideTimer <= 0f)
        {
            EndSlide();
        }
    }

    void EndSlide()
    {
        isSliding = false;

        // Сохраняем позицию перед изменением контроллера
        Vector3 positionBeforeChange = transform.position;

        // Восстанавливаем исходные параметры контроллера
        controller.height = standHeight;
        controller.center = originalCenter;

        // Восстанавливаем позицию
        transform.position = positionBeforeChange;

        // Возвращаем камеру в нормальное положение
        targetCameraHeight = cameraStandHeight;
    }
}