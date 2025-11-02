using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Transform shootPoint;  // Точка выстрела
    public float speed = 20f;     // Скорость пули
    public float lifeTime = 2f;   // Время жизни пули

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Направление пули будет по оси Z
        Vector3 direction = shootPoint.forward;

        // Применяем скорость
        rb.velocity = direction * speed;

        Destroy(gameObject, lifeTime);  // Уничтожаем пулю через 2 секунды
    }

    void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);  // Уничтожаем пулю при столкновении
    }
}
