using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Echipament de Hunter: prinde fantoma. Owner aimeaza camera spre o fantoma
/// (de regula luminata cu UV ca s-o vezi) si apasa C in raza -> CatchServerRpc.
/// Serverul valideaza si marcheaza fantoma prinsa; GameManager verifica victoria.
/// Activat de PlayerRoleController doar pentru rolul Hunter.
/// </summary>
public class HunterCatcher : NetworkBehaviour
{
    [Header("Catch")]
    public KeyCode catchKey = KeyCode.C;
    public float catchRange = 3f;
    public LayerMask ghostLayer;
    [Tooltip("Daca true, fantoma trebuie sa fie luminata (Revealed) ca s-o poti prinde.")]
    public bool requireRevealed = true;

    [Header("Camera owner-ului")]
    public Camera aimCamera;

    public override void OnNetworkSpawn()
    {
        if (aimCamera == null)
            aimCamera = GetComponentInChildren<Camera>(true);
    }

    void Update()
    {
        if (!IsOwner) return;
        if (aimCamera == null) return;

        if (Input.GetKeyDown(catchKey))
            TryCatch();
    }

    void TryCatch()
    {
        Ray ray = new Ray(aimCamera.transform.position, aimCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, catchRange, ghostLayer))
        {
            var ghost = hit.collider.GetComponentInParent<PlayerGhostVisibility>();
            if (ghost != null && ghost.TryGetComponent(out NetworkObject netObj))
                CatchServerRpc(netObj);
        }
    }

    [Rpc(SendTo.Server)]
    void CatchServerRpc(NetworkObjectReference targetRef)
    {
        if (!targetRef.TryGet(out NetworkObject target)) return;
        if (!target.TryGetComponent(out PlayerGhostVisibility ghost)) return;

        // Validari server-side (anti-cheat de baza).
        if (ghost.Caught.Value) return;
        if (requireRevealed && !ghost.Revealed.Value) return;

        // Distanta reala server-side.
        float dist = Vector3.Distance(transform.position, ghost.transform.position);
        if (dist > catchRange + 1f) return; // mica toleranta de lag

        ghost.Caught.Value = true;
        Debug.Log($"[CATCH] Fantoma {ghost.OwnerClientId} a fost prinsa de hunter {OwnerClientId}!");

        if (GameManager.Instance != null)
            GameManager.Instance.OnGhostCaught();
    }
}
