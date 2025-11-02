using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Speed Settings")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float slideSpeed = 10f;
    public float gravity = -20f;
    public float jumpHeight = 1.6f;

    [Header("Slide Settings")]
    public float slideDuration = 0.7f;
    public float slideHeight = 1.0f;
    public float standHeight = 2.0f;
    public float slideCooldown = 1.0f;
    public float heightSmoothSpeed = 8f;
    public float cameraSmoothSpeed = 8f;

    [Header("References")]
    public Transform playerCamera;

    [Header("Ground Check")]
    public LayerMask groundMask;
    public float groundCheckDistance = 0.2f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isSliding = false;
    private float slideTimer = 0f;
    private float slideCooldownTimer = 0f;

    private float targetHeight;
    private Vector3 targetCenter;
    private float defaultCamY;
    private float crouchCamY;
    private bool isCrouched;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera == null)
            playerCamera = Camera.main.transform;

        defaultCamY = playerCamera.localPosition.y;
        crouchCamY = defaultCamY - 0.5f;

        targetHeight = standHeight;
        targetCenter = new Vector3(0, standHeight / 2f, 0);
    }

    void Update()
    {
        GroundCheck();

        if (slideCooldownTimer > 0f)
            slideCooldownTimer -= Time.deltaTime;

        SmoothTransition();

        if (isSliding)
            SlideMove();
        else
            NormalMove();
    }

    void GroundCheck()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;
    }

    void NormalMove()
    {
        float inputX = Input.GetAxisRaw("Horizontal"); 
        float inputZ = Input.GetAxisRaw("Vertical");

        Vector3 move = (transform.right * inputX + transform.forward * inputZ).normalized;

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        controller.Move(move * currentSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (isRunning && Input.GetKeyDown(KeyCode.LeftControl) && isGrounded && slideCooldownTimer <= 0f)
            StartSlide(move);
    }

    void StartSlide(Vector3 direction)
    {
        if (direction.magnitude < 0.1f) return;

        isSliding = true;
        slideTimer = slideDuration;
        slideCooldownTimer = slideCooldown;
        isCrouched = true;

        targetHeight = slideHeight;
        targetCenter = new Vector3(0, slideHeight / 2f, 0);

        velocity = direction * slideSpeed;
        velocity.y = 0f;
    }

    void SlideMove()
    {
        controller.Move(velocity * Time.deltaTime);
        velocity = Vector3.Lerp(velocity, Vector3.zero, Time.deltaTime * 5f); 
        velocity.y += gravity * Time.deltaTime;
        controller.Move(Vector3.up * velocity.y * Time.deltaTime);

        slideTimer -= Time.deltaTime;
        if (slideTimer <= 0f || velocity.magnitude < 1f)
            EndSlide();
    }

    void EndSlide()
    {
        isSliding = false;
        isCrouched = false;
        targetHeight = standHeight;
        targetCenter = new Vector3(0, standHeight / 2f, 0);
        velocity = Vector3.zero; 
    }

    void SmoothTransition()
    {
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * heightSmoothSpeed);
        controller.center = Vector3.Lerp(controller.center, targetCenter, Time.deltaTime * heightSmoothSpeed);

        float targetY = isCrouched ? crouchCamY : defaultCamY;
        Vector3 camPos = playerCamera.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetY, Time.deltaTime * cameraSmoothSpeed);
        playerCamera.localPosition = camPos;
    }
}