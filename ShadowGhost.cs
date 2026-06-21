using UnityEngine;

/// <summary>
/// SHADOW — the fastest ghost type, but leaves more visible UV trails.
///
/// ACTIVE ABILITY:  Dead Silence (cooldown: 15 s)
///                  Suppresses footstep audio and interaction reveals for 5 s.
///                  Also lets the ghost pass through Motion Sensors without triggering them.
///
/// PASSIVE:         Movement speed +10% over the base values.
///
/// WEAKNESS:        UV footprints are brighter and persist longer than those
///                  of other ghost types, making the Shadow easier to track visually.
/// </summary>
public class ShadowGhost : GhostBase
{
    // ─────────────────────────────────────────────
    // SHADOW-SPECIFIC SETTINGS
    // ─────────────────────────────────────────────

    [Header("Shadow — Dead Silence")]
    [Tooltip("How long Dead Silence suppresses audio and sensor triggers (seconds)")]
    public float deadSilenceDuration = 5f;

    [Header("Shadow — UV Weakness")]
    [Tooltip("Scale multiplier applied to each UV footprint (>1 = larger / brighter)")]
    public float uvTrailSizeMultiplier = 1.8f;
    [Tooltip("Extra seconds added to the base footprint lifetime")]
    public float uvTrailLifetimeBonus  = 6f;

    // ─────────────────────────────────────────────
    // INTERNAL STATE
    // ─────────────────────────────────────────────

    private bool  isDeadSilenceActive = false;
    private float deadSilenceTimer    = 0f;

    // ─────────────────────────────────────────────
    // ABSTRACT IMPLEMENTATIONS
    // ─────────────────────────────────────────────

    public override float AbilityCooldownDuration => 15f;
    public override GhostWeakness Weakness        => GhostWeakness.EnhancedUVTrails;

    // ─────────────────────────────────────────────
    // INITIALISATION
    // ─────────────────────────────────────────────

    protected override void Start()
    {
        base.Start();

        // Passive speed bonus (+10%)
        walkSpeed   *= 1.1f;
        sprintSpeed *= 1.1f;

        // Weakness: UV footprints last longer
        footprintLifetime += uvTrailLifetimeBonus;

        Debug.Log($"[Shadow] Initialised. Walk speed: {walkSpeed} | UV footprint lifetime: {footprintLifetime}s");
    }

    // ─────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────

    protected override void Update()
    {
        // Tick the Dead Silence timer
        if (isDeadSilenceActive)
        {
            deadSilenceTimer -= Time.deltaTime;
            if (deadSilenceTimer <= 0f)
                DeactivateDeadSilence();
        }

        base.Update();
    }

    // ─────────────────────────────────────────────
    // ACTIVE ABILITY: DEAD SILENCE
    // ─────────────────────────────────────────────

    public override void UseAbility()
    {
        if (!abilityReady)
        {
            Debug.Log($"[Shadow] Dead Silence on cooldown: {abilityCooldown:F1}s remaining.");
            return;
        }

        isDeadSilenceActive = true;
        deadSilenceTimer    = deadSilenceDuration;
        abilityCooldown     = AbilityCooldownDuration;

        // Mute both audio sources for the duration
        if (audioSource  != null) audioSource.mute  = true;
        if (breathSource != null) breathSource.mute = true;

        Debug.Log($"[Shadow] Dead Silence ACTIVATED — footsteps and sensor triggers suppressed for {deadSilenceDuration}s.");
    }

    // ─────────────────────────────────────────────
    // PASSIVE EFFECT
    // ─────────────────────────────────────────────

    protected override void ApplyPassiveEffect()
    {
        // Speed bonus is applied once in Start().
        // This method is intentionally empty but kept for future passive additions
        // (e.g. particle effects, screen distortion for other players).
    }

    // ─────────────────────────────────────────────
    // INTERACTION OVERRIDE — suppressed during Dead Silence
    // ─────────────────────────────────────────────

    /// <summary>
    /// While Dead Silence is active, interacting with objects does NOT reveal the Shadow.
    /// </summary>
    public override void TriggerInteraction()
    {
        if (isDeadSilenceActive)
        {
            Debug.Log("[Shadow] Interaction reveal suppressed by Dead Silence.");
            return;
        }
        base.TriggerInteraction();
    }

    // ─────────────────────────────────────────────
    // FOOTPRINT OVERRIDE — no footprints during Dead Silence
    // ─────────────────────────────────────────────

    protected override void HandleFootprints()
    {
        // No footprints while silent
        if (isDeadSilenceActive) return;

        // Base spawning logic runs normally.
        // uvTrailSizeMultiplier should be applied to the footprint prefab's
        // local scale inside SpawnFootprint (customised per project needs).
        base.HandleFootprints();
    }

    // ─────────────────────────────────────────────
    // PUBLIC QUERY — used by MotionSensorGhost
    // ─────────────────────────────────────────────

    /// <summary>
    /// Returns true while Dead Silence is active.
    /// MotionSensorGhost calls this to decide whether to trigger an alarm.
    /// </summary>
    public bool IsSilent() => isDeadSilenceActive;

    // ─────────────────────────────────────────────
    // INTERNAL
    // ─────────────────────────────────────────────

    void DeactivateDeadSilence()
    {
        isDeadSilenceActive = false;
        if (audioSource  != null) audioSource.mute  = false;
        if (breathSource != null) breathSource.mute = false;
        Debug.Log("[Shadow] Dead Silence expired.");
    }
}
