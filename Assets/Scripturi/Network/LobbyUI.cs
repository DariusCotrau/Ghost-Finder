using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// UI minimal (IMGUI) pentru lobby LAN: Host / Client (IP) / Start.
/// Zero wiring de Canvas - se pune pe un GameObject gol si merge.
/// Inlocuieste-l mai tarziu cu un Canvas uGUI daca vrei UI stilizat.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    private string joinAddress = "127.0.0.1";
    private string status = "";

    private void OnGUI()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            GUI.Label(new Rect(10, 10, 400, 20), "NetworkManager lipseste din scena.");
            return;
        }

        GUILayout.BeginArea(new Rect(10, 10, 320, 400));

        if (!nm.IsClient && !nm.IsServer)
        {
            // --- Meniu pre-conectare ---
            GUILayout.Label("=== GHOST FINDER - LOBBY (LAN) ===");

            GUILayout.Label("IP server (pentru Client):");
            joinAddress = GUILayout.TextField(joinAddress);

            if (GUILayout.Button("HOST (Server + Joc)"))
            {
                SetAddress(joinAddress);
                nm.StartHost();
            }

            if (GUILayout.Button("CLIENT (Join)"))
            {
                SetAddress(joinAddress);
                if (nm.StartClient())
                    status = "Conectare...";
            }

            if (!string.IsNullOrEmpty(status))
                GUILayout.Label(status);
        }
        else
        {
            // --- In lobby / in meci ---
            // ConnectedClientsList e server-only in NGO; pe client ar da eroare.
            // Folosim jucatorii spawnati (exista local pe orice peer).
            LobbyPlayer[] players = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);

            string mode = nm.IsHost ? "HOST" : (nm.IsServer ? "SERVER" : "CLIENT");
            GUILayout.Label($"Mod: {mode}  |  Jucatori: {players.Length}");

            var gm = GameManager.Instance;
            if (gm != null)
            {
                if (gm.MatchEnded.Value)
                    GUILayout.Label($">>> MECI TERMINAT - Castiga: {gm.Winner.Value} <<<");
                else if (gm.MatchStarted.Value)
                {
                    int t = Mathf.CeilToInt(gm.TimeRemaining.Value);
                    GUILayout.Label($"MECI PORNIT  |  Timp: {t / 60:00}:{t % 60:00}");
                    GUILayout.Label("Hunter: UV=click, Catch=C  |  Ghost: Atac=C");
                }
                else
                    GUILayout.Label($"In asteptare (min {GameManager.MinPlayers} jucatori)...");
            }

            // Lista jucatori + roluri
            GUILayout.Space(6);
            GUILayout.Label("--- Jucatori ---");
            foreach (var lp in players)
            {
                string role = lp.Role.Value == PlayerRole.None ? "(neatribuit)" : lp.Role.Value.ToString();
                GUILayout.Label($"{lp.DisplayName.Value}: {role}");
            }

            // Buton Start - doar host, doar daca poate porni
            GUILayout.Space(6);
            if (nm.IsServer && gm != null)
            {
                if (!gm.MatchStarted.Value)
                {
                    GUI.enabled = gm.CanStart;
                    if (GUILayout.Button("START MECI (atribuie roluri)"))
                        gm.StartMatch();
                    GUI.enabled = true;
                }
                else
                {
                    if (GUILayout.Button("RESET (lobby nou)"))
                        gm.ResetMatch();
                }
            }

            GUILayout.Space(6);
            if (GUILayout.Button("DECONECTARE"))
            {
                nm.Shutdown();
                status = "";
            }
        }

        GUILayout.EndArea();
    }

    private void SetAddress(string addr)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
            transport.ConnectionData.Address = string.IsNullOrWhiteSpace(addr) ? "127.0.0.1" : addr.Trim();
    }
}
