using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerBody;

    private float xRotation = 0f;

    void Start()
    {
        // Blochează cursorul în centrul ecranului și îl ascunde
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Preluăm mișcarea mouse-ului
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Calculăm rotația pe verticală și o limităm (clamping) la 90 de grade
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Aplicăm rotația camerei (sus-jos)
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotim corpul jucătorului pe orizontală (stânga-dreapta)
        playerBody.Rotate(Vector3.up * mouseX);
    }
}