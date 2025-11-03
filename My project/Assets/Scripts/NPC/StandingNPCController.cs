using UnityEngine;
using System.Collections;

public class StandingNPCController : MonoBehaviour
{
    [Header("Настройки атаки")]
    public float attackRange = 8f;
    public float attackSpeed = 4f;
    public float attackDamage = 15f;
    public float attackCooldown = 1.5f;

    [Header("Настройки зрения")]
    public float sightAngle = 120f;
    public float sightDistance = 10f;
    public LayerMask obstacleLayers = Physics.DefaultRaycastLayers;

    [Header("Ссылки")]
    public Transform playerTarget;
    public string playerTag = "Player";

    [Header("Визуальные эффекты")]
    public Color normalColor = Color.blue;
    public Color alertColor = Color.yellow;
    public Color attackColor = Color.red;

    private Rigidbody rb;
    private Renderer npcRenderer;
    private bool isAttacking = false;
    private float lastAttackTime = 0f;
    private bool canSeePlayer = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        npcRenderer = GetComponent<Renderer>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.freezeRotation = true;

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
                playerTarget = playerObj.transform;
            else
                Debug.LogWarning("Player not found! Please add a player with tag: " + playerTag);
        }

        if (npcRenderer != null)
            npcRenderer.material.color = normalColor;
    }

    void Update()
    {
        bool couldSeePlayerPreviously = canSeePlayer;
        canSeePlayer = CanSeePlayer();

        if (canSeePlayer && !isAttacking)
        {
            StartAttack();
        }
        else if (!canSeePlayer && isAttacking)
        {
            StopAttack();
        }

        if (isAttacking)
        {
            AttackBehavior();
        }
        else
        {
            IdleBehavior();
        }

        if (canSeePlayer != couldSeePlayerPreviously)
        {
            OnVisionStateChanged(canSeePlayer);
        }
    }

    bool CanSeePlayer()
    {
        if (playerTarget == null) return false;

        Vector3 directionToPlayer = playerTarget.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > sightDistance) return false;

        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer.normalized);
        if (angleToPlayer > sightAngle / 2f) return false;

        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 1f;
        if (Physics.Raycast(rayStart, directionToPlayer.normalized, out hit, sightDistance, obstacleLayers))
        {
            if (hit.transform == playerTarget || hit.transform.CompareTag(playerTag))
            {
                return true;
            }
        }

        return false;
    }

    void IdleBehavior()
    {
        transform.Rotate(0f, 30f * Time.deltaTime, 0f);

        if (Vector3.Distance(transform.position, initialPosition) > 0.1f)
        {
            transform.position = Vector3.Lerp(transform.position, initialPosition, 2f * Time.deltaTime);
        }
    }

    void AttackBehavior()
    {
        if (playerTarget == null) return;

        Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
        directionToPlayer.y = 0;

        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 6f * Time.deltaTime);
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        if (distanceToPlayer > 2f)
        {
            Vector3 moveDirection = directionToPlayer * attackSpeed * Time.deltaTime;
            transform.position += moveDirection;
        }

        if (distanceToPlayer <= 3f && Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack();
        }
    }

    void StartAttack()
    {
        isAttacking = true;
        Debug.Log("NPC обнаружил игрока! Начинает атаку.");

        if (npcRenderer != null)
            npcRenderer.material.color = alertColor;
    }

    void StopAttack()
    {
        isAttacking = false;
        Debug.Log("NPC потерял игрока. Возвращается на позицию.");

        if (npcRenderer != null)
            npcRenderer.material.color = normalColor;
    }

    void PerformAttack()
    {
        lastAttackTime = Time.time;

        if (playerTarget == null || Vector3.Distance(transform.position, playerTarget.position) > 3f)
            return;

        Debug.Log("NPC атакует игрока! Урон: " + attackDamage);

        PlayerController playerController = playerTarget.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.TakeDamage(attackDamage);
        }

        StartCoroutine(AttackEffect());
    }

    IEnumerator AttackEffect()
    {
        if (npcRenderer != null)
        {
            Color originalColor = npcRenderer.material.color;
            npcRenderer.material.color = attackColor;

            yield return new WaitForSeconds(0.3f);

            npcRenderer.material.color = isAttacking ? alertColor : normalColor;
        }
    }

    void OnVisionStateChanged(bool canSeeNow)
    {
        if (canSeeNow)
        {
            Debug.Log("Игрок обнаружен!");
        }
        else
        {
            Debug.Log("Игрок скрылся из виду");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = canSeePlayer ? Color.red : Color.yellow;

        Vector3 forward = transform.forward * sightDistance;
        Gizmos.DrawRay(transform.position + Vector3.up, forward);

        Vector3 leftBoundary = Quaternion.Euler(0, -sightAngle / 2, 0) * transform.forward * sightDistance;
        Vector3 rightBoundary = Quaternion.Euler(0, sightAngle / 2, 0) * transform.forward * sightDistance;

        Gizmos.DrawRay(transform.position + Vector3.up, leftBoundary);
        Gizmos.DrawRay(transform.position + Vector3.up, rightBoundary);

        DrawVisionArc();

        if (canSeePlayer && playerTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up, playerTarget.position + Vector3.up);
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 3f);
    }

    void DrawVisionArc()
    {
        Gizmos.color = canSeePlayer ? new Color(1, 0, 0, 0.1f) : new Color(1, 1, 0, 0.1f);

        int segments = 20;
        float angleStep = sightAngle / segments;
        Vector3 prevPoint = transform.position + Vector3.up;

        for (int i = 0; i <= segments; i++)
        {
            float angle = -sightAngle / 2 + angleStep * i;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward * sightDistance;
            Vector3 newPoint = transform.position + Vector3.up + dir;

            if (i > 0)
            {
                Gizmos.DrawLine(prevPoint, newPoint);
            }

            prevPoint = newPoint;
        }
    }
}