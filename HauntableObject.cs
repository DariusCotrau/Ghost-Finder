using UnityEngine;

/// <summary>
/// Component placed on any prop in the scene that can be activated by the Poltergeist
/// (books, chairs, paintings, objects on tables, etc.).
///
/// Implements the IHauntable interface defined in PoltergeistGhost.cs.
/// Requires a Rigidbody on the same GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class HauntableObject : MonoBehaviour, IHauntable
{
    [Header("Haunt Settings")]
    [Tooltip("Direction the object is thrown in local space. Default: upward and slightly forward.")]
    public Vector3 throwDirection = new Vector3(0f, 1f, 0.5f);
    [Tooltip("Sound played when the object is activated by the ghost")]
    public AudioClip hauntSound;
    [Tooltip("Delay before the object resets to its original position (0 = never resets)")]
    public float resetDelay = 5f;

    private Rigidbody   rb;
    private AudioSource audioSource;
    private Vector3     originalPosition;
    private Quaternion  originalRotation;
    private bool        isHaunted = false;

    void Awake()
    {
        rb               = GetComponent<Rigidbody>();
        audioSource      = GetComponent<AudioSource>();
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    // ─────────────────────────────────────────────
    // IHauntable INTERFACE
    // ─────────────────────────────────────────────

    /// <summary>
    /// Called by the Poltergeist (during Haunt or passive interaction).
    /// Throws the object with the given force and plays a sound.
    /// </summary>
    public void ActivateByGhost(float force)
    {
        if (isHaunted) return; // prevent double-activation while already in motion

        isHaunted = true;

        // Convert local throw direction to world space and apply impulse force
        Vector3 worldDir = transform.TransformDirection(throwDirection.normalized);
        rb.AddForce(worldDir * force, ForceMode.Impulse);

        // Play the haunting sound
        if (audioSource != null && hauntSound != null)
            audioSource.PlayOneShot(hauntSound);

        Debug.Log($"[HauntableObject] {gameObject.name} activated with force {force}.");

        // Schedule a reset if configured
        if (resetDelay > 0f)
            Invoke(nameof(ResetObject), resetDelay);
    }

    // ─────────────────────────────────────────────
    // RESET
    // ─────────────────────────────────────────────

    void ResetObject()
    {
        // Stop all physics motion before teleporting back
        rb.linearVelocity  = Vector3.zero; // Unity 6 API — use rb.velocity for older versions
        rb.angularVelocity = Vector3.zero;

        transform.position = originalPosition;
        transform.rotation = originalRotation;

        isHaunted = false;
    }
}
