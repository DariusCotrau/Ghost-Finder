using Unity.Netcode;
using UnityEngine;

public class PlayerInteraction : NetworkBehaviour
{
    public float interactDistance = 3f;
    public GameObject interactText; // Trage obiectul "Text (TMP)" aici în Inspector

    [Header("Camera owner-ului (pt raycast)")]
    public Camera aimCamera;

    void Start()
    {
        if (interactText != null)
            interactText.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        // Doar jucatorul local face raycast de interactiune si vede textul "E".
        if (!IsOwner)
        {
            enabled = false;
            if (interactText != null) interactText.SetActive(false);
            return;
        }

        if (aimCamera == null)
            aimCamera = GetComponentInChildren<Camera>(true);
    }

    void Update()
    {
        if (!IsOwner) return;

        Transform origin = aimCamera != null ? aimCamera.transform : transform;
        Ray ray = new Ray(origin.position, origin.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            DoorController door = hit.collider.GetComponentInParent<DoorController>();

            if (door != null)
            {
                if (interactText != null) interactText.SetActive(true);

                // Usa e networked: cerem toggle pe server, sincronizat la toti.
                if (Input.GetKeyDown(KeyCode.E))
                    door.ToggleDoorServerRpc();
            }
            else
            {
                if (interactText != null) interactText.SetActive(false);
            }
        }
        else
        {
            if (interactText != null) interactText.SetActive(false);
        }
    }
}
