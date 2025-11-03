using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Speed Settings")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float crouchSpeedMultiplier = 0.5f;
    public float gravity = -20f;
    public float jumpHeight = 1.6f;

    

    [Header("Movement Settings")]
    public float acceleration = 50f;        // Ускорение
    public float deceleration = 60f;        // Замедление
    public float airControl = 0.3f;         // Контроль в воздухе

    

    [Header("Camera Settings")]
    public Transform cameraTransform;       // Камера
    public float cameraStandHeight = 1.6f;  // Высота камеры в стоячем положении
    public float cameraCrouchHeight = 1.0f; // Высота камеры при приседе
    public float cameraTransitionSpeed = 5f; // Скорость перехода камеры

    [Header("Ground Check")]
    public LayerMask groundMask;
    public float groundCheckDistance = 0.2f;
    public float standHeight = 2.0f;        // Стандартная высота
    public float crouchHeight = 1.2f;       // Высота при приседе

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 currentVelocity;        // Текущая скорость
    private bool isGrounded;
    private bool isCrouching = false;

    private float targetCameraHeight;
    private float currentCameraHeight;
    private Vector3 originalCenter;
    private float originalHeight;

    public Animator animator;  // Ссылка на Animator

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();  // Получаем компонент Animator

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraTransform == null)
        {
            cameraTransform = GetComponentInChildren<Camera>().transform;
        }

        targetCameraHeight = cameraStandHeight;
        currentCameraHeight = cameraStandHeight;

        originalHeight = controller.height;
        originalCenter = controller.center;

        Vector3 cameraLocalPos = cameraTransform.localPosition;
        cameraLocalPos.y = currentCameraHeight;
        cameraTransform.localPosition = cameraLocalPos;
    }

    void Update()
    {
        GroundCheck();

        HandleCrouchInput();
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
        // Raycast-based ground check for more reliable detection
        // Cast from the bottom of the character controller
        Vector3 rayOrigin = transform.position + controller.center + Vector3.down * (controller.height / 2f);
        float rayDistance = groundCheckDistance + controller.skinWidth;
        
        bool raycastHit = Physics.Raycast(rayOrigin, Vector3.down, rayDistance, groundMask);
        bool controllerGrounded = controller.isGrounded;
        
        // Use raycast as primary check, controller as backup
        isGrounded = raycastHit || controllerGrounded;
        
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
            if (animator != null)
                animator.SetBool("IsJumping", false);
        }
    }

    void NormalMove()
    {
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");

        Vector3 moveDirection = (transform.right * inputX + transform.forward * inputZ).normalized;

        bool isRunning = !isCrouching && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        float targetSpeed = isRunning ? runSpeed : walkSpeed;
        if (isCrouching) targetSpeed *= crouchSpeedMultiplier;
        float moveSpeed = moveDirection.magnitude * targetSpeed;
        if (animator != null)
            animator.SetFloat("Speed", moveSpeed);

        HandleHorizontalMovement(moveDirection, targetSpeed);

        // Support both Space key and Jump button input
        bool jumpInput = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);
        
        if (jumpInput && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (animator != null)
                animator.SetBool("IsJumping", true);  // Включаем анимацию прыжка
        }

        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = currentVelocity * Time.deltaTime;
        finalMove.y = velocity.y * Time.deltaTime;
        controller.Move(finalMove);

        
    }

    void HandleCrouchInput()
    {
        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (ctrlHeld && !isCrouching && isGrounded)
        {
            StartCrouch();
        }
        else if (!ctrlHeld && isCrouching)
        {
            if (CanStandUp())
                EndCrouch();
        }
    }

    void StartCrouch()
    {
        isCrouching = true;

        Vector3 positionBeforeChange = transform.position;

        float heightDifference = originalHeight - crouchHeight;
        controller.height = crouchHeight;
        controller.center = new Vector3(controller.center.x, originalCenter.y - heightDifference / 2f, controller.center.z);

        transform.position = positionBeforeChange;
        targetCameraHeight = cameraCrouchHeight;
    }

    void EndCrouch()
    {
        isCrouching = false;

        Vector3 positionBeforeChange = transform.position;

        controller.height = originalHeight;
        controller.center = originalCenter;

        transform.position = positionBeforeChange;
        targetCameraHeight = cameraStandHeight;
    }

    bool CanStandUp()
    {
        // Check for headroom above the controller
        Vector3 bottom = transform.position + controller.center + Vector3.down * (controller.height / 2f) + Vector3.up * controller.skinWidth;
        float radius = controller.radius * 0.95f;
        float castDistance = (originalHeight - controller.height);
        if (castDistance <= 0f) return true;

        return !Physics.SphereCast(bottom, radius, Vector3.up, out _, castDistance, ~0, QueryTriggerInteraction.Ignore);
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

    

    
}
