using UnityEngine;

public class UVFlashlight : MonoBehaviour
{
    [Header("Setari Lumina")]
    public GameObject uvLightObject; // Trage componenta de Spotlight aici
    public float lightRange = 10f;   // Distanta maxima a lanternei
    public LayerMask ghostLayer;     // Layer-ul pe care se afla fantoma

    private bool isEquipped = true;  // Seteaza pe false daca ai sistem de inventar
    private bool isOn = false;

    void Start()
    {
        // Ne asiguram ca la inceputul jocului lanterna este stinsa in cod
        isOn = false;
        
        // Dezactivam obiectul de lumina (Spotlight-ul mov)
        if (uvLightObject != null)
        {
            uvLightObject.SetActive(false);
        }
    }
    void Update()
    {
        // Verifica daca lanterna este echipata
        if (!isEquipped) return;

        // Click stanga (Fire1) pentru pornire/oprire
        if (Input.GetButtonDown("Fire1"))
        {
            ToggleFlashlight();
        }

        // Daca lanterna e pornita, cautam fantoma
        if (isOn)
        {
            DetectGhost();
        }
    }

    void ToggleFlashlight()
    {
        isOn = !isOn;
        if (uvLightObject != null)
        {
            uvLightObject.SetActive(isOn);
        }
    }

    void DetectGhost()
    {
        // Trimitem o raza din centrul ecranului (sau din pozitia lanternei) in fata
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;
        // Va desena o linie rosie in Scene View care iti arata exact unde "bate" raza ta invizibila
        Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * lightRange, Color.red);
        // Tragem raza doar pe layer-ul fantomei pentru performanta si acuratete
        if (Physics.Raycast(ray, out hit, lightRange, ghostLayer))
        {
            // Verificam daca am lovit componenta fantomei
            GhostController ghost = hit.collider.GetComponent<GhostController>();
            if (ghost != null)
            {
                ghost.RevealGhost();
            }
        }
    }

    // Functii apelate de sistemul tau de inventar cand schimbi armele/uneltele
    public void Equip() 
    { 
        isEquipped = true; 
        gameObject.SetActive(true);
    }
    
    public void Unequip() 
    { 
        isEquipped = false; 
        isOn = false;
        if (uvLightObject != null) uvLightObject.SetActive(false);
        gameObject.SetActive(false);
    }
}