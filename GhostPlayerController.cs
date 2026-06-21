using UnityEngine;

/// <summary>
/// Input controller for the ghost player.
/// Handles movement, sprinting, camera look, and ability input.
/// Attach this to the same GameObject as one of the GhostBase subclasses
/// (ShadowGhost, PoltergeistGhost, or WraithGhost).
///
/// Key bindings:
///   WASD / Arrow Keys  — movement
///   Left Shift         — sprint (triggers brief visibility shimmer)
///   Mouse              — camera rotation
///   Q                  — active ability
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class GhostPlayerController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SETTINGS
    // ─────────────────────────────────────────────

    [Header("Camera")]
    public Transform cameraTransform;
    public float mouseSensitivity = 120f;

    [Header("Gravity")]
    public float gravity = -9.81f;

    // ─────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────

    private GhostBase           ghost;
    private CharacterController cc;

    private float   xRotation   = 0f;
    private Vector3 velocity    = Vector3.zero;
    private bool    wasSprinting = false;

    // ─────────────────────────────────────────────
    // INITIALISATION
    // ─────────────────────────────────────────────

    void Awake()
    {
        ghost = GetComponent<GhostBase>();
        cc    = GetComponent<CharacterController>();

        if (ghost == null)
            Debug.LogError("[GhostPlayerController] No GhostBase script found on this GameObject!");

        Cursor.lockState = CursorLockMode.Locked;
    }

    // ─────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleAbilityInput();
    }

    // ─────────────────────────────────────────────
    // MOUSE LOOK
    // ─────────────────────────────────────────────

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Vertical look (clamped to avoid over-rotation)
        xRotation -= mouseY;
        xRotation  = Mathf.Clamp(xRotation, -80f, 80f);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal rotation applied to the whole body
        transform.Rotate(Vector3.up * mouseX);
    }

    // ─────────────────────────────────────────────
    // MOVEMENT
    // ─────────────────────────────────────────────

    void HandleMovement()
    {
        if (ghost == null) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && (h != 0 || v != 0);

        // Notify GhostBase about sprint state changes (triggers shimmer on start)
        if (isSprinting && !wasSprinting)
            ghost.StartSprint();
        else if (!isSprinting && wasSprinting)
            ghost.StopSprint();
        wasSprinting = isSprinting;

        float speed = isSprinting ? ghost.sprintSpeed : ghost.walkSpeed;

        // Build movement direction relative to where the ghost is facing
        Vector3 direction = transform.right * h + transform.forward * v;
        if (direction.magnitude > 1f) direction.Normalize();

        // Simple gravity
        if (cc.isGrounded && velocity.y < 0f)
            velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;

        cc.Move((direction * speed + velocity) * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    // ABILITY INPUT
    // ─────────────────────────────────────────────

    void HandleAbilityInput()
    {
        if (ghost == null) return;

        // Q activates the ghost's unique ability
        if (Input.GetKeyDown(KeyCode.Q))
            ghost.UseAbility();
    }
}
