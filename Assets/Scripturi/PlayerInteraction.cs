using UnityEngine;
using TMPro; // Necesar pentru TextMeshPro

public class PlayerInteraction : MonoBehaviour
{
    public float interactDistance = 3f;
    public GameObject interactText; // Trage obiectul "Text (TMP)" aici în Inspector

    void Start()
    {
        // Ne asigurăm că textul este ascuns la începutul jocului
        if (interactText != null)
            interactText.SetActive(false);
    }

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // Verificăm dacă raza lovește ceva
        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            // Căutăm scriptul de ușă pe obiectul lovit sau pe părinții lui
            DoorController door = hit.collider.GetComponentInParent<DoorController>();

            if (door != null)
            {
                // Dacă vedem o ușă, afișăm textul "E"
                if (interactText != null) interactText.SetActive(true);

                if (Input.GetKeyDown(KeyCode.E))
                {
                    door.ToggleDoor();
                }
            }
            else
            {
                // Dacă lovim altceva care nu e ușă, ascundem textul
                if (interactText != null) interactText.SetActive(false);
            }
        }
        else
        {
            // Dacă nu lovim nimic, ascundem textul
            if (interactText != null) interactText.SetActive(false);
        }
    }
}