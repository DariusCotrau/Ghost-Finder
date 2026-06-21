using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
public float interactDistance = 3f;
public GameObject interactText;

private PlayerInventory inventory;

void Start()
{
    inventory = GetComponent<PlayerInventory>();

    if (interactText != null)
        interactText.SetActive(false);
}

void Update()
{
    Ray ray = new Ray(transform.position, transform.forward);
    RaycastHit hit;

    if (Physics.Raycast(ray, out hit, interactDistance))
    {
        // Verificam mai intai daca este un pickup
        ItemPickup pickup =
            hit.collider.GetComponentInParent<ItemPickup>();

        if (pickup != null)
        {
            if (interactText != null)
                interactText.SetActive(true);

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (inventory != null &&
                    inventory.AddItem(pickup.itemType))
                {
                    Destroy(pickup.gameObject);
                }
            }

            return;
        }

        // Verificam apoi daca este o usa
        DoorController door =
            hit.collider.GetComponentInParent<DoorController>();

        if (door != null)
        {
            if (interactText != null)
                interactText.SetActive(true);

            if (Input.GetKeyDown(KeyCode.E))
            {
                door.ToggleDoor();
            }
        }
        else
        {
            if (interactText != null)
                interactText.SetActive(false);
        }
    }
    else
    {
        if (interactText != null)
            interactText.SetActive(false);
    }
}

}