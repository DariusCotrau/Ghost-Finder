using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Pune-l pe acelasi GameObject ca NetworkManager (in scena Lobby).
/// La pornirea serverului spawneaza GameManager-ul (NetworkObject persistent),
/// astfel incat starea meciului supravietuieste schimbarii de scena Lobby -> Joc.
///
/// GameManager NU mai sta in-scene. Foloseste un PREFAB cu NetworkObject + GameManager,
/// inregistrat in lista Network Prefabs din NetworkManager.
/// </summary>
public class NetworkBootstrap : MonoBehaviour
{
    [Tooltip("Prefab cu NetworkObject + GameManager. Trebuie in Network Prefabs din NetworkManager.")]
    public GameObject gameManagerPrefab;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
    }

    private void HandleServerStarted()
    {
        // Doar serverul/host-ul spawneaza. Idempotent: nu dublam daca exista deja.
        if (GameManager.Instance != null) return;
        if (gameManagerPrefab == null)
        {
            Debug.LogError("[BOOT] gameManagerPrefab nesetat - meciul nu poate porni.");
            return;
        }

        GameObject go = Instantiate(gameManagerPrefab);
        go.GetComponent<NetworkObject>().Spawn();
        Debug.Log("[BOOT] GameManager spawnat.");
    }
}
