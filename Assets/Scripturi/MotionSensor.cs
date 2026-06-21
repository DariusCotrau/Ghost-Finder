using UnityEngine;
using System.Collections.Generic;

public class MotionSensor : MonoBehaviour
{
    private AudioSource audioSource;
    
    [Header("Setari Sunet")]
    public float beepInterval = 1f; // O data la cate secunde sa bipaie cand e cineva aproape
    private float beepTimer;

    // Folosim o lista ca sa tinem minte exact cine e inauntru (Player, Ghost sau ambii)
    private List<Collider> objectsInside = new List<Collider>();

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        beepTimer = beepInterval;
    }

    void Update()
    {
        // Daca lista nu e goala, inseamna ca e cineva in raza (Player-ul, Fantoma, sau amandoi!)
        if (objectsInside.Count > 0)
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
        if (other.CompareTag("Ghost") || other.CompareTag("Player")) 
        {
            // Daca nu cumva l-am adaugat deja, il punem in lista
            if (!objectsInside.Contains(other))
            {
                objectsInside.Add(other);
                
                // Daca e primul care intra, bipaie instant
                if (objectsInside.Count == 1)
                {
                    beepTimer = 0f; 
                }
                
                Debug.Log($"[SENZOR] Miscare detectata! Obiect in raza: {other.gameObject.name} (Total: {objectsInside.Count})");
            }
        }
    }

    // Ruleaza automat cand corpul paraseste sfera invizibila
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ghost") || other.CompareTag("Player"))
        {
            if (objectsInside.Contains(other))
            {
                objectsInside.Remove(other);
                Debug.Log($"[SENZOR] A parasit zona: {other.gameObject.name} (Ramasi: {objectsInside.Count})");
            }
        }
    }

    // === FISICA DROPPING ===
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player") && !collision.gameObject.CompareTag("Ghost")) 
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; 
                Debug.Log("[SENZOR] S-a oprit pe sol si a activat masurile anti-cadere.");
            }
        }
    }

    void PlayBeep()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
    }
}