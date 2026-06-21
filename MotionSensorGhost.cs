using UnityEngine;

/// <summary>
/// Extended version of MotionSensor.cs that is aware of ghost mechanics.
///
/// Differences from the original MotionSensor.cs:
///   — Shadow with Dead Silence active passes through WITHOUT triggering the alarm.
///   — Wraith while phasing passes through WITHOUT triggering the alarm.
///   — Poltergeist triggers normally (no special case).
///   — Uses the "Player" tag (consistent with the existing MovementScript / MotionSensor).
///
/// USAGE: Replace the MotionSensor component on placed sensor prefabs with this script,
/// OR use this on a new sensor prefab variant assigned to the ghost-compatible slot.
/// The original MotionSensor.cs does not need to be modified.
/// </summary>
public class MotionSensorGhost : MonoBehaviour
{
    [Header("Beep Settings")]
    [Tooltip("Seconds between each beep when something is inside the trigger zone")]
    public float beepInterval = 1f;

    private AudioSource audioSource;
    private float       beepTimer    = 0f;
    private bool        isTriggered  = false;

    // ─────────────────────────────────────────────
    // INITIALISATION
    // ─────────────────────────────────────────────

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        beepTimer   = beepInterval;
    }

    // ─────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────

    void Update()
    {
        if (!isTriggered) return;

        beepTimer -= Time.deltaTime;
        if (beepTimer <= 0f)
        {
            PlayBeep();
            beepTimer = beepInterval;
        }
    }

    // ─────────────────────────────────────────────
    // TRIGGER — ENTER
    // ─────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        // Regular players always trigger the sensor
        if (other.CompareTag("Player"))
        {
            isTriggered = true;
            beepTimer   = 0f; // beep immediately on entry
            Debug.Log("[SENSOR] Player detected!");
            return;
        }

        // Check if it is a ghost
        GhostBase ghost = other.GetComponent<GhostBase>()
                       ?? other.GetComponentInParent<GhostBase>();

        if (ghost == null) return;

        // Shadow with Dead Silence active — passes through silently
        if (ghost is ShadowGhost shadow && shadow.IsSilent())
        {
            Debug.Log("[SENSOR] Shadow with Dead Silence — ignored.");
            return;
        }

        // Wraith while phasing — passes through the sensor without triggering it
        if (ghost is WraithGhost wraith && wraith.IsPhasing())
        {
            Debug.Log("[SENSOR] Wraith phasing — ignored.");
            return;
        }

        // All other ghost types (including Poltergeist) trigger normally
        isTriggered = true;
        beepTimer   = 0f;
        Debug.Log($"[SENSOR] Ghost detected: {ghost.GetType().Name}!");
    }

    // ─────────────────────────────────────────────
    // TRIGGER — EXIT
    // ─────────────────────────────────────────────

    void OnTriggerExit(Collider other)
    {
        // Stop beeping when the player or ghost leaves the zone
        bool isPlayer = other.CompareTag("Player");
        bool isGhost  = other.GetComponent<GhostBase>() != null
                     || other.GetComponentInParent<GhostBase>() != null;

        if (isPlayer || isGhost)
        {
            isTriggered = false;
            Debug.Log("[SENSOR] Zone is clear.");
        }
    }

    // ─────────────────────────────────────────────
    // AUDIO
    // ─────────────────────────────────────────────

    void PlayBeep()
    {
        if (audioSource != null && audioSource.clip != null)
            audioSource.Play();
    }
}
