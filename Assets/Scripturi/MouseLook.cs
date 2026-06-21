using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Camera la prima persoana, owner-authoritative. Doar owner-ul citeste mouse-ul
/// si tine camera + AudioListener active (ceilalti jucatori au camera dezactivata
/// pe instanta lor). Sensibilitatea vine din PlayerPrefs (setata in Main Menu).
/// </summary>
public class MouseLook : NetworkBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerBody;

    [Header("Activate doar pentru owner")]
    public Camera playerCamera;
    public AudioListener audioListener;

    private float xRotation = 0f;

    public override void OnNetworkSpawn()
    {
        bool owner = IsOwner;

        // Camera + audio doar pentru jucatorul local.
        if (playerCamera != null) playerCamera.enabled = owner;
        if (audioListener != null) audioListener.enabled = owner;

        if (owner)
            mouseSensitivity = PlayerPrefs.GetFloat("gf_sensitivity", 2f) * 50f;
        else
            enabled = false; // nu rula Update pe non-owner
    }

    private void Update()
    {
        if (!IsOwner) return;

        // In lobby / dupa meci: cursor liber, fara rotire (ca sa poti da click pe UI).
        var gm = GameManager.Instance;
        bool playing = gm != null && gm.MatchStarted.Value && !gm.MatchEnded.Value && !GameHUD.MenuOpen;
        if (!playing)
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            return;
        }
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        if (playerBody != null) playerBody.Rotate(Vector3.up * mouseX);
    }
}
