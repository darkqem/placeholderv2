using UnityEngine;
using System.Collections;

public class PatrolAttack : MonoBehaviour
{
    [Header("Настройки патрулирования")]
    public Transform pointC;
    public Transform pointD;
    public float patrolSpeed = 2f;
    public float waitTimeAtPoint = 2f;

    [Header("Настройки атаки")]
    public float attackRange = 5f;
    public float attackSpeed = 3.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 1f;

    [Header("Настройки зрения")]
    public float sightAngle = 90f;
    public float sightDistance = 8f;
    public LayerMask obstacleLayers = Physics.DefaultRaycastLayers;

    [Header("Ссылки")]
    public Transform playerTarget;
    public string playerTag = "Player";

    private Rigidbody rb;
    private Vector3 currentTargetPosition;
    private bool isMovingToB = true;
    private bool isWaiting = false;
    private bool isAttacking = false;
    private float waitTimer = 0f;
    private float lastAttackTime = 0f;

    private bool canSeePlayer = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.freezeRotation = true;
        }

        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
                playerTarget = playerObj.transform;
        }

        if (pointC != null && pointD != null)
        {
            transform.position = pointC.position;
            currentTargetPosition = pointD.position;
        }
        else
        {
            Debug.LogError("Не назначены точки патрулирования pointC и pointD!");
        }
    }

    void Update()
    {
        canSeePlayer = CanSeePlayer();

        if (canSeePlayer && !isAttacking)
        {
            StartCoroutine(AttackPlayer());
        }
        else if (!canSeePlayer && isAttacking)
        {
            StopAllCoroutines();
            isAttacking = false;
            waitTimer = 0f;
        }

        if (isAttacking)
        {
            AttackBehavior();
        }
        else
        {
            PatrolBehavior();
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
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(rayStart, directionToPlayer.normalized, out hit, sightDistance, obstacleLayers))
        {
            if (hit.transform == playerTarget || hit.transform.CompareTag(playerTag))
            {
                return true;
            }
        }

        return false;
    }

    void PatrolBehavior()
    {
        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtPoint)
            {
                isWaiting = false;
                waitTimer = 0f;
                SwitchPatrolTarget();
            }
            return;
        }

        Vector3 direction = (currentTargetPosition - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(transform.position, currentTargetPosition, patrolSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, currentTargetPosition) < 0.2f)
        {
            isWaiting = true;
        }
    }

    void AttackBehavior()
    {
        if (playerTarget == null) return;

        Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 8f * Time.deltaTime);

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        if (distanceToPlayer > 1.5f)
        {
            transform.position = Vector3.MoveTowards(transform.position, playerTarget.position, attackSpeed * Time.deltaTime);
        }

        if (distanceToPlayer <= 2f && Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack();
        }
    }

    void PerformAttack()
    {
        lastAttackTime = Time.time;
        Debug.Log("NPC атакует игрока! Урон: " + attackDamage);

        StartCoroutine(AttackEffect());
    }

    IEnumerator AttackEffect()
    {
        Renderer renderer = GetComponent<Renderer>();
        Color originalColor = renderer.material.color;
        renderer.material.color = Color.red;

        yield return new WaitForSeconds(0.3f);

        renderer.material.color = originalColor;
    }

    IEnumerator AttackPlayer()
    {
        isAttacking = true;
        Debug.Log("NPC переходит в режим атаки!");
        yield return null;
    }

    void SwitchPatrolTarget()
    {
        if (isMovingToB)
        {
            currentTargetPosition = pointC.position;
            isMovingToB = false;
        }
        else
        {
            currentTargetPosition = pointD.position;
            isMovingToB = true;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (pointC != null && pointD != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(pointC.position, 0.3f);
            Gizmos.DrawSphere(pointD.position, 0.3f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(pointC.position, pointD.position);
        }

        Gizmos.color = canSeePlayer ? Color.red : Color.yellow;
        Vector3 leftBoundary = Quaternion.Euler(0, -sightAngle / 2, 0) * transform.forward * sightDistance;
        Vector3 rightBoundary = Quaternion.Euler(0, sightAngle / 2, 0) * transform.forward * sightDistance;

        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * sightDistance);
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, leftBoundary);
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, rightBoundary);

        if (canSeePlayer && playerTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up, playerTarget.position + Vector3.up);
        }
    }
}