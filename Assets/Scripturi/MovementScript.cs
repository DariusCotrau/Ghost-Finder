using Unity.Netcode;
using UnityEngine;

public class MovementScript : NetworkBehaviour
{
    public float viteza = 5f;
    public float fortaSaritura = 5f; // Puterea săriturii

    private Rigidbody rb;
    private Vector2 movement;
    private bool vreaSaSara = false;
    private bool estePePamant = true;
    private PlayerGhostVisibility ghostVis; // null daca nu e fantoma
    private LobbyPlayer lobbyPlayer;

    // Control blocat daca: fantoma prinsa SAU hunter eliminat.
    private bool ContolBlocat =>
        (ghostVis != null && ghostVis.Caught.Value) ||
        (lobbyPlayer != null && lobbyPlayer.Eliminated.Value);

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Împiedică jucătorul să se rotească aiurea la coliziuni
    }

    public override void OnNetworkSpawn()
    {
        // Doar owner-ul simuleaza fizica local. Pe celelalte clienti corpul
        // este miscat de NetworkTransform (Owner authority), deci facem kinematic
        // ca sa nu se bata cu sincronizarea.
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (!IsOwner && rb != null)
            rb.isKinematic = true;

        ghostVis = GetComponent<PlayerGhostVisibility>();
        lobbyPlayer = GetComponent<LobbyPlayer>();
    }

    /// <summary>
    /// Mutat de server (GameManager) la spawn point-ul rolului. Ruleaza pe owner
    /// (autoritatea NetworkTransform), deci pozitia se propaga corect la toti.
    /// </summary>
    [Rpc(SendTo.Owner)]
    public void TeleportRpc(Vector3 pos, float yaw)
    {
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);

        var nt = GetComponent<Unity.Netcode.Components.NetworkTransform>();
        if (nt != null)
            nt.Teleport(pos, rot, transform.localScale); // snap fara interpolare
        else
            transform.SetPositionAndRotation(pos, rot);

        if (rb != null)
        {
            rb.position = pos;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void Update()
    {
        if (!IsOwner) return;
        if (ContolBlocat) { movement = Vector2.zero; return; }

        // Input mișcare
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Verificăm dacă apasă Space în Update (pentru a nu rata input-ul)
        if (Input.GetButtonDown("Jump") && estePePamant)
        {
            vreaSaSara = true;
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
        if (ContolBlocat)
        {
            // Oprim orice viteza orizontala ramasa.
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        // Mișcare orizontală
        Vector3 moveDirection = transform.right * movement.x + transform.forward * movement.y;
        if (moveDirection.magnitude > 1) moveDirection.Normalize();

        MoveCharacter(moveDirection);

        // Logică săritură
        if (vreaSaSara)
        {
            rb.AddForce(Vector3.up * fortaSaritura, ForceMode.Impulse);
            vreaSaSara = false;
            estePePamant = false; // Presupunem că a părăsit solul
        }
    }

    void MoveCharacter(Vector3 direction)
    {
        Vector3 targetVelocity = direction * viteza;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }

    // Detectăm când atingem din nou pământul
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner) return;

        // Verificăm dacă obiectul de care ne-am izbit are o suprafață orizontală
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.5f)
        {
            estePePamant = true;
        }
    }
}
