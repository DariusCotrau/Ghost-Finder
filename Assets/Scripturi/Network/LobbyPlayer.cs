using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Reprezentarea de retea a unui jucator, una per client. Pusa pe Player Prefab
/// (cu NetworkObject). Supravietuieste schimbarii de scena Lobby -> Joc
/// (DontDestroyOnLoad) ca sa pastreze nume + rol.
///
/// NetworkVariables:
///  - DisplayName: scris de server (din numele trimis de owner), citit de toti.
///  - IsReady: scris de owner (toggle in lobby), citit de toti.
///  - Role: scris de server (atribuit la StartMatch), citit de toti.
/// </summary>
public class LobbyPlayer : NetworkBehaviour
{
    public readonly NetworkVariable<FixedString32Bytes> DisplayName =
        new NetworkVariable<FixedString32Bytes>(
            "Player", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public readonly NetworkVariable<bool> IsReady =
        new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public readonly NetworkVariable<PlayerRole> Role =
        new NetworkVariable<PlayerRole>(
            PlayerRole.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Hunter doborat de o fantoma (GhostAttacker). Server-write.
    public readonly NetworkVariable<bool> Eliminated =
        new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        // NU apela DontDestroyOnLoad: NGO migreaza singur obiectele spawnate
        // (DestroyWithScene=false) la incarcarea scenei prin NetworkManager.
        // DontDestroyOnLoad + scene management dezactiveaza obiectul.
        if (IsOwner)
        {
            string name = PlayerPrefs.GetString("gf_player_name", "");
            if (string.IsNullOrWhiteSpace(name))
                name = "Player " + OwnerClientId;
            SubmitNameServerRpc(name);
        }
    }

    /// <summary>Owner trimite numele ales; serverul il scrie in NetworkVariable.</summary>
    [ServerRpc]
    private void SubmitNameServerRpc(string name)
    {
        if (name.Length > 24) name = name.Substring(0, 24);
        DisplayName.Value = name;
    }

    /// <summary>Apelat local de owner pentru a comuta starea de "gata".</summary>
    public void ToggleReady()
    {
        if (IsOwner) IsReady.Value = !IsReady.Value;
    }
}
