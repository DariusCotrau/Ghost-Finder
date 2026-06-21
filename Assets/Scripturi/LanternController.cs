using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Felinar/far de mana. Owner comuta cu F; starea se sincronizeaza prin
/// NetworkVariable (owner-write) ca toti sa vada lumina aprinsa/stinsa.
/// </summary>
public class LanternController : NetworkBehaviour
{
    public Light lanternLight;
    public bool startOn = true;

    private readonly NetworkVariable<bool> IsOn =
        new NetworkVariable<bool>(true,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        if (lanternLight == null) lanternLight = GetComponent<Light>();
        if (IsOwner) IsOn.Value = startOn;
        IsOn.OnValueChanged += (_, on) => Apply(on);
        Apply(IsOn.Value);
    }

    private void Apply(bool on)
    {
        if (lanternLight != null) lanternLight.enabled = on;
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (Input.GetKeyDown(KeyCode.F))
            IsOn.Value = !IsOn.Value;
    }
}
