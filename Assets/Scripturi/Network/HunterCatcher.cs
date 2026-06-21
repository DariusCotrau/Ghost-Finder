using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Pe Player Prefab, activat de PlayerRoleController doar pentru Hunteri.
/// Owner-ul tinteste cu camera si apasa C pentru a prinde o fantoma. Serverul
/// valideaza (nedeja-prinsa, distanta, optional dezvaluita) si o marcheaza Caught.
/// </summary>
public class HunterCatcher : NetworkBehaviour
{
    public Transform aimCamera;
    public float catchRange = 3f;
    public LayerMask ghostLayer;
    public bool requireRevealed = false;

    private void Update()
    {
        if (!IsOwner || aimCamera == null) return;
        if (GameManager.Instance == null || !GameManager.Instance.MatchStarted.Value) return;
        if (GameManager.Instance.MatchEnded.Value) return;

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (Physics.Raycast(aimCamera.position, aimCamera.forward, out RaycastHit hit, catchRange, ghostLayer))
            {
                var ghost = hit.collider.GetComponentInParent<NetworkObject>();
                if (ghost != null) CatchServerRpc(ghost);
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void CatchServerRpc(NetworkObjectReference ghostRef)
    {
        if (!ghostRef.TryGet(out NetworkObject ghostObj)) return;
        var vis = ghostObj.GetComponent<PlayerGhostVisibility>();
        var lp = ghostObj.GetComponent<LobbyPlayer>();
        if (vis == null || lp == null) return;
        if (lp.Role.Value != PlayerRole.Ghost) return;
        if (vis.Caught.Value) return;
        if (requireRevealed && !vis.Revealed.Value) return;

        // Distanta server-side anti-cheat.
        if (Vector3.Distance(transform.position, ghostObj.transform.position) > catchRange + 1.5f) return;

        vis.Caught.Value = true;
        if (GameManager.Instance != null) GameManager.Instance.OnGhostCaught();
    }
}
