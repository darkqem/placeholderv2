using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public Transform playerBody;
    public float mouseSensitivity = 100f;
    public float minPitch = -90f;
    public float maxPitch = 90f;

    private float xRotation = 0f;
    private bool mouseMovedEventTriggered = false; // Флаг для однократного срабатывания события

    void Start()
    {
        if (playerBody == null)
        {
            Debug.LogError("Player body not assigned in MouseLook.");
            enabled = false;
            return;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Check if input is blocked (e.g., during dialogue)
        if (Systems.DialogueInputBlocker.IsInputBlocked)
        {
            return; // Stop camera rotation
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Проверяем, было ли движение мыши (только один раз)
        bool mouseMoved = Mathf.Abs(mouseX) > 0.001f || Mathf.Abs(mouseY) > 0.001f;

        if (mouseMoved && !mouseMovedEventTriggered)
        {
            // Триггерим событие движения мыши (только один раз)
            if (Systems.EventManager.Instance != null)
            {
                Systems.EventManager.Instance.TriggerMouseMoved();
                mouseMovedEventTriggered = true;
            }
        }

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    /// <summary>
    /// Сброс флага события движения мыши (можно вызвать извне при необходимости)
    /// </summary>
    public void ResetMouseMovedEvent()
    {
        mouseMovedEventTriggered = false;
    }
}
