using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Настройки движения")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float jumpForce = 7f;

    [Header("Настройки игрока")]
    public float health = 100f;
    public float maxHealth = 100f;

    [Header("Визуальные эффекты")]
    public Color damageColor = Color.red;
    public float damageFlashDuration = 0.3f;

    [Header("Компоненты")]
    public GameObject cameraObject;

    private Rigidbody rb;
    private Vector3 movement;
    private Camera mainCamera;
    private Renderer playerRenderer;
    private Color originalColor;
    private bool isGrounded = true;
    private bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerRenderer = GetComponent<Renderer>();
        mainCamera = Camera.main;

        if (playerRenderer != null)
            originalColor = playerRenderer.material.color;

        gameObject.tag = "Player";

        health = maxHealth;

        if (cameraObject == null && mainCamera != null)
        {
            cameraObject = mainCamera.gameObject;
        }
    }

    void Update()
    {
        if (isDead) return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        movement = (cameraForward * vertical + cameraRight * horizontal).normalized;

        if (movement != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }

        UpdateCameraPosition();
    }

    void FixedUpdate()
    {
        if (isDead) return;

        if (rb != null && movement.magnitude > 0.1f)
        {
            Vector3 moveVelocity = movement * moveSpeed;
            rb.velocity = new Vector3(moveVelocity.x, rb.velocity.y, moveVelocity.z);
        }
    }

    void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isGrounded = false;
    }

    void UpdateCameraPosition()
    {
        if (cameraObject != null)
        {
            Vector3 targetPosition = transform.position + new Vector3(0, 5f, -7f);
            cameraObject.transform.position = Vector3.Lerp(cameraObject.transform.position, targetPosition, 3f * Time.deltaTime);
            cameraObject.transform.LookAt(transform.position + Vector3.up * 1f);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        health -= damage;
        Debug.Log("Игрок получил урон: " + damage + ". Осталось здоровья: " + health);

        StartCoroutine(DamageEffect());

        if (health <= 0)
        {
            Die();
        }
    }

    IEnumerator DamageEffect()
    {
        if (playerRenderer != null)
        {
            playerRenderer.material.color = damageColor;
            yield return new WaitForSeconds(damageFlashDuration);
            playerRenderer.material.color = originalColor;
        }
    }

    public void Heal(float healAmount)
    {
        health = Mathf.Min(health + healAmount, maxHealth);
        Debug.Log("Игрок восстановил здоровье: " + healAmount + ". Текущее здоровье: " + health);
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Игрок умер!");

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (playerRenderer != null)
            playerRenderer.material.color = Color.gray;

        StartCoroutine(RespawnAfterDelay(3f));
    }

    IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Respawn();
    }

    void Respawn()
    {
        isDead = false;
        health = maxHealth;

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        if (playerRenderer != null)
            playerRenderer.material.color = originalColor;

        transform.position = new Vector3(0, 1, 0);

        Debug.Log("Игрок возродился!");
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }

    public bool IsAlive()
    {
        return !isDead;
    }

    public float GetHealth()
    {
        return health;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }
}