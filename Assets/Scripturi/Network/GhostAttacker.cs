using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Pe Player Prefab, activat de PlayerRoleController doar pentru Ghosti.
/// Owner-ul tinteste un Hunter si apasa C pentru a-l doborî (cu cooldown).
/// Serverul valideaza (e hunter, neeliminat, distanta) si il marcheaza Eliminated.
/// </summary>
public class GhostAttacker : NetworkBehaviour
{
    public Transform aimCamera;
    public float attackRange = 3f;
    public LayerMask hunterLayer;
    public float cooldown = 5f;

    private float nextAttack;

    private void Update()
    {
        if (!IsOwner || aimCamera == null) return;
        if (GameManager.Instance == null || !GameManager.Instance.MatchStarted.Value) return;
        if (GameManager.Instance.MatchEnded.Value) return;
        if (Time.time < nextAttack) return;

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (Physics.Raycast(aimCamera.position, aimCamera.forward, out RaycastHit hit, attackRange, hunterLayer))
            {
                var hunter = hit.collider.GetComponentInParent<NetworkObject>();
                if (hunter != null)
                {
                    nextAttack = Time.time + cooldown;
                    AttackServerRpc(hunter);
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void AttackServerRpc(NetworkObjectReference hunterRef)
    {
        if (!hunterRef.TryGet(out NetworkObject hunterObj)) return;
        var lp = hunterObj.GetComponent<LobbyPlayer>();
        if (lp == null || lp.Role.Value != PlayerRole.Hunter) return;
        if (lp.Eliminated.Value) return;

        if (Vector3.Distance(transform.position, hunterObj.transform.position) > attackRange + 1.5f) return;

        lp.Eliminated.Value = true;
        if (GameManager.Instance != null) GameManager.Instance.OnHunterEliminated();
    }
}
