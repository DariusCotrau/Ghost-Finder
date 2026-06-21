using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// POLTERGEIST — the chaos and distraction ghost.
/// Activates objects at a distance and interacts with the environment
/// WITHOUT becoming visible (passive), unlike other ghost types.
///
/// ACTIVE ABILITY:  Haunt (cooldown: 20 s)
///                  Simultaneously activates 3-4 random interactable objects
///                  within range (doors, throwable props, etc.).
///
/// PASSIVE:         Interacts with nearby objects without triggering a visibility reveal.
///
/// WEAKNESS:        The EMF Reader detects the Poltergeist at double its normal range.
/// </summary>
public class PoltergeistGhost : GhostBase
{
    // ─────────────────────────────────────────────
    // POLTERGEIST-SPECIFIC SETTINGS
    // ─────────────────────────────────────────────

    [Header("Poltergeist — Haunt")]
    [Tooltip("Minimum number of objects activated simultaneously during Haunt")]
    public int   hauntObjectCountMin  = 3;
    [Tooltip("Maximum number of objects activated simultaneously during Haunt")]
    public int   hauntObjectCountMax  = 4;
    [Tooltip("Search radius for interactable objects during Haunt (metres)")]
    public float hauntSearchRadius    = 15f;
    [Tooltip("Force applied to thrown objects during Haunt")]
    public float hauntThrowForce      = 5f;
    [Tooltip("Duration of the Haunt visual effect (seconds)")]
    public float hauntEffectDuration  = 2.5f;

    [Header("Poltergeist — Passive Interaction")]
    [Tooltip("Minimum interval between passive object interactions (seconds)")]
    public float passiveInteractionCooldown = 8f;

    // ─────────────────────────────────────────────
    // INTERNAL STATE
    // ─────────────────────────────────────────────

    private float passiveInteractionTimer = 0f;
    private bool  hauntActive             = false;

    // ─────────────────────────────────────────────
    // ABSTRACT IMPLEMENTATIONS
    // ─────────────────────────────────────────────

    public override float AbilityCooldownDuration => 20f;
    public override GhostWeakness Weakness        => GhostWeakness.DoubleEMFRange;

    // ─────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────

    protected override void Update()
    {
        base.Update();

        if (passiveInteractionTimer > 0f)
            passiveInteractionTimer -= Time.deltaTime;
    }

    // ─────────────────────────────────────────────
    // ACTIVE ABILITY: HAUNT
    // ─────────────────────────────────────────────

    public override void UseAbility()
    {
        if (!abilityReady)
        {
            Debug.Log($"[Poltergeist] Haunt on cooldown: {abilityCooldown:F1}s remaining.");
            return;
        }
        if (hauntActive)
        {
            Debug.Log("[Poltergeist] Haunt is already in progress.");
            return;
        }

        abilityCooldown = AbilityCooldownDuration;
        StartCoroutine(HauntCoroutine());
    }

    IEnumerator HauntCoroutine()
    {
        hauntActive = true;
        Debug.Log("[Poltergeist] HAUNT ACTIVATED!");

        // Find all hauntable objects within range
        List<IHauntable> candidates = FindHauntableObjects();

        if (candidates.Count == 0)
        {
            Debug.Log("[Poltergeist] Haunt: no interactable objects found within range.");
            hauntActive = false;
            yield break;
        }

        // Shuffle the list and pick 3-4 targets
        ShuffleList(candidates);
        int count = Mathf.Min(Random.Range(hauntObjectCountMin, hauntObjectCountMax + 1), candidates.Count);

        for (int i = 0; i < count; i++)
        {
            candidates[i].ActivateByGhost(hauntThrowForce);
            // Small delay between each object to create a wave-of-chaos feeling
            yield return new WaitForSeconds(0.15f);
        }

        Debug.Log($"[Poltergeist] Haunt: {count} objects activated.");
        yield return new WaitForSeconds(hauntEffectDuration);
        hauntActive = false;
    }

    // ─────────────────────────────────────────────
    // PASSIVE EFFECT: Interact without revealing
    // ─────────────────────────────────────────────

    protected override void ApplyPassiveEffect()
    {
        if (passiveInteractionTimer > 0f) return;

        // Occasionally nudge a nearby object silently (no visibility reveal)
        IHauntable nearby = FindNearbyHauntable(3f);
        if (nearby != null)
        {
            nearby.ActivateByGhost(1.5f); // subtle force — just enough to notice
            passiveInteractionTimer = passiveInteractionCooldown;
            Debug.Log("[Poltergeist] Silent passive interaction with a nearby object.");
        }
    }

    // ─────────────────────────────────────────────
    // INTERACTION OVERRIDE — Poltergeist never reveals on interaction
    // ─────────────────────────────────────────────

    /// <summary>
    /// The Poltergeist interacts with objects WITHOUT becoming visible.
    /// This is its defining passive trait — we intentionally skip base.TriggerInteraction().
    /// </summary>
    public override void TriggerInteraction()
    {
        // Do NOT call base.TriggerInteraction() — Poltergeist stays invisible
        Debug.Log("[Poltergeist] Object interaction — NO visibility reveal (passive active).");
        passiveInteractionTimer = passiveInteractionCooldown * 0.5f;
    }

    // ─────────────────────────────────────────────
    // PUBLIC QUERY — used by EMFReader
    // ─────────────────────────────────────────────

    /// <summary>
    /// The EMFReader calls this to know it should double its detection range.
    /// </summary>
    public bool IsPoltergeist() => true;

    // ─────────────────────────────────────────────
    // UTILITIES
    // ─────────────────────────────────────────────

    List<IHauntable> FindHauntableObjects()
    {
        List<IHauntable> result = new List<IHauntable>();
        Collider[] cols = Physics.OverlapSphere(transform.position, hauntSearchRadius);
        foreach (var col in cols)
        {
            IHauntable h = col.GetComponent<IHauntable>();
            if (h != null) result.Add(h);
        }
        return result;
    }

    IHauntable FindNearbyHauntable(float radius)
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, radius);
        foreach (var col in cols)
        {
            IHauntable h = col.GetComponent<IHauntable>();
            if (h != null) return h;
        }
        return null;
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j   = Random.Range(0, i + 1);
            T   tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }
}

/// <summary>
/// Interface implemented by any interactable prop in the scene
/// (doors, books, chairs, etc.) that the Poltergeist can activate.
/// Add HauntableObject.cs to a prop to make it implement this interface automatically.
/// </summary>
public interface IHauntable
{
    void ActivateByGhost(float force);
}
