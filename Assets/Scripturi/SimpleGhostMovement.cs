using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleGhostMovement : MonoBehaviour
{
    [Header("Setari Miscare")]
    public float speed = 2f;             // Viteza cu care se plimba
    public float changeDirectionTime = 3f; // La cate secunde isi schimba directia

    private Rigidbody rb;
    private Vector3 movementDirection;
    private float timer;
    private bool isExorcised = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true; // O lăsăm cu gravitație ca să stea pe podea
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        ChooseRandomDirection();
    }

    void FixedUpdate()
    {
        if (isExorcised)
            {
                rb.linearVelocity = Vector3.zero;
                return;
            }
        Vector3 velocity = movementDirection * speed;
        velocity.y = rb.linearVelocity.y; 
        rb.linearVelocity = velocity;
    }

    void Update()
    {
        // Numaram timpul pentru schimbarea directiei
        timer += Time.deltaTime;

        if (timer >= changeDirectionTime)
        {
            ChooseRandomDirection();
        }
    }

    void ChooseRandomDirection()
    {
        timer = 0f;

        // Direcție random pe X și Z
        float randomX = Random.Range(-1f, 1f);
        float randomZ = Random.Range(-1f, 1f);

        movementDirection = new Vector3(randomX, 0f, randomZ).normalized;

        // Rotim fantoma spre direcția în care merge
        if (movementDirection != Vector3.zero)
        {
            transform.forward = movementDirection;
        }
    }

    // Dacă se lovește de un perete înainte să treacă cele 3 secunde, își schimbă direcția instant ca să nu meargă în perete ca un robot blocat
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
        {
            ChooseRandomDirection();
        }
    }
    public void Exorcise()
    {
        isExorcised = true;

        rb.linearVelocity = Vector3.zero;

        Debug.Log("[GHOST] Fantoma a fost exorcizata!");
    }
}