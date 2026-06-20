using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Vizibilitatea networked pentru un jucator cu rol Ghost.
/// - Jucatorul-fantoma se vede mereu pe sine (ca sa navigheze).
/// - Ceilalti (hunters) il vad DOAR cat timp e "revealed" (lovit de lanterna UV).
/// Reveal-ul este autoritate de server: hunterul cere prin ServerRpc, serverul
/// reseteaza un timer si seteaza NetworkVariable Revealed, sincronizat la toti.
///
/// Inlocuieste GhostController (care era pentru o fantoma AI standalone) cand
/// fantoma este un jucator. Activat de PlayerRoleController doar pentru rolul Ghost.
/// </summary>
public class PlayerGhostVisibility : NetworkBehaviour
{
    [Header("Setari Transparenta")]
    [Range(0f, 1f)] public float revealAlpha = 0.3f;   // cat de vizibila pt hunters cand e luminata
    [Range(0f, 1f)] public float ownerAlpha = 0.6f;    // cat se vede fantoma pe sine
    public float fadeDelay = 0.2f;                      // cat ramane vizibila dupa ultimul UV
    public float fadeSpeed = 10f;

    // Sincronizat: e luminata acum? Scris doar de server.
    public NetworkVariable<bool> Revealed = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Sincronizat: fantoma a fost prinsa de un hunter? Scris doar de server.
    public NetworkVariable<bool> Caught = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Renderer[] ghostRenderers;
    private Material[] uniqueMaterials;
    private float serverRevealTimer = 0f;

    public override void OnNetworkSpawn()
    {
        ghostRenderers = GetComponentsInChildren<Renderer>(true);
        uniqueMaterials = new Material[ghostRenderers.Length];
        for (int i = 0; i < ghostRenderers.Length; i++)
            uniqueMaterials[i] = ghostRenderers[i].material;
    }

    /// <summary>
    /// Apelat de UVFlashlight (de pe orice client) cand raza loveste aceasta fantoma.
    /// RequireOwnership=false: hunterul nu detine obiectul fantomei.
    /// </summary>
    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void RevealServerRpc()
    {
        serverRevealTimer = fadeDelay;
        if (!Revealed.Value) Revealed.Value = true;
    }

    void Update()
    {
        // Serverul gestioneaza expirarea reveal-ului.
        if (IsServer && Revealed.Value)
        {
            serverRevealTimer -= Time.deltaTime;
            if (serverRevealTimer <= 0f)
                Revealed.Value = false;
        }

        // Toti clientii deseneaza in functie de cine sunt si de starea Revealed.
        // Cand e prinsa, devine vizibila tuturor (game over pt fantoma).
        bool visibleToMe = IsOwner || Revealed.Value || Caught.Value;
        float targetAlpha = (IsOwner || Caught.Value) ? ownerAlpha : revealAlpha;

        if (ghostRenderers == null) return;

        for (int i = 0; i < ghostRenderers.Length; i++)
        {
            if (ghostRenderers[i] == null) continue;

            if (!visibleToMe)
            {
                ghostRenderers[i].enabled = false;
                continue;
            }

            ghostRenderers[i].enabled = true;
            FadeAlpha(uniqueMaterials[i], targetAlpha);
        }
    }

    void FadeAlpha(Material mat, float alphaTarget)
    {
        if (mat != null && mat.HasProperty("_BaseColor"))
        {
            Color c = mat.GetColor("_BaseColor");
            c.a = Mathf.Lerp(c.a, alphaTarget, Time.deltaTime * fadeSpeed);
            mat.SetColor("_BaseColor", c);
        }
    }
}
