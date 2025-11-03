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
    public float acceleration = 50f;        // Ускорение
    public float deceleration = 60f;        // Замедление
    public float airControl = 0.3f;         // Контроль в воздухе

    [Header("Slide Settings")]
    public float slideDuration = 0.7f;      // Длительность слайда
    public float slideHeight = 1.0f;        // Высота при слайде
    public float standHeight = 2.0f;        // Стандартная высота
    public float slideCooldown = 1.0f;      // Кулдаун слайда

    [Header("Camera Settings")]
    public Transform cameraTransform;       // Камера
    public float cameraStandHeight = 1.6f;  // Высота камеры в стоячем положении
    public float cameraSlideHeight = 0.8f;  // Высота камеры при слайде
    public float cameraTransitionSpeed = 5f; // Скорость перехода камеры

    [Header("Ground Check")]
    public LayerMask groundMask;
    public float groundCheckDistance = 0.2f;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 currentVelocity;        // Текущая скорость
    private bool isGrounded;
    private bool isSliding = false;
    private float slideTimer = 0f;
    private float slideCooldownTimer = 0f;

    private Vector3 originalCenter;
    private float originalHeight;
    private float targetCameraHeight;
    private float currentCameraHeight;

    public Animator animator;  // Ссылка на Animator

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();  // Получаем компонент Animator

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        originalHeight = controller.height;
        originalCenter = controller.center;

        if (cameraTransform == null)
        {
            cameraTransform = GetComponentInChildren<Camera>().transform;
        }

        targetCameraHeight = cameraStandHeight;
        currentCameraHeight = cameraStandHeight;

        Vector3 cameraLocalPos = cameraTransform.localPosition;
        cameraLocalPos.y = currentCameraHeight;
        cameraTransform.localPosition = cameraLocalPos;
    }

    void Update()
    {
        GroundCheck();

        if (slideCooldownTimer > 0f)
            slideCooldownTimer -= Time.deltaTime;

        if (isSliding)
            SlideMove();
        else
            NormalMove();

        UpdateCameraHeight();
    }

    void UpdateCameraHeight()
    {
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
            Debug.Log("хуй2");
            velocity.y = -2f;
            animator.SetBool("IsJumping", false);
        }
    }

    void NormalMove()
    {
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");

        Vector3 moveDirection = (transform.right * inputX + transform.forward * inputZ).normalized;

        bool isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        float targetSpeed = isRunning ? runSpeed : walkSpeed;
        float moveSpeed = moveDirection.magnitude * targetSpeed;
        animator.SetFloat("Speed", moveSpeed);

        HandleHorizontalMovement(moveDirection, targetSpeed);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetBool("IsJumping", true);  // Включаем анимацию прыжка
            
        }

        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = currentVelocity * Time.deltaTime;
        finalMove.y = velocity.y * Time.deltaTime;
        controller.Move(finalMove);

        if (isRunning && Input.GetKeyDown(KeyCode.LeftControl) && isGrounded && slideCooldownTimer <= 0f)
        {
            StartSlide(moveDirection);
        }
    }

    void HandleHorizontalMovement(Vector3 moveDirection, float targetSpeed)
    {
        Vector3 targetVelocity = moveDirection * targetSpeed;

        bool isAccelerating = moveDirection.magnitude > 0.1f;
        float currentAcceleration = isAccelerating ? acceleration : deceleration;

        float controlMultiplier = isGrounded ? 1f : airControl;

        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity,
                                                currentAcceleration * controlMultiplier * Time.deltaTime);

        if (isGrounded && moveDirection.magnitude < 0.1f && currentVelocity.magnitude < 0.5f)
        {
            currentVelocity = Vector3.zero;
        }
    }

    void StartSlide(Vector3 direction)
    {
        if (direction.magnitude < 0.1f) return;

        
        slideTimer = slideDuration;
        slideCooldownTimer = slideCooldown;

        Vector3 positionBeforeChange = transform.position;

        float heightDifference = standHeight - slideHeight;
        controller.height = slideHeight;

        controller.center = new Vector3(0, originalCenter.y - heightDifference / 2f, 0);

        transform.position = positionBeforeChange;

        targetCameraHeight = cameraSlideHeight;

        velocity = direction.normalized * slideSpeed;
        velocity.y = 0f;

        currentVelocity = velocity;
         // Включаем анимацию слайда
    }

    void SlideMove()
    {
        currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, Time.deltaTime * 2f);

        Vector3 finalMove = currentVelocity * Time.deltaTime;
        finalMove.y = velocity.y * Time.deltaTime;
        controller.Move(finalMove);

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

        Vector3 positionBeforeChange = transform.position;

        controller.height = standHeight;
        controller.center = originalCenter;

        transform.position = positionBeforeChange;

        targetCameraHeight = cameraStandHeight;
         // Выключаем анимацию слайда
    }

    
}
