using UnityEngine;

public class MotionSensor : MonoBehaviour
{
    private AudioSource audioSource;
    
    [Header("Setari Sunet")]
    public float beepInterval = 1f; // O data la cate secunde sa bipaie cand e cineva aproape
    private float beepTimer;

    private bool isObjectInside = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        beepTimer = beepInterval;
    }

    void Update()
    {
        // Daca este cineva in raza, senzorul va bipaia ritmic
        if (isObjectInside)
        {
            beepTimer -= Time.deltaTime;
            if (beepTimer <= 0f)
            {
                PlayBeep();
                beepTimer = beepInterval; // Reseteaza timerul
            }
        }
    }

    // Ruleaza automat cand un corp intra in sfera invizibila (Trigger)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            isObjectInside = true;
            beepTimer = 0f; // Bipaie instant cand ai calcat in raza lui
            Debug.Log("[SENZOR] Miscare detectata in zona!");
        }
    }

    // Ruleaza automat cand corpul paraseste sfera invizibila
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isObjectInside = false;
            Debug.Log("[SENZOR] Zona este din nou sigura.");
        }
    }

    // === COPIAZĂ DE AICI JOS PENTRU FISICĂ ===

    // Ruleaza automat cand obiectul fizic loveste podeaua sau un perete dupa ce l-ai aruncat
    private void OnCollisionEnter(Collision collision)
    {
        // Daca am lovit mediul (podea, pereti, mobila - adica NU player-ul)
        if (!collision.gameObject.CompareTag("Player"))
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; // Înghețăm senzorul pe loc ca sa nu treaca sub harta
                Debug.Log("[SENZOR] S-a oprit pe sol si a activat masurile anti-cadere.");
            }
        }
    }

    // =========================================

    void PlayBeep()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
    }
}