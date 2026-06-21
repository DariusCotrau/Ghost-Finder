using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Senzor de miscare networked. Detectia (Trigger) ruleaza autoritar pe server;
/// beep-ul e trimis la toti clientii prin ClientRpc, ca toata lumea sa-l auda.
/// Detecteaza orice jucator (Hunter sau Ghost) care intra in raza.
///
/// Necesita NetworkObject pe acest GameObject (senzor plasat in scena sau spawnat).
/// </summary>
public class MotionSensor : NetworkBehaviour
{
    private AudioSource audioSource;

    [Header("Setari Sunet")]
    public float beepInterval = 1f;
    private float beepTimer;

    private int occupants = 0; // cati jucatori sunt in raza (doar pe server)

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        beepTimer = beepInterval;
    }

    void Update()
    {
        // Doar serverul decide ritmul beep-ului (sursa de adevar).
        if (!IsServer) return;

        if (occupants > 0)
        {
            beepTimer -= Time.deltaTime;
            if (beepTimer <= 0f)
            {
                BeepClientRpc();
                beepTimer = beepInterval;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (!other.CompareTag("Player")) return;

        occupants++;
        if (occupants == 1) beepTimer = 0f; // beep instant la prima intrare
        Debug.Log("[SENZOR] Miscare detectata in zona!");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;
        if (!other.CompareTag("Player")) return;

        occupants = Mathf.Max(0, occupants - 1);
        if (occupants == 0)
            Debug.Log("[SENZOR] Zona este din nou sigura.");
    }

    [Rpc(SendTo.Everyone)]
    void BeepClientRpc()
    {
        if (audioSource != null && audioSource.clip != null)
            audioSource.Play();
    }
}
