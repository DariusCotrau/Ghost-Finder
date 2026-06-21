using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Orchestrator de meci, server-authoritative. Spawnat de NetworkBootstrap ca
/// NetworkObject persistent (DontDestroyOnLoad) ca sa supravietuiasca trecerii
/// Lobby -> Joc. Singleton accesibil prin GameManager.Instance.
///
/// Responsabilitati (Faza 2/3): stare meci, conditie de start, atribuire roluri,
/// incarcare scena de joc. Conditiile de victorie + timer vin in Faza 4.
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public const int MinPlayers = 2;

    [Header("Scene")]
    public string gameplaySceneName = "SampleScene";
    public string lobbySceneName = "Lobby";

    public readonly NetworkVariable<bool> MatchStarted =
        new NetworkVariable<bool>(false);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnDestroy()
    {
        if (Instance == this) Instance = null;
        base.OnDestroy();
    }

    /// <summary>Toti jucatorii din lobby (orice peer poate enumera).</summary>
    public static LobbyPlayer[] AllPlayers()
        => Object.FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);

    /// <summary>Server: se poate porni? min jucatori + toti gata.</summary>
    public bool CanStart
    {
        get
        {
            if (!IsServer || MatchStarted.Value) return false;
            var players = AllPlayers();
            if (players.Length < MinPlayers) return false;
            foreach (var p in players)
                if (!p.IsReady.Value) return false;
            return true;
        }
    }

    /// <summary>Server: porneste meciul - atribuie roluri + incarca scena de joc.</summary>
    public void StartMatch()
    {
        if (!IsServer || !CanStart) return;
        AssignRoles();
        MatchStarted.Value = true;
        NetworkManager.SceneManager.LoadScene(gameplaySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    /// <summary>Server: revino in lobby si reseteaza starea.</summary>
    public void ReturnToLobby()
    {
        if (!IsServer) return;
        MatchStarted.Value = false;
        foreach (var p in AllPlayers())
            p.Role.Value = PlayerRole.None;
        NetworkManager.SceneManager.LoadScene(lobbySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    /// <summary>
    /// Atribuie roluri aleator, garantand minim 1 Hunter si 1 Ghost.
    /// Restul impartiti aleator ( usor inclinat spre Hunter).
    /// </summary>
    private void AssignRoles()
    {
        var players = new List<LobbyPlayer>(AllPlayers());
        // Shuffle Fisher-Yates.
        for (int i = players.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (players[i], players[j]) = (players[j], players[i]);
        }

        for (int i = 0; i < players.Count; i++)
        {
            PlayerRole role;
            if (i == 0) role = PlayerRole.Ghost;          // garantat 1 ghost
            else if (i == 1) role = PlayerRole.Hunter;     // garantat 1 hunter
            else role = Random.value < 0.5f ? PlayerRole.Hunter : PlayerRole.Ghost;
            players[i].Role.Value = role;
        }
    }
}
