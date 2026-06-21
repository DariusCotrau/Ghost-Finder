using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lanterna UV - echipament de Hunter. Procesata doar de owner.
/// Cand raza loveste un jucator-fantoma (PlayerGhostVisibility), cere
/// reveal-ul prin ServerRpc, deci toti clientii vad fantoma luminata.
/// Activata/dezactivata de PlayerRoleController in functie de rol.
/// </summary>
public class UVFlashlight : NetworkBehaviour
{
    [Header("Setari Lumina")]
    public GameObject uvLightObject;
    public float lightRange = 10f;
    public LayerMask ghostLayer;

    [Header("Camera owner-ului (pt raycast)")]
    public Camera aimCamera;

    [Header("Setari Baterie (Nerf)")]
    public float maxBatteryDuration = 5f;
    public float rechargeDuration = 25f;

    [Header("Setari UI")]
    public Slider batterySlider; // doar pt owner

    private float currentBattery;
    private bool isRecharging = false;
    private bool isEquipped = true;
    private bool isOn = false;

    void Start()
    {
        currentBattery = maxBatteryDuration;

        if (batterySlider != null)
        {
            batterySlider.maxValue = maxBatteryDuration;
            batterySlider.value = currentBattery;
        }

        isOn = false;
        if (uvLightObject != null)
            uvLightObject.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        // UI baterie doar pt jucatorul local.
        if (!IsOwner && batterySlider != null)
            batterySlider.gameObject.SetActive(false);

        if (aimCamera == null)
            aimCamera = GetComponentInChildren<Camera>(true);
    }

    void Update()
    {
        // Doar owner-ul controleaza lanterna; ceilalti nu proceseaza input.
        if (!IsOwner) return;
        if (!isEquipped) return;

        if (Input.GetButtonDown("Fire1") && !isRecharging)
            ToggleFlashlight();

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
                    if (isRecharging)
                        Debug.Log("[SISTEM] Baterie lanterna UV: 100%. Gata de utilizare.");
                    isRecharging = false;
                }
            }
        }

        if (batterySlider != null)
            batterySlider.value = currentBattery;
    }

    void ForceTurnOff()
    {
        isOn = false;
        isRecharging = true;
        SetLightActive(false);
        Debug.LogWarning("[AVERTIZARE] Baterie descarcata! Sistemul UV intra in mod de reincarcare...");
    }

    void ToggleFlashlight()
    {
        isOn = !isOn;
        SetLightActive(isOn);
    }

    // Lumina UV trebuie vazuta si de ceilalti jucatori -> sincronizam vizualul.
    void SetLightActive(bool active)
    {
        if (uvLightObject != null) uvLightObject.SetActive(active);
        SetLightServerRpc(active);
    }

    [Rpc(SendTo.Server)]
    void SetLightServerRpc(bool active)
    {
        SetLightClientRpc(active);
    }

    [Rpc(SendTo.NotOwner)]
    void SetLightClientRpc(bool active)
    {
        if (uvLightObject != null) uvLightObject.SetActive(active);
    }

    void DetectGhost()
    {
        if (aimCamera == null) return;

        Ray ray = new Ray(aimCamera.transform.position, aimCamera.transform.forward);
        Debug.DrawRay(aimCamera.transform.position, aimCamera.transform.forward * lightRange, Color.red);

        if (Physics.Raycast(ray, out RaycastHit hit, lightRange, ghostLayer))
        {
            // Jucator-fantoma?
            var ghost = hit.collider.GetComponentInParent<PlayerGhostVisibility>();
            if (ghost != null)
            {
                ghost.RevealServerRpc();
                return;
            }

            // Fallback: fantoma AI veche (daca mai exista in scena).
            var aiGhost = hit.collider.GetComponent<GhostController>();
            if (aiGhost != null)
                aiGhost.RevealGhost();
        }
    }

    public void Equip()
    {
        isEquipped = true;
        gameObject.SetActive(true);
        if (IsOwner && batterySlider != null) batterySlider.gameObject.SetActive(true);
    }

    public void Unequip()
    {
        isEquipped = false;
        isOn = false;
        SetLightActive(false);
        if (batterySlider != null) batterySlider.gameObject.SetActive(false);
    }
}
