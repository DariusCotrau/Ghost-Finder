using Unity.Netcode;
using UnityEngine;

public class MouseLook : NetworkBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerBody;

    [Header("Local Player")]
    [Tooltip("Camera jucatorului (copil al prefab-ului). Activata doar pentru owner.")]
    public Camera playerCamera;
    public AudioListener audioListener;

    private float xRotation = 0f;

    public override void OnNetworkSpawn()
    {
        // Activam camera + audio + control mouse DOAR pentru jucatorul local.
        // Pe ceilalti jucatori camera lor trebuie sa fie inactiva la noi.
        bool local = IsOwner;

        if (playerCamera != null) playerCamera.enabled = local;
        if (audioListener != null) audioListener.enabled = local;

        // Daca nu e owner, oprim acest script ca sa nu miste camera altcuiva.
        enabled = local;

        if (local)
            Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (!IsOwner) return;

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
