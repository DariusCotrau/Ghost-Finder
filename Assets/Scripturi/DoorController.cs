using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Usa networked. Starea (deschis/inchis) e NetworkVariable autoritate-server,
/// deci toti jucatorii vad aceeasi animatie. Orice client poate cere toggle
/// (RequireOwnership=false) cand interactioneaza.
///
/// Necesita NetworkObject pe acest GameObject (usa plasata in scena).
/// </summary>
public class DoorController : NetworkBehaviour
{
    public float openRotation = 90f; // Unghiul de deschidere
    public float smooth = 2f;        // Viteza animației

    // Stare sincronizata. Scrisa doar de server.
    public NetworkVariable<bool> IsOpen = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Quaternion closedRot;
    private Quaternion openRot;

    void Awake()
    {
        // Memoram rotatia initiala (inchisa) inainte de orice animatie.
        closedRot = transform.localRotation;
        openRot = Quaternion.Euler(0, openRotation, 0) * closedRot;
    }

    void Update()
    {
        // Toti clientii animeaza catre starea sincronizata.
        Quaternion target = IsOpen.Value ? openRot : closedRot;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * smooth);
    }

    /// <summary>
    /// Apelat de client (PlayerInteraction) la apasarea E.
    /// </summary>
    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void ToggleDoorServerRpc()
    {
        IsOpen.Value = !IsOpen.Value;
    }
}
