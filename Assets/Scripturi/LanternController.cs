using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Lanterna de cap (headlight). Toggle cu F doar de owner; starea e sincronizata
/// (NetworkVariable) ca toti jucatorii sa vada lumina celorlalti.
/// </summary>
public class LanternController : NetworkBehaviour
{
    public Light lanternLight; // Referință către componenta de lumină

    // Starea sincronizata. Scrisa de owner, citita de toti.
    public NetworkVariable<bool> IsOn = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    void Awake()
    {
        if (lanternLight == null)
            lanternLight = GetComponent<Light>();
    }

    public override void OnNetworkSpawn()
    {
        IsOn.OnValueChanged += OnLightChanged;
        ApplyLight(IsOn.Value);
    }

    public override void OnNetworkDespawn()
    {
        IsOn.OnValueChanged -= OnLightChanged;
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.F))
            IsOn.Value = !IsOn.Value; // scriere owner -> sincronizat la toti
    }

    private void OnLightChanged(bool oldVal, bool newVal) => ApplyLight(newVal);

    private void ApplyLight(bool on)
    {
        if (lanternLight != null)
            lanternLight.enabled = on;
    }
}
