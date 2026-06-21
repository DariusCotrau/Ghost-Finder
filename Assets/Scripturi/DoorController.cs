using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Usa in scena. NetworkBehaviour: starea deschis/inchis e un NetworkVariable
/// server-write; oricine poate cere comutarea prin ToggleDoorServerRpc
/// (RequireOwnership=false). Usa trebuie sa aiba NetworkObject in scena.
/// Animatia ruleaza pe toate peer-urile catre starea sincronizata.
/// </summary>
public class DoorController : NetworkBehaviour
{
    public float openRotation = 90f;
    public float smooth = 2f;

    private readonly NetworkVariable<bool> IsOpen = new NetworkVariable<bool>(false);

    private Quaternion closedRot;
    private Quaternion openRot;

    private void Start()
    {
        closedRot = transform.localRotation;
        openRot = Quaternion.Euler(0, openRotation, 0) * closedRot;
    }

    private void Update()
    {
        Quaternion target = IsOpen.Value ? openRot : closedRot;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * smooth);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void ToggleDoorServerRpc()
    {
        IsOpen.Value = !IsOpen.Value;
    }
}
