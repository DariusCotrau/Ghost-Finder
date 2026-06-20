using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// Componenta atasata pe Player Prefab (cel inregistrat in NetworkManager).
/// Tine rolul atribuit (sincronizat in retea) si numele jucatorului.
/// Rolul este scris doar de Server prin GameManager.AssignRoles().
/// </summary>
public class LobbyPlayer : NetworkBehaviour
{
    // Rolul jucatorului. Default None pana cand serverul porneste meciul.
    public NetworkVariable<PlayerRole> Role = new NetworkVariable<PlayerRole>(
        PlayerRole.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Nume afisat in lobby (Player + clientId). Scris de server.
    public NetworkVariable<FixedString32Bytes> DisplayName = new NetworkVariable<FixedString32Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Hunter eliminat de fantoma (echivalentul lui Caught pentru ghost). Scris de server.
    public NetworkVariable<bool> Eliminated = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            DisplayName.Value = $"Player {OwnerClientId}";
        }

        Role.OnValueChanged += OnRoleChanged;

        // Daca rolul a fost deja setat inainte sa ne abonam (join tarziu), aplicam acum.
        if (Role.Value != PlayerRole.None)
            OnRoleChanged(PlayerRole.None, Role.Value);
    }

    public override void OnNetworkDespawn()
    {
        Role.OnValueChanged -= OnRoleChanged;
    }

    private void OnRoleChanged(PlayerRole oldRole, PlayerRole newRole)
    {
        // Hook pentru logica de gameplay (activare echipament, model, etc.).
        // Momentan doar logam; conversia gameplay-ului vine in pasul urmator.
        if (IsOwner)
            Debug.Log($"[ROL] Esti acum: {newRole}");
    }
}
