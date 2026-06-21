using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Interactiune jucator (E), owner-only. Raycast din camera; deschide usi prin
/// ToggleDoorServerRpc. Ridicarea itemelor ramane deocamdata locala (inventarul
/// va fi networked in Faza 4c).
/// </summary>
public class PlayerInteraction : NetworkBehaviour
{
    public float interactDistance = 3f;
    public GameObject interactText;
    public Transform aimCamera;

    private PlayerInventory inventory;

    public override void OnNetworkSpawn()
    {
        inventory = GetComponent<PlayerInventory>();
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        if (interactText != null) interactText.SetActive(false);
    }

    private void Update()
    {
        if (!IsOwner) return;

        Vector3 origin = aimCamera != null ? aimCamera.position : transform.position;
        Vector3 dir = aimCamera != null ? aimCamera.forward : transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, interactDistance))
        {
            var pickup = hit.collider.GetComponentInParent<ItemPickup>();
            if (pickup != null)
            {
                if (interactText != null) interactText.SetActive(true);
                if (Input.GetKeyDown(KeyCode.E) && inventory != null && inventory.AddItem(pickup.itemType))
                    Destroy(pickup.gameObject);
                return;
            }

            var door = hit.collider.GetComponentInParent<DoorController>();
            if (door != null)
            {
                if (interactText != null) interactText.SetActive(true);
                if (Input.GetKeyDown(KeyCode.E))
                    door.ToggleDoorServerRpc();
                return;
            }
        }

        if (interactText != null) interactText.SetActive(false);
    }
}
