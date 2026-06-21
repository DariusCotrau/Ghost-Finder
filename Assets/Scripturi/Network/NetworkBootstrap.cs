using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// Pus pe GameObject-ul NetworkManager din scena Lobby. La incarcarea scenei
/// citeste alegerea facuta in Main Menu (PlayerPrefs gf_net_role / gf_join_ip)
/// si porneste automat Host sau Client. La pornirea serverului spawneaza
/// prefab-ul GameManager.
///
/// Wiring Editor: NetworkManager + UnityTransport + acest script pe acelasi
/// obiect; gameManagerPrefab setat la prefab-ul GameManager; player prefab
/// (cu LobbyPlayer) setat in NetworkManager.NetworkConfig.
/// </summary>
[RequireComponent(typeof(NetworkManager))]
public class NetworkBootstrap : MonoBehaviour
{
    [Tooltip("Prefab cu NetworkObject + GameManager. Spawnat de server.")]
    public GameObject gameManagerPrefab;

    [Tooltip("Porneste automat conexiunea la incarcarea scenei, pe baza PlayerPrefs.")]
    public bool autoConnect = true;

    private NetworkManager nm;

    private void Awake()
    {
        nm = GetComponent<NetworkManager>();
        nm.OnServerStarted += OnServerStarted;
    }

    private void Start()
    {
        if (autoConnect) Connect();
    }

    private void Connect()
    {
        if (nm.IsClient || nm.IsServer) return;

        string role = PlayerPrefs.GetString("gf_net_role", "host");
        if (role == "join")
        {
            string ip = PlayerPrefs.GetString("gf_join_ip", "127.0.0.1");
            var transport = nm.GetComponent<UnityTransport>();
            if (transport != null)
                transport.ConnectionData.Address = string.IsNullOrWhiteSpace(ip) ? "127.0.0.1" : ip.Trim();
            nm.StartClient();
        }
        else
        {
            nm.StartHost();
        }
    }

    private void OnServerStarted()
    {
        if (gameManagerPrefab == null) return;
        if (GameManager.Instance != null) return; // persista intre scene
        var go = Instantiate(gameManagerPrefab);
        go.GetComponent<NetworkObject>().Spawn();
    }

    private void OnDestroy()
    {
        if (nm != null) nm.OnServerStarted -= OnServerStarted;
    }
}
