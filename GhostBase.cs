using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Abstract base class for all ghosts in Ghost Finder.
/// Handles: default invisibility, forced visibility, UV footprints, footstep/breath audio.
/// Each ghost type (Shadow, Poltergeist, Wraith) extends this class.
/// </summary>
public abstract class GhostBase : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // GENERAL SETTINGS
    // ─────────────────────────────────────────────

    [Header("Movement")]
    public float walkSpeed = 3.5f;
    public float sprintSpeed = 6f;
    protected bool isSprinting = false;

    [Header("Visibility")]
    [Tooltip("How long the ghost stays visible after a trigger (seconds)")]
    public float visibilityDuration = 1.5f;
    [Tooltip("Speed of the fade-in/out on all materials")]
    public float fadeSpeed = 8f;
    [Tooltip("Maximum alpha when visible (0 = fully invisible, 1 = fully opaque)")]
    [Range(0f, 1f)]
    public float visibleAlpha = 0.85f;

    [Header("UV Footprints")]
    [Tooltip("Prefab for the UV footprint (simple object with a UV-reactive material)")]
    public GameObject uvFootprintPrefab;
    [Tooltip("How long footprints stay on the floor (seconds)")]
    public float footprintLifetime = 6f;
    [Tooltip("Minimum distance between two consecutive footprints")]
    public float footprintSpacing = 0.6f;

    [Header("Passive Audio")]
    [Tooltip("Footstep clips (chosen at random)")]
    public AudioClip[] footstepClips;
    [Tooltip("Ambient breath / static clip")]
    public AudioClip ambientBreathClip;
    [Tooltip("Maximum footstep volume (scales with distance to Hunter)")]
    public float maxFootstepVolume = 0.6f;
    [Tooltip("Maximum breath volume")]
    public float maxBreathVolume = 0.4f;
    [Tooltip("Radius within which hunters can hear footsteps (metres)")]
    public float footstepAudibleRange = 8f;
    [Tooltip("Radius within which hunters can hear the breath")]
    public float breathAudibleRange = 6f;

    // ─────────────────────────────────────────────
    // INTERNAL COMPONENTS
    // ─────────────────────────────────────────────

    protected Renderer[]          ghostRenderers;
    protected Material[]          ghostMaterials;
    protected AudioSource         audioSource;   // footsteps
    protected AudioSource         breathSource;  // ambient breath
    protected CharacterController characterController;

    // ─────────────────────────────────────────────
    // INTERNAL STATE
    // ─────────────────────────────────────────────

    protected float   visibilityTimer  = 0f;
    protected bool    isVisible        = false;
    protected float   abilityCooldown  = 0f;
    protected bool    abilityReady     => abilityCooldown <= 0f;

    private   Vector3 lastFootprintPos = Vector3.zero;
    private   float   footstepTimer    = 0f;
    private   float   footstepInterval = 0.45f; // seconds between footstep sounds

    // ─────────────────────────────────────────────
    // ABSTRACT MEMBERS — each subclass must implement these
    // ─────────────────────────────────────────────

    /// <summary>Cooldown of the active ability (seconds).</summary>
    public abstract float AbilityCooldownDuration { get; }

    /// <summary>Logic for the ghost's active ability.</summary>
    public abstract void UseAbility();

    /// <summary>Passive effect specific to this ghost type (called every Update).</summary>
    protected abstract void ApplyPassiveEffect();

    /// <summary>The ghost's weakness, used by hunter gadgets.</summary>
    public abstract GhostWeakness Weakness { get; }

    // ─────────────────────────────────────────────
    // INITIALISATION
    // ─────────────────────────────────────────────

    protected virtual void Awake()
    {
        // Collect all renderers and create unique material instances
        ghostRenderers = GetComponentsInChildren<Renderer>();
        ghostMaterials = new Material[ghostRenderers.Length];
        for (int i = 0; i < ghostRenderers.Length; i++)
            ghostMaterials[i] = ghostRenderers[i].material;

        characterController = GetComponent<CharacterController>();

        // Assign audio sources — expect two AudioSource components on the GameObject
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length >= 2)
        {
            audioSource  = sources[0]; // footsteps
            breathSource = sources[1]; // breath / ambient
        }
        else if (sources.Length == 1)
        {
            audioSource = sources[0];
        }

        // Start the breath loop silently; volume is driven dynamically
        if (breathSource != null && ambientBreathClip != null)
        {
            breathSource.clip         = ambientBreathClip;
            breathSource.loop         = true;
            breathSource.spatialBlend = 1f; // full 3D audio
            breathSource.volume       = 0f;
            breathSource.Play();
        }
    }

    protected virtual void Start()
    {
        SetAllAlpha(0f);           // start fully invisible
        SetRenderersEnabled(false);
    }

    // ─────────────────────────────────────────────
    // MAIN UPDATE
    // ─────────────────────────────────────────────

    protected virtual void Update()
    {
        // Tick ability cooldown
        if (abilityCooldown > 0f)
            abilityCooldown -= Time.deltaTime;

        // Visibility timer — fade in while active, fade out when expired
        if (visibilityTimer > 0f)
        {
            visibilityTimer -= Time.deltaTime;
            FadeToAlpha(visibleAlpha);
            if (!isVisible) ShowGhost();
        }
        else
        {
            FadeToAlpha(0f);
            if (isVisible && GetCurrentAlpha() < 0.01f)
                HideGhost();
        }

        // UV footprints (Wraith overrides this to produce none)
        HandleFootprints();

        // Footstep audio
        HandleFootstepAudio();

        // Subtype passive effect
        ApplyPassiveEffect();
    }

    // ─────────────────────────────────────────────
    // VISIBILITY
    // ─────────────────────────────────────────────

    /// <summary>
    /// Forces the ghost to become visible for <duration> seconds.
    /// Called by: Sprint, UseAbility, TriggerInteraction.
    /// </summary>
    public void ForceVisible(float duration = -1f)
    {
        float d = duration > 0f ? duration : visibilityDuration;
        visibilityTimer = Mathf.Max(visibilityTimer, d);
    }

    void ShowGhost()
    {
        SetRenderersEnabled(true);
        isVisible = true;
    }

    void HideGhost()
    {
        SetRenderersEnabled(false);
        isVisible = false;
    }

    void FadeToAlpha(float target)
    {
        foreach (var mat in ghostMaterials)
        {
            if (!mat.HasProperty("_BaseColor")) continue;
            Color c = mat.GetColor("_BaseColor");
            c.a = Mathf.MoveTowards(c.a, target, fadeSpeed * Time.deltaTime);
            mat.SetColor("_BaseColor", c);
        }
    }

    float GetCurrentAlpha()
    {
        if (ghostMaterials.Length == 0) return 0f;
        var mat = ghostMaterials[0];
        return mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor").a : 0f;
    }

    void SetAllAlpha(float a)
    {
        foreach (var mat in ghostMaterials)
        {
            if (!mat.HasProperty("_BaseColor")) continue;
            Color c = mat.GetColor("_BaseColor");
            c.a = a;
            mat.SetColor("_BaseColor", c);
        }
    }

    void SetRenderersEnabled(bool enabled)
    {
        foreach (var r in ghostRenderers)
            if (r != null) r.enabled = enabled;
    }

    // ─────────────────────────────────────────────
    // UV FOOTPRINTS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Spawns UV footprints on the floor while the ghost moves.
    /// Wraith overrides this method to produce no footprints.
    /// </summary>
    protected virtual void HandleFootprints()
    {
        if (uvFootprintPrefab == null) return;
        if (!IsMoving()) return;

        float dist = Vector3.Distance(transform.position, lastFootprintPos);
        if (dist >= footprintSpacing)
        {
            SpawnFootprint();
            lastFootprintPos = transform.position;
        }
    }

    void SpawnFootprint()
    {
        // Raycast downward to place the footprint on the floor surface
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 1.5f))
        {
            GameObject fp = Instantiate(
                uvFootprintPrefab,
                hit.point + Vector3.up * 0.01f,
                Quaternion.LookRotation(transform.forward));
            Destroy(fp, footprintLifetime);
        }
    }

    // ─────────────────────────────────────────────
    // FOOTSTEP AUDIO
    // ─────────────────────────────────────────────

    void HandleFootstepAudio()
    {
        if (!IsMoving()) return;

        float interval = isSprinting ? footstepInterval * 0.6f : footstepInterval;
        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            PlayFootstep();
            footstepTimer = interval;
        }

        UpdateBreathVolume();
    }

    void PlayFootstep()
    {
        if (audioSource == null || footstepClips == null || footstepClips.Length == 0) return;
        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        float vol = isSprinting ? maxFootstepVolume : maxFootstepVolume * 0.6f;
        audioSource.PlayOneShot(clip, vol);
    }

    void UpdateBreathVolume()
    {
        if (breathSource == null) return;

        float minDist = GetDistanceToNearestPlayer();
        if (minDist <= breathAudibleRange)
        {
            float t = 1f - (minDist / breathAudibleRange);
            breathSource.volume = Mathf.Lerp(0f, maxBreathVolume, t);
        }
        else
        {
            breathSource.volume = Mathf.MoveTowards(breathSource.volume, 0f, Time.deltaTime * 2f);
        }
    }

    // ─────────────────────────────────────────────
    // INTERACTION WITH THE MAP
    // ─────────────────────────────────────────────

    /// <summary>
    /// Called when the ghost opens a door or knocks an object.
    /// Makes the ghost briefly visible (Poltergeist overrides this).
    /// </summary>
    public virtual void TriggerInteraction()
    {
        ForceVisible(visibilityDuration);
    }

    // ─────────────────────────────────────────────
    // SPRINT
    // ─────────────────────────────────────────────

    /// <summary>
    /// Activates sprinting. Sprinting causes a brief visibility shimmer
    /// and increases footstep volume.
    /// </summary>
    public void StartSprint()
    {
        isSprinting = true;
        ForceVisible(0.3f); // brief shimmer while sprinting
    }

    public void StopSprint()
    {
        isSprinting = false;
    }

    // ─────────────────────────────────────────────
    // ABILITY BLOCKING — called by the Crucifix gadget
    // ─────────────────────────────────────────────

    /// <summary>
    /// Blocks the next active ability use by resetting the cooldown as a penalty.
    /// </summary>
    public void BlockAbility()
    {
        abilityCooldown = AbilityCooldownDuration;
        Debug.Log($"[{GetType().Name}] Ability blocked by Crucifix!");
    }

    // ─────────────────────────────────────────────
    // UTILITIES
    // ─────────────────────────────────────────────

    protected bool IsMoving()
    {
        if (characterController != null)
            return characterController.velocity.magnitude > 0.1f;
        return false;
    }

    /// <summary>
    /// Returns the distance to the nearest Player in the scene.
    /// Uses the "Player" tag — consistent with the existing project scripts.
    /// </summary>
    protected float GetDistanceToNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float minDist = float.MaxValue;
        foreach (var p in players)
        {
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < minDist) minDist = d;
        }
        return minDist == float.MaxValue ? 999f : minDist;
    }
}

/// <summary>Possible ghost weaknesses, used by hunter gadgets.</summary>
public enum GhostWeakness
{
    EnhancedUVTrails,  // Shadow:       UV footprints are larger and last longer
    DoubleEMFRange,    // Poltergeist:  EMF Reader detects it at double the normal range
    MetallicEcho       // Wraith:       emits a metallic echo audible within 4 m even when still
}
