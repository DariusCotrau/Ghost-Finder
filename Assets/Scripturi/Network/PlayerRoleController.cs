using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Pe Player Prefab. Asculta LobbyPlayer.Role si activeaza kitul potrivit:
///  - Hunter: lanterna UV + HunterCatcher + far (headlight).
///  - Ghost: PlayerGhostVisibility + GhostAttacker.
/// Seteaza si layer-ul fizic al jucatorului pe rol, ca raycast-urile de
/// reveal/catch/attack sa loveasca tinta corecta. Ruleaza pe toate peer-urile.
/// </summary>
[RequireComponent(typeof(LobbyPlayer))]
public class PlayerRoleController : NetworkBehaviour
{
    [Header("Hunter")]
    public UVFlashlight uvFlashlight;
    public HunterCatcher hunterCatcher;
    public GameObject hunterHeadlight;

    [Header("Ghost")]
    public PlayerGhostVisibility ghostVisibility;
    public GhostAttacker ghostAttacker;

    [Header("Layere (index, nu mask)")]
    public int hunterLayer = 0;
    public int ghostLayer = 0;

    private LobbyPlayer lobbyPlayer;

    private void Awake()
    {
        lobbyPlayer = GetComponent<LobbyPlayer>();
    }

    public override void OnNetworkSpawn()
    {
        lobbyPlayer.Role.OnValueChanged += OnRoleChanged;
        Apply(lobbyPlayer.Role.Value);
    }

    public override void OnNetworkDespawn()
    {
        if (lobbyPlayer != null) lobbyPlayer.Role.OnValueChanged -= OnRoleChanged;
    }

    private void OnRoleChanged(PlayerRole _, PlayerRole now) => Apply(now);

    private void Apply(PlayerRole role)
    {
        bool hunter = role == PlayerRole.Hunter;
        bool ghost = role == PlayerRole.Ghost;

        if (uvFlashlight != null) uvFlashlight.gameObject.SetActive(hunter);
        if (hunterCatcher != null) hunterCatcher.enabled = hunter;
        if (hunterHeadlight != null) hunterHeadlight.SetActive(hunter);

        if (ghostVisibility != null) ghostVisibility.enabled = ghost;
        if (ghostAttacker != null) ghostAttacker.enabled = ghost;

        if (role != PlayerRole.None)
            SetLayerRecursive(gameObject, ghost ? ghostLayer : hunterLayer);
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform c in go.transform)
            SetLayerRecursive(c.gameObject, layer);
    }
}
