using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton de scena (NetworkObject). Gestioneaza starea meciului si
/// atribuirea randomizata a rolurilor.
///
/// Regula: meciul poate porni doar cu minim 2 jucatori, iar dupa atribuire
/// exista garantat cel putin un Hunter si o Fantoma.
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public const int MinPlayers = 2;

    [Header("Scene")]
    [Tooltip("Numele scenei de joc (mapa). Trebuie adaugata in Build Settings.")]
    public string gameplaySceneName = "SampleScene";
    [Tooltip("Numele scenei de lobby. Trebuie adaugata in Build Settings.")]
    public string lobbySceneName = "Lobby";

    [Header("Setari Meci")]
    [Tooltip("Durata meciului in secunde. La expirare, daca exista fantome ramase -> Ghost castiga (a supravietuit).")]
    public float matchDuration = 300f;

    // Timp ramas, sincronizat. Scris de server.
    public NetworkVariable<float> TimeRemaining = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // true dupa ce serverul a pornit meciul (rolurile atribuite). Sincronizat.
    public NetworkVariable<bool> MatchStarted = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // true cand meciul s-a terminat. Sincronizat.
    public NetworkVariable<bool> MatchEnded = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Rolul castigator (Hunter daca prind toate fantomele). Sincronizat.
    public NetworkVariable<PlayerRole> Winner = new NetworkVariable<PlayerRole>(
        PlayerRole.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Persistent intre scena Lobby si scena de joc (spawnat dinamic de NetworkBootstrap).
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Numarul de jucatori conectati. Valid doar pe server/host.
    /// </summary>
    public int ConnectedPlayerCount =>
        NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsList.Count : 0;

    public bool CanStart => IsServer && !MatchStarted.Value && ConnectedPlayerCount >= MinPlayers;

    /// <summary>
    /// Apelat de host pentru a porni meciul. Atribuie roluri randomizate.
    /// </summary>
    public void StartMatch()
    {
        if (!CanStart)
        {
            Debug.LogWarning($"[GAME] Nu pot porni: jucatori={ConnectedPlayerCount} (min {MinPlayers}), pornit={MatchStarted.Value}");
            return;
        }

        // Incarcam scena de joc pe toti clientii. Rolurile + spawn-ul se fac
        // dupa ce scena s-a incarcat peste tot (SpawnManager exista abia acolo).
        NetworkManager.SceneManager.OnLoadEventCompleted += OnGameplayLoaded;
        NetworkManager.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
        Debug.Log($"[GAME] Incarc scena de joc '{gameplaySceneName}'...");
    }

    /// <summary>
    /// Apelat cand scena de joc s-a incarcat la toti clientii. Atribuie roluri,
    /// pozitioneaza jucatorii si porneste meciul. Server-only.
    /// </summary>
    private void OnGameplayLoaded(string sceneName, LoadSceneMode mode, List<ulong> done, List<ulong> timedOut)
    {
        if (sceneName != gameplaySceneName) return;
        NetworkManager.SceneManager.OnLoadEventCompleted -= OnGameplayLoaded;

        AssignRoles();
        SpawnPlayers();
        TimeRemaining.Value = matchDuration;
        MatchStarted.Value = true;
        MatchEnded.Value = false;
        Winner.Value = PlayerRole.None;
        Debug.Log("[GAME] Scena incarcata. Meci pornit, roluri atribuite.");
    }

    void Update()
    {
        // Serverul gestioneaza timer-ul de meci.
        if (!IsServer) return;
        if (!MatchStarted.Value || MatchEnded.Value) return;

        TimeRemaining.Value -= Time.deltaTime;
        if (TimeRemaining.Value <= 0f)
        {
            TimeRemaining.Value = 0f;
            // Timp expirat: daca a mai ramas vreo fantoma neprinsa, fantomele castiga.
            EndMatch(PlayerRole.Ghost);
        }
    }

    /// <summary>
    /// Pozitioneaza fiecare jucator la spawn point-ul rolului sau (daca exista
    /// un SpawnManager in scena). Server-only.
    /// </summary>
    private void SpawnPlayers()
    {
        if (SpawnManager.Instance == null)
        {
            Debug.LogWarning("[GAME] Lipseste SpawnManager - jucatorii raman pe loc.");
            return;
        }

        int hunterIdx = 0, ghostIdx = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;
            if (!client.PlayerObject.TryGetComponent(out LobbyPlayer lp)) continue;

            int idx = lp.Role.Value == PlayerRole.Ghost ? ghostIdx++ : hunterIdx++;
            Transform sp = SpawnManager.Instance.GetSpawn(lp.Role.Value, idx);
            if (sp == null) continue;

            if (client.PlayerObject.TryGetComponent(out MovementScript mv))
                mv.TeleportRpc(sp.position, sp.eulerAngles.y);
        }
    }

    /// <summary>
    /// Apelat de server (HunterCatcher) cand o fantoma e prinsa.
    /// Daca toate fantomele sunt prinse -> hunters castiga.
    /// </summary>
    public void OnGhostCaught()
    {
        if (!IsServer || MatchEnded.Value) return;

        int ghostsRemaining = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null &&
                client.PlayerObject.TryGetComponent(out LobbyPlayer lp) &&
                lp.Role.Value == PlayerRole.Ghost)
            {
                var gv = client.PlayerObject.GetComponent<PlayerGhostVisibility>();
                if (gv != null && !gv.Caught.Value)
                    ghostsRemaining++;
            }
        }

        Debug.Log($"[GAME] Fantome ramase: {ghostsRemaining}");
        if (ghostsRemaining == 0)
            EndMatch(PlayerRole.Hunter);
    }

    /// <summary>
    /// Apelat de server (GhostAttacker) cand un hunter e eliminat.
    /// Daca toti hunterii sunt eliminati -> fantomele castiga.
    /// </summary>
    public void OnHunterEliminated()
    {
        if (!IsServer || MatchEnded.Value) return;

        int huntersRemaining = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null &&
                client.PlayerObject.TryGetComponent(out LobbyPlayer lp) &&
                lp.Role.Value == PlayerRole.Hunter &&
                !lp.Eliminated.Value)
            {
                huntersRemaining++;
            }
        }

        Debug.Log($"[GAME] Hunteri ramasi: {huntersRemaining}");
        if (huntersRemaining == 0)
            EndMatch(PlayerRole.Ghost);
    }

    /// <summary>
    /// Termina meciul cu un castigator. Server-only.
    /// </summary>
    public void EndMatch(PlayerRole winner)
    {
        if (!IsServer || MatchEnded.Value) return;
        MatchEnded.Value = true;
        Winner.Value = winner;
        Debug.Log($"[GAME] Meci terminat. Castigator: {winner}");
    }

    /// <summary>
    /// Atribuie un rol randomizat fiecarui jucator conectat, garantand
    /// minim un Hunter si o Fantoma.
    /// </summary>
    private void AssignRoles()
    {
        // Strangem toti LobbyPlayer din clientii conectati.
        List<LobbyPlayer> players = new List<LobbyPlayer>();
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null &&
                client.PlayerObject.TryGetComponent(out LobbyPlayer lp))
            {
                players.Add(lp);
            }
        }

        if (players.Count < MinPlayers)
        {
            Debug.LogWarning("[GAME] Prea putini LobbyPlayer valizi pentru atribuire.");
            return;
        }

        // 1) Rol random pentru fiecare (50/50 Hunter/Ghost).
        foreach (var p in players)
            p.Role.Value = (Random.value < 0.5f) ? PlayerRole.Hunter : PlayerRole.Ghost;

        // 2) Garantam minim 1 Hunter si 1 Ghost.
        EnsureAtLeastOne(players, PlayerRole.Ghost);
        EnsureAtLeastOne(players, PlayerRole.Hunter);

        foreach (var p in players)
            Debug.Log($"[GAME] {p.DisplayName.Value} -> {p.Role.Value}");
    }

    /// <summary>
    /// Daca niciun jucator nu are rolul cerut, forteaza un jucator random
    /// (dintre cei care au rolul opus) sa-l preia. Nu rupe celalalt minim
    /// pentru ca apelam intai pentru Ghost, apoi pentru Hunter, si exista >=2 jucatori.
    /// </summary>
    private void EnsureAtLeastOne(List<LobbyPlayer> players, PlayerRole required)
    {
        bool hasRole = players.Exists(p => p.Role.Value == required);
        if (hasRole) return;

        PlayerRole opposite = (required == PlayerRole.Ghost) ? PlayerRole.Hunter : PlayerRole.Ghost;

        // Candidati: cei cu rolul opus (toti, in acest caz, fiindca lipseste 'required').
        List<LobbyPlayer> candidates = players.FindAll(p => p.Role.Value == opposite);
        if (candidates.Count == 0) return;

        var chosen = candidates[Random.Range(0, candidates.Count)];
        chosen.Role.Value = required;
    }

    /// <summary>
    /// Reset rapid pentru un nou meci (host).
    /// </summary>
    public void ResetMatch()
    {
        if (!IsServer) return;
        MatchStarted.Value = false;
        MatchEnded.Value = false;
        Winner.Value = PlayerRole.None;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            if (client.PlayerObject.TryGetComponent(out LobbyPlayer lp))
            {
                lp.Role.Value = PlayerRole.None;
                lp.Eliminated.Value = false;
            }

            if (client.PlayerObject.TryGetComponent(out PlayerGhostVisibility gv))
            {
                gv.Caught.Value = false;
                gv.Revealed.Value = false;
            }
        }

        // Inapoi in scena de lobby.
        NetworkManager.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
    }
}
