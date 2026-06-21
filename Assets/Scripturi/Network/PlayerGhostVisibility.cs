using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Pe Player Prefab. Cand jucatorul are rol Ghost, e invizibil pentru Hunteri
/// cu exceptia momentelor in care e dezvaluit (UV) sau prins. Pentru ghosti
/// (coechipieri) si pentru el insusi ramane vizibil. Ruleaza pe TOATE peer-urile
/// ca sa ascunda corect modelul pe ecranul fiecarui hunter.
///
/// NetworkVariables (server-write):
///  - Revealed: dezvaluit temporar de lanterna UV.
///  - Caught: prins definitiv (devine vizibil tuturor + inghetat).
/// </summary>
public class PlayerGhostVisibility : NetworkBehaviour
{
    [Header("Reveal")]
    public float revealDuration = 1.5f;

    public readonly NetworkVariable<bool> Revealed =
        new NetworkVariable<bool>(false);
    public readonly NetworkVariable<bool> Caught =
        new NetworkVariable<bool>(false);

    private LobbyPlayer lobbyPlayer;
    private Renderer[] renderers;
    private float revealTimer;

    private void Awake()
    {
        lobbyPlayer = GetComponent<LobbyPlayer>();
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    private void Update()
    {
        // Server: scade timerul de reveal.
        if (IsServer && Revealed.Value && !Caught.Value)
        {
            revealTimer -= Time.deltaTime;
            if (revealTimer <= 0f) Revealed.Value = false;
        }

        ApplyVisibility();
    }

    private bool IsGhost() => lobbyPlayer != null && lobbyPlayer.Role.Value == PlayerRole.Ghost;

    private PlayerRole LocalViewerRole()
    {
        foreach (var lp in GameManager.AllPlayers())
            if (lp.IsOwner) return lp.Role.Value;
        return PlayerRole.None;
    }

    private void ApplyVisibility()
    {
        if (renderers == null) return;

        bool visible = true;
        if (IsGhost() && !Caught.Value)
        {
            // Owner-ul (fantoma insasi) si ceilalti ghosti vad fantoma;
            // hunterii o vad doar daca e dezvaluita.
            if (IsOwner) visible = true;
            else
            {
                var viewer = LocalViewerRole();
                visible = viewer == PlayerRole.Ghost || Revealed.Value;
            }
        }

        foreach (var r in renderers)
            if (r != null && r.enabled != visible) r.enabled = visible;
    }

    /// <summary>Hunterul (UV) cere dezvaluirea. Apelat pe obiectul fantomei.</summary>
    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void RevealServerRpc()
    {
        if (Caught.Value) return;
        Revealed.Value = true;
        revealTimer = revealDuration;
    }
}
