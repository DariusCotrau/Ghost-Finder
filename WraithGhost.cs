using UnityEngine;
using System.Collections;

/// <summary>
/// WRAITH — the traversal ghost. Can phase through walls and closed doors,
/// but emits a metallic echo that betrays it at close range.
///
/// ACTIVE ABILITY:  Phase (cooldown: 25 s)
///                  Teleports through solid geometry in the forward direction.
///
/// PASSIVE:         Leaves NO UV footprints on the floor — the hardest ghost to track visually.
///
/// WEAKNESS:        Emits a metallic echo audible within 4 m of any hunter, even while stationary.
/// </summary>
public class WraithGhost : GhostBase
{
    // ─────────────────────────────────────────────
    // WRAITH-SPECIFIC SETTINGS
    // ─────────────────────────────────────────────

    [Header("Wraith — Phase")]
    [Tooltip("Maximum distance of the phase teleport (metres)")]
    public float phaseDistance    = 4f;
    [Tooltip("Duration of the phase travel animation (seconds)")]
    public float phaseDuration    = 0.4f;
    [Tooltip("Visual effect spawned at the start and end of a phase")]
    public GameObject phaseEffectPrefab;

    [Header("Wraith — Metallic Echo (Weakness)")]
    [Tooltip("Distance within which the metallic echo is audible (metres)")]
    public float echoRange        = 4f;
    [Tooltip("Audio clip for the metallic echo")]
    public AudioClip metallicEchoClip;
    [Tooltip("Interval between echo pulses (seconds)")]
    public float echoInterval     = 2.5f;

    // ─────────────────────────────────────────────
    // INTERNAL STATE
    // ─────────────────────────────────────────────

    private bool        isPhasing  = false;
    private float       echoTimer  = 0f;
    private AudioSource echoSource;

    // ─────────────────────────────────────────────
    // ABSTRACT IMPLEMENTATIONS
    // ─────────────────────────────────────────────

    public override float AbilityCooldownDuration => 25f;
    public override GhostWeakness Weakness        => GhostWeakness.MetallicEcho;

    // ─────────────────────────────────────────────
    // INITIALISATION
    // ─────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();

        // Third dedicated AudioSource for the metallic echo
        echoSource              = gameObject.AddComponent<AudioSource>();
        echoSource.spatialBlend = 1f;   // full 3D audio
        echoSource.playOnAwake  = false;
        echoSource.loop         = false;
        echoSource.volume       = 0.9f;
    }

    // ─────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────

    protected override void Update()
    {
        base.Update();
        HandleMetallicEcho();
    }

    // ─────────────────────────────────────────────
    // ACTIVE ABILITY: PHASE
    // ─────────────────────────────────────────────

    public override void UseAbility()
    {
        if (!abilityReady)
        {
            Debug.Log($"[Wraith] Phase on cooldown: {abilityCooldown:F1}s remaining.");
            return;
        }
        if (isPhasing)
        {
            Debug.Log("[Wraith] Phase already in progress.");
            return;
        }

        StartCoroutine(PhaseCoroutine());
    }

    IEnumerator PhaseCoroutine()
    {
        isPhasing       = true;
        abilityCooldown = AbilityCooldownDuration;

        Debug.Log("[Wraith] PHASE ACTIVATED — phasing through geometry.");

        // 1. Spawn entry visual effect
        if (phaseEffectPrefab != null)
            Instantiate(phaseEffectPrefab, transform.position, Quaternion.identity);

        // 2. Disable the CharacterController so we can move through colliders
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 3. Lerp to the target position (straight through any geometry)
        Vector3 startPos = transform.position;
        Vector3 target   = transform.position + transform.forward * phaseDistance;
        float   elapsed  = 0f;

        while (elapsed < phaseDuration)
        {
            transform.position = Vector3.Lerp(startPos, target, elapsed / phaseDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = target;

        // 4. Re-enable the CharacterController
        if (cc != null) cc.enabled = true;

        // 5. Spawn exit visual effect
        if (phaseEffectPrefab != null)
            Instantiate(phaseEffectPrefab, transform.position, Quaternion.identity);

        isPhasing = false;
        Debug.Log("[Wraith] Phase complete.");
    }

    // ─────────────────────────────────────────────
    // PASSIVE: NO UV FOOTPRINTS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Wraith never leaves UV footprints — this override is intentionally empty.
    /// </summary>
    protected override void HandleFootprints()
    {
        // Intentionally empty. The Wraith produces no UV trail (passive trait).
    }

    protected override void ApplyPassiveEffect()
    {
        // Passive is handled by the empty HandleFootprints above.
        // Nothing else needed here.
    }

    // ─────────────────────────────────────────────
    // WEAKNESS: METALLIC ECHO
    // ─────────────────────────────────────────────

    void HandleMetallicEcho()
    {
        if (metallicEchoClip == null) return;

        echoTimer -= Time.deltaTime;
        if (echoTimer > 0f) return;

        // Reset the timer regardless — we always tick, even if no player is near
        echoTimer = echoInterval;

        float distToPlayer = GetDistanceToNearestPlayer();
        if (distToPlayer <= echoRange)
        {
            echoSource.PlayOneShot(metallicEchoClip);
            Debug.Log($"[Wraith] Metallic echo emitted — player at {distToPlayer:F1}m.");
        }
    }

    // ─────────────────────────────────────────────
    // PUBLIC QUERY — used by MotionSensorGhost
    // ─────────────────────────────────────────────

    /// <summary>
    /// Returns true while the Wraith is actively phasing through geometry.
    /// MotionSensorGhost uses this to skip the alarm trigger during a phase.
    /// </summary>
    public bool IsPhasing() => isPhasing;
}
