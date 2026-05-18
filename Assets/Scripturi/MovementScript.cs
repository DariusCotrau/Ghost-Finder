using UnityEngine;

public class MovementScript : MonoBehaviour
{
    public float viteza = 5f;
    public float fortaSaritura = 5f; // Puterea săriturii
    
    private Rigidbody rb;
    private Vector2 movement;
    private bool vreaSaSara = false;
    private bool estePePamant = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Împiedică jucătorul să se rotească aiurea la coliziuni
    }

    void Update()
    {
        // Input mișcare
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Verificăm dacă apasă Space în Update (pentru a nu rata input-ul)
        if (Input.GetButtonDown("Jump") && estePePamant)
        {
            vreaSaSara = true;
        }
    }

    void FixedUpdate()
    {
        // Mișcare orizontală
        Vector3 moveDirection = transform.right * movement.x + transform.forward * movement.y;
        if (moveDirection.magnitude > 1) moveDirection.Normalize();

        MoveCharacter(moveDirection);

        // Logică săritură
        if (vreaSaSara)
        {
            rb.AddForce(Vector3.up * fortaSaritura, ForceMode.Impulse);
            vreaSaSara = false;
            estePePamant = false; // Presupunem că a părăsit solul
        }
    }

    void MoveCharacter(Vector3 direction)
    {
        Vector3 targetVelocity = direction * viteza;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }

    // Detectăm când atingem din nou pământul
    private void OnCollisionEnter(Collision collision)
    {
        // Verificăm dacă obiectul de care ne-am izbit are o suprafață orizontală
        // contacts[0].normal.y > 0.5f înseamnă că suprafața e destul de plată ca să stăm pe ea
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.5f)
        {
            estePePamant = true;
        }
    }
}