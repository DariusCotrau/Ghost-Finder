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
        // MOMENTAN: Verificam daca cel care a intrat este Jucatorul (poti folosi Tag-ul "Player")
        // Mai incolo, cand fantoma va merge, poti schimba sa verifice tag-ul "Ghost" sau layer-ul
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

    void PlayBeep()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
    }
}