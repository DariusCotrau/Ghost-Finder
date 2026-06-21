using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Orchestrator de meci, server-authoritative. Spawnat de NetworkBootstrap ca
/// NetworkObject persistent (DontDestroyOnLoad) ca sa supravietuiasca trecerii
/// Lobby -> Joc. Singleton accesibil prin GameManager.Instance.
///
/// Responsabilitati: stare meci, conditie de start, atribuire roluri, incarcare
/// scena de joc, spawn pe roluri, timer + conditii de victorie.
///
/// Victorie: Hunterii castiga cand toate fantomele sunt prinse. Fantomele
/// castiga cand toti hunterii sunt eliminati SAU expira timpul.
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    // TEMP: 1 pentru testare solo. Revino la 2 pentru joc real.
    public const int MinPlayers = 1;

    [Header("Scene")]
    public string gameplaySceneName = "SampleScene";
    public string lobbySceneName = "Lobby";

    [Header("Meci")]
    public float matchDuration = 300f;

    public readonly NetworkVariable<bool> MatchStarted = new NetworkVariable<bool>(false);
    public readonly NetworkVariable<bool> MatchEnded = new NetworkVariable<bool>(false);
    public readonly NetworkVariable<PlayerRole> Winner = new NetworkVariable<PlayerRole>(PlayerRole.None);
    public readonly NetworkVariable<float> TimeRemaining = new NetworkVariable<float>(0f);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
    }

    public override void OnDestroy()
    {
        if (IsServer && NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
        if (Instance == this) Instance = null;
        base.OnDestroy();
    }

    public static LobbyPlayer[] AllPlayers()
        => Object.FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);

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

    public void StartMatch()
    {
        if (!IsServer || !CanStart) return;
        AssignRoles();
        MatchEnded.Value = false;
        Winner.Value = PlayerRole.None;
        TimeRemaining.Value = matchDuration;
        MatchStarted.Value = true;
        NetworkManager.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    public void ReturnToLobby()
    {
        if (!IsServer) return;
        ResetState();
        MatchStarted.Value = false;
        NetworkManager.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
    }

    private void Update()
    {
        if (!IsServer || !MatchStarted.Value || MatchEnded.Value) return;

        TimeRemaining.Value -= Time.deltaTime;
        if (TimeRemaining.Value <= 0f)
        {
            TimeRemaining.Value = 0f;
            EndMatch(PlayerRole.Ghost); // fantomele au supravietuit
        }
    }

    // ---------------- Spawn ----------------

    private void OnSceneLoaded(string sceneName, LoadSceneMode mode,
        List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName != gameplaySceneName) return;
        SpawnPlayers();
    }

    private void SpawnPlayers()
    {
        if (SpawnManager.Instance == null) return;
        int hunters = 0, ghosts = 0;
        foreach (var p in AllPlayers())
        {
            var move = p.GetComponent<MovementScript>();
            if (move == null) continue;

            Transform spawn = p.Role.Value == PlayerRole.Ghost
                ? SpawnManager.Instance.GetSpawn(PlayerRole.Ghost, ghosts++)
                : SpawnManager.Instance.GetSpawn(PlayerRole.Hunter, hunters++);

            if (spawn != null)
                move.TeleportRpc(spawn.position, spawn.eulerAngles.y);
        }
    }

    // ---------------- Roluri ----------------

    private void AssignRoles()
    {
        var players = new List<LobbyPlayer>(AllPlayers());

        // TEMP solo: un singur jucator -> Hunter (ca sa aiba kit + camera de testat).
        if (players.Count == 1)
        {
            players[0].Role.Value = PlayerRole.Hunter;
            return;
        }

        for (int i = players.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (players[i], players[j]) = (players[j], players[i]);
        }

        for (int i = 0; i < players.Count; i++)
        {
            PlayerRole role;
            if (i == 0) role = PlayerRole.Ghost;
            else if (i == 1) role = PlayerRole.Hunter;
            else role = Random.value < 0.5f ? PlayerRole.Hunter : PlayerRole.Ghost;
            players[i].Role.Value = role;
        }
    }

    // ---------------- Conditii de victorie ----------------

    public void OnGhostCaught()
    {
        if (!IsServer || MatchEnded.Value) return;
        foreach (var p in AllPlayers())
        {
            if (p.Role.Value != PlayerRole.Ghost) continue;
            var vis = p.GetComponent<PlayerGhostVisibility>();
            if (vis != null && !vis.Caught.Value) return; // mai exista fantome libere
        }
        EndMatch(PlayerRole.Hunter);
    }

    public void OnHunterEliminated()
    {
        if (!IsServer || MatchEnded.Value) return;
        foreach (var p in AllPlayers())
        {
            if (p.Role.Value != PlayerRole.Hunter) continue;
            if (!p.Eliminated.Value) return; // mai exista hunteri in viata
        }
        EndMatch(PlayerRole.Ghost);
    }

    private void EndMatch(PlayerRole winner)
    {
        if (!IsServer || MatchEnded.Value) return;
        Winner.Value = winner;
        MatchEnded.Value = true;
    }

    private void ResetState()
    {
        Winner.Value = PlayerRole.None;
        MatchEnded.Value = false;
        TimeRemaining.Value = 0f;
        foreach (var p in AllPlayers())
        {
            p.Role.Value = PlayerRole.None;
            p.Eliminated.Value = false;
            var vis = p.GetComponent<PlayerGhostVisibility>();
            if (vis != null) { vis.Caught.Value = false; vis.Revealed.Value = false; }
        }
    }
}
