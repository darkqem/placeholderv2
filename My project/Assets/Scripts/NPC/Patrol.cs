using UnityEngine;

public class SimplePatrol : MonoBehaviour
{
    [Header("Настройки патрулирования")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private float waitTimeAtPoint = 1f;

    [Header("Настройки поворота")]
    [SerializeField] private bool rotateTowardsMovement = true;
    [SerializeField] private float rotationSpeed = 5f;

    private Vector3 targetPosition;
    private bool isMovingToB = true;
    private bool isWaiting = false;
    private float waitTimer = 0f;

    private Rigidbody rb;
    private Vector3 direction;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.freezeRotation = true;
        }

        if (pointA != null && pointB != null)
        {
            targetPosition = pointB.position;
            transform.position = pointA.position;
        }
        else
        {
            Debug.LogError("Не назначены точки патрулирования!");
        }
    }

    void Update()
    {
        if (pointA == null || pointB == null) return;

        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtPoint)
            {
                isWaiting = false;
                waitTimer = 0f;
                SwitchTarget();
            }
            return;
        }

        direction = (targetPosition - transform.position).normalized;

        if (rotateTowardsMovement && direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        MoveNPC();

        CheckIfReachedPoint();
    }

    void MoveNPC()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, movementSpeed * Time.deltaTime);
    }

    void CheckIfReachedPoint()
    {
        Vector3 currentPos = transform.position;
        Vector3 targetPos = targetPosition;
        currentPos.y = 0;
        targetPos.y = 0;

        float distance = Vector3.Distance(currentPos, targetPos);

        if (distance < 0.1f)
        {
            if (waitTimeAtPoint > 0)
            {
                isWaiting = true;
                waitTimer = 0f;
            }
            else
            {
                SwitchTarget();
            }
        }
    }

    void SwitchTarget()
    {
        if (isMovingToB)
        {
            targetPosition = pointA.position;
            isMovingToB = false;
        }
        else
        {
            targetPosition = pointB.position;
            isMovingToB = true;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(pointA.position, 0.3f);
            Gizmos.DrawSphere(pointB.position, 0.3f);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(pointA.position, pointB.position);

            if (Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(targetPosition, 0.4f);
            }
        }
    }

    public void SetMovementSpeed(float newSpeed)
    {
        movementSpeed = newSpeed;
    }

    public void SetWaitTime(float newWaitTime)
    {
        waitTimeAtPoint = newWaitTime;
    }

    public void SetPatrolPoints(Transform newPointA, Transform newPointB)
    {
        pointA = newPointA;
        pointB = newPointB;
        transform.position = pointA.position;
        targetPosition = pointB.position;
        isMovingToB = true;
        isWaiting = false;
    }
}