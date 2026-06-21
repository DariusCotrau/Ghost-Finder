using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Pune-l pe Player Prefab. Asculta LobbyPlayer.Role si activeaza echipamentul
/// potrivit pentru fiecare rol, pe TOTI clientii (vizualul trebuie sa fie corect
/// si pentru ceilalti jucatori, nu doar local).
///
/// Hunter -> lanterna UV + headlight, vizibil normal.
/// Ghost  -> PlayerGhostVisibility (invizibil pt hunters), fara echipament UV.
/// </summary>
[RequireComponent(typeof(LobbyPlayer))]
public class PlayerRoleController : NetworkBehaviour
{
    [Header("Echipament Hunter")]
    public UVFlashlight uvFlashlight;
    public LanternController headlight;
    public HunterCatcher catcher;
    public GameObject hunterVisual;   // model/optional

    [Header("Ghost")]
    public PlayerGhostVisibility ghostVisibility;
    public GhostAttacker ghostAttacker;
    public GameObject ghostVisual;    // model/optional

    private LobbyPlayer lobbyPlayer;

    public override void OnNetworkSpawn()
    {
        lobbyPlayer = GetComponent<LobbyPlayer>();
        lobbyPlayer.Role.OnValueChanged += OnRoleChanged;

        // Aplicam starea curenta (acopera join tarziu / rol deja setat).
        ApplyRole(lobbyPlayer.Role.Value);
    }

    public override void OnNetworkDespawn()
    {
        if (lobbyPlayer != null)
            lobbyPlayer.Role.OnValueChanged -= OnRoleChanged;
    }

    private void OnRoleChanged(PlayerRole oldRole, PlayerRole newRole) => ApplyRole(newRole);

    private void ApplyRole(PlayerRole role)
    {
        bool isHunter = role == PlayerRole.Hunter;
        bool isGhost = role == PlayerRole.Ghost;

        // Echipament Hunter
        if (uvFlashlight != null) uvFlashlight.enabled = isHunter;
        if (uvFlashlight != null && uvFlashlight.uvLightObject != null && !isHunter)
            uvFlashlight.uvLightObject.SetActive(false);
        if (headlight != null) headlight.enabled = isHunter;
        if (catcher != null) catcher.enabled = isHunter;
        if (hunterVisual != null) hunterVisual.SetActive(isHunter);

        // Ghost
        if (ghostVisibility != null) ghostVisibility.enabled = isGhost;
        if (ghostAttacker != null) ghostAttacker.enabled = isGhost;
        if (ghostVisual != null) ghostVisual.SetActive(isGhost);

        if (role != PlayerRole.None)
            Debug.Log($"[ROLE] {name} configurat ca {role}");
    }
}
