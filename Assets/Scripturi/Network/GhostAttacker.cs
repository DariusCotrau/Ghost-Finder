using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Abilitatea fantomei: elimina hunterii. Owner aimeaza un hunter in raza si
/// apasa tasta de atac -> AttackServerRpc. Serverul valideaza si marcheaza
/// hunterul eliminat. Toti hunterii eliminati -> fantoma castiga.
/// Activat de PlayerRoleController doar pentru rolul Ghost.
/// </summary>
public class GhostAttacker : NetworkBehaviour
{
    [Header("Atac")]
    public KeyCode attackKey = KeyCode.C;
    public float attackRange = 2.5f;
    public float attackCooldown = 3f;

    [Header("Camera owner-ului")]
    public Camera aimCamera;

    private float cooldownTimer = 0f;

    public override void OnNetworkSpawn()
    {
        if (aimCamera == null)
            aimCamera = GetComponentInChildren<Camera>(true);
    }

    void Update()
    {
        if (!IsOwner) return;
        if (aimCamera == null) return;

        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(attackKey) && cooldownTimer <= 0f)
            TryAttack();
    }

    void TryAttack()
    {
        Ray ray = new Ray(aimCamera.transform.position, aimCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, attackRange))
        {
            var target = hit.collider.GetComponentInParent<LobbyPlayer>();
            if (target != null &&
                target.Role.Value == PlayerRole.Hunter &&
                !target.Eliminated.Value &&
                target.TryGetComponent(out NetworkObject netObj))
            {
                cooldownTimer = attackCooldown;
                AttackServerRpc(netObj);
            }
        }
    }

    [Rpc(SendTo.Server)]
    void AttackServerRpc(NetworkObjectReference targetRef)
    {
        if (!targetRef.TryGet(out NetworkObject target)) return;
        if (!target.TryGetComponent(out LobbyPlayer hunter)) return;

        // Validari server-side.
        if (hunter.Role.Value != PlayerRole.Hunter) return;
        if (hunter.Eliminated.Value) return;

        float dist = Vector3.Distance(transform.position, hunter.transform.position);
        if (dist > attackRange + 1f) return; // toleranta lag

        hunter.Eliminated.Value = true;
        Debug.Log($"[ATAC] Hunter {hunter.OwnerClientId} eliminat de fantoma {OwnerClientId}!");

        if (GameManager.Instance != null)
            GameManager.Instance.OnHunterEliminated();
    }
}
