using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lanterna UV a Hunterului, owner-authoritative. Owner-ul gestioneaza bateria
/// + raycast-ul de dezvaluire; starea aprins/stins se sincronizeaza prin
/// NetworkVariable ca toti sa vada lumina. Raza loveste fantome-jucator
/// (PlayerGhostVisibility) -> RevealServerRpc.
/// </summary>
public class UVFlashlight : NetworkBehaviour
{
    [Header("Setari Lumina")]
    public GameObject uvLightObject;
    public float lightRange = 10f;
    public LayerMask ghostLayer;
    public Transform aimCamera;

    [Header("Setari Baterie")]
    public float maxBatteryDuration = 5f;
    public float rechargeDuration = 25f;

    [Header("Setari UI (doar owner)")]
    public Slider batterySlider;

    private readonly NetworkVariable<bool> IsOn =
        new NetworkVariable<bool>(false,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private float currentBattery;
    private bool isRecharging = false;

    public override void OnNetworkSpawn()
    {
        currentBattery = maxBatteryDuration;
        IsOn.OnValueChanged += (_, on) => { if (uvLightObject != null) uvLightObject.SetActive(on); };
        if (uvLightObject != null) uvLightObject.SetActive(IsOn.Value);

        if (IsOwner && batterySlider != null)
        {
            batterySlider.maxValue = maxBatteryDuration;
            batterySlider.value = currentBattery;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetButtonDown("Fire1") && !isRecharging)
            IsOn.Value = !IsOn.Value;

        if (IsOn.Value)
        {
            currentBattery -= Time.deltaTime;
            if (currentBattery <= 0)
            {
                currentBattery = 0;
                IsOn.Value = false;
                isRecharging = true;
            }
            else
            {
                DetectGhost();
            }
        }
        else if (currentBattery < maxBatteryDuration)
        {
            currentBattery += Time.deltaTime * (maxBatteryDuration / rechargeDuration);
            if (currentBattery >= maxBatteryDuration)
            {
                currentBattery = maxBatteryDuration;
                isRecharging = false;
            }
        }

        if (batterySlider != null) batterySlider.value = currentBattery;
    }

    private void DetectGhost()
    {
        if (aimCamera == null) return;
        if (Physics.Raycast(aimCamera.position, aimCamera.forward, out RaycastHit hit, lightRange, ghostLayer))
        {
            var ghost = hit.collider.GetComponentInParent<PlayerGhostVisibility>();
            if (ghost != null) ghost.RevealServerRpc();
        }
    }

    public void SetEquipped(bool on) => gameObject.SetActive(on);
}
