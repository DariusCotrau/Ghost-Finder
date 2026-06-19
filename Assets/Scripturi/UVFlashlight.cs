using UnityEngine;
using UnityEngine.UI; // IMPORTANT: Adauga aceasta linie pentru a putea controla UI-ul!

public class UVFlashlight : MonoBehaviour
{
    [Header("Setari Lumina")]
    public GameObject uvLightObject; 
    public float lightRange = 10f;   
    public LayerMask ghostLayer;     

    [Header("Setari Baterie (Nerf)")]
    public float maxBatteryDuration = 5f;  
    public float rechargeDuration = 25f;  
    
    [Header("Setari UI")]
    public Slider batterySlider; // Trage Slider-ul din scena aici

    private float currentBattery;
    private bool isRecharging = false;
    private bool isEquipped = true;  
    private bool isOn = false;

    void Start()
    {
        currentBattery = maxBatteryDuration;

        // Configuram slider-ul sa aiba valorile corecte automat
        if (batterySlider != null)
        {
            batterySlider.maxValue = maxBatteryDuration;
            batterySlider.value = currentBattery;
        }

        isOn = false;
        if (uvLightObject != null)
        {
            uvLightObject.SetActive(false);
        }
    }

void Update()
    {
        if (!isEquipped) return;

        if (Input.GetButtonDown("Fire1") && !isRecharging)
        {
            ToggleFlashlight();
        }

        if (isOn)
        {
            currentBattery -= Time.deltaTime;

            if (currentBattery <= 0)
            {
                currentBattery = 0;
                ForceTurnOff(); 
            }
            else
            {
                DetectGhost();
            }
        }
        else
        {
            if (currentBattery < maxBatteryDuration)
            {
                float rechargeRate = maxBatteryDuration / rechargeDuration;
                currentBattery += Time.deltaTime * rechargeRate;

                if (currentBattery >= maxBatteryDuration)
                {
                    currentBattery = maxBatteryDuration;
                    
                    // DOAR DACA era in starea de incarcare, trimitem mesajul de Full
                    if (isRecharging)
                    {
                        Debug.Log("[SISTEM] Baterie lanterna UV: 100%. Gata de utilizare.");
                    }
                    
                    isRecharging = false; 
                }
            }
        }

        if (batterySlider != null)
        {
            batterySlider.value = currentBattery;
        }
    }

    void ForceTurnOff()
    {
        isOn = false;
        isRecharging = true; 
        if (uvLightObject != null)
        {
            uvLightObject.SetActive(false);
        }
        // Schimbat textul sa sune mai tehnic/realist pentru un dispozitiv de ghost hunting
        Debug.LogWarning("[AVERTIZARE] Baterie descarcata! Sistemul UV intra in mod de reincarcare (10s)...");
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
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;
        Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * lightRange, Color.red);
        
        if (Physics.Raycast(ray, out hit, lightRange, ghostLayer))
        {
            GhostController ghost = hit.collider.GetComponent<GhostController>();
            if (ghost != null)
            {
                ghost.RevealGhost();
            }
        }
    }

    public void Equip() 
    { 
        isEquipped = true; 
        gameObject.SetActive(true);
        if (batterySlider != null) batterySlider.gameObject.SetActive(true);
    }
    
    public void Unequip() 
    { 
        isEquipped = false; 
        isOn = false;
        if (uvLightObject != null) uvLightObject.SetActive(false);
        if (batterySlider != null) batterySlider.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }
}