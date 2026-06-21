using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Miscare jucator, owner-authoritative. Doar owner-ul citeste input + muta
/// Rigidbody; NetworkTransform (owner authority) sincronizeaza pozitia la
/// ceilalti. Controlul e blocat in lobby (meci nepornit), cand jucatorul e
/// prins (ghost) sau eliminat (hunter).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MovementScript : NetworkBehaviour
{
    public float viteza = 5f;
    public float fortaSaritura = 5f;

    private Rigidbody rb;
    private LobbyPlayer lobbyPlayer;
    private PlayerGhostVisibility ghostVis;
    private Vector2 movement;
    private bool vreaSaSara = false;
    private bool estePePamant = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        lobbyPlayer = GetComponent<LobbyPlayer>();
        ghostVis = GetComponent<PlayerGhostVisibility>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[GHOST] Movement.OnNetworkSpawn IsOwner={IsOwner} GM={(GameManager.Instance != null)}");
        // Non-owner nu simuleaza fizica local (NetworkTransform conduce pozitia).
        if (!IsOwner)
            rb.isKinematic = true;
    }

    private bool loggedOnce;
    private bool lastBlocked;

    /// <summary>Controlul e blocat? (lobby, prins, eliminat).</summary>
    private bool ControlBlocat()
    {
        var gm = GameManager.Instance;
        if (gm != null && !gm.MatchStarted.Value) return true;
        if (gm != null && gm.MatchEnded.Value) return true;
        if (ghostVis != null && ghostVis.Caught.Value) return true;
        if (lobbyPlayer != null && lobbyPlayer.Eliminated.Value) return true;
        return false;
    }

    private void Update()
    {
        if (!IsOwner) return;

        bool blocked = ControlBlocat();
        if (!loggedOnce || blocked != lastBlocked)
        {
            loggedOnce = true;
            lastBlocked = blocked;
            var gm = GameManager.Instance;
            Debug.Log($"[GHOST] Movement.Update owner=1 ControlBlocat={blocked} GM={(gm != null)} MatchStarted={(gm != null && gm.MatchStarted.Value)} scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        }

        if (ControlBlocat())
        {
            movement = Vector2.zero;
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        if (Input.GetButtonDown("Jump") && estePePamant)
            vreaSaSara = true;
    }

    private void FixedUpdate()
    {
        if (!IsOwner || rb.isKinematic) return;

        if (ControlBlocat())
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        Vector3 moveDirection = transform.right * movement.x + transform.forward * movement.y;
        if (moveDirection.magnitude > 1) moveDirection.Normalize();

        Vector3 targetVelocity = moveDirection * viteza;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

        if (vreaSaSara)
        {
            rb.AddForce(Vector3.up * fortaSaritura, ForceMode.Impulse);
            vreaSaSara = false;
            estePePamant = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.5f)
            estePePamant = true;
    }

    /// <summary>Server cere owner-ului sa se teleporteze (spawn la rol).</summary>
    [Rpc(SendTo.Owner)]
    public void TeleportRpc(Vector3 pos, float yaw)
    {
        transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
        if (!rb.isKinematic)
            rb.linearVelocity = Vector3.zero;
        var nt = GetComponent<NetworkTransform>();
        if (nt != null) nt.Teleport(pos, Quaternion.Euler(0, yaw, 0), transform.localScale);
    }
}
