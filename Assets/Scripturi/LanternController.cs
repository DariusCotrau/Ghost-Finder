using UnityEngine;

public class LanternController : MonoBehaviour
{
    public Light lanternLight; // Referință către componenta de lumină
    public bool isOn = true;   // Starea inițială

    void Start()
    {
        // Ne asigurăm că avem componenta, dacă nu e setată în Inspector
        if (lanternLight == null)
        {
            lanternLight = GetComponent<Light>();
        }

        // Setăm starea inițială
        if (lanternLight != null)
        {
            lanternLight.enabled = isOn;
        }
    }

    void Update()
    {
        // Detectăm apăsarea tastei F
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleLantern();
        }
    }

    void ToggleLantern()
    {
        isOn = !isOn; // Schimbăm starea

        if (lanternLight != null)
        {
            lanternLight.enabled = isOn; // Activăm/Dezactivăm lumina
        }
    }
}