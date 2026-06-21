using UnityEngine;

public class MovementScript : MonoBehaviour
{
    public float viteza = 5f;
    public float fortaSaritura = 5f; 
    
    private Rigidbody rb;
    private Vector2 movement;
    private bool vreaSaSara = false;
    private bool estePePamant = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; 
    }

    void Update()
    {
        // Input miscare
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        
        if (Input.GetButtonDown("Jump") && estePePamant)
        {
            vreaSaSara = true;
        }
    }

    void FixedUpdate()
    {
        
        Vector3 moveDirection = transform.right * movement.x + transform.forward * movement.y;
        if (moveDirection.magnitude > 1) moveDirection.Normalize();

        MoveCharacter(moveDirection);

       
        if (vreaSaSara)
        {
            rb.AddForce(Vector3.up * fortaSaritura, ForceMode.Impulse);
            vreaSaSara = false;
            estePePamant = false; 
        }
    }

    void MoveCharacter(Vector3 direction)
    {
        Vector3 targetVelocity = direction * viteza;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }

    
    private void OnCollisionEnter(Collision collision)
    {
        
        // contacts[0].normal.y > 0.5f inseamna ca suprafata e plata
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.5f)
        {
            estePePamant = true;
        }
    }
}