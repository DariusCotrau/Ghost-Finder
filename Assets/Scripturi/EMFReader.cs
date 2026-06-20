using UnityEngine;

public class EMFReader : MonoBehaviour
{
    [Header("Setari Raycast")]
    public float maxDetectionRange = 10f; // S-a schimbat la 10 metri. Cele 5 zone vor fi de cate 2 metri fiecare!
    public LayerMask ghostLayer;

    [Header("Cele 5 Sunete EMF (De la Trimmer)")]
    public AudioClip emfLevel1Clip; 
    public AudioClip emfLevel2Clip;
    public AudioClip emfLevel3Clip;
    public AudioClip emfLevel4Clip;
    public AudioClip emfLevel5Clip; 

    private AudioSource audioSource;
    private bool isDetectingGhost = false;
    private float currentDistanceToGhost;
    private int currentEMFLevel = 0;
    private int previousEMFLevel = 0; 
    private bool isEquipped = true;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = true; 
        }
    }

    void Update()
    {
        if (!isEquipped) return;

        CheckForGhost();

        if (isDetectingGhost)
        {
            if (currentEMFLevel != previousEMFLevel)
            {
                ChangeEMFSound(currentEMFLevel);
                previousEMFLevel = currentEMFLevel;
            }
        }
        else
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
                audioSource.clip = null;
            }
            currentEMFLevel = 0;
            previousEMFLevel = 0;
        }
    }

    void CheckForGhost()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDetectionRange, ghostLayer))
        {
            if (hit.collider.GetComponent<GhostController>() != null)
            {
                isDetectingGhost = true;
                currentDistanceToGhost = hit.distance;
                
                // Matematica se ajusteaza singura la 10m:
                // Nivel 1: intre 8m si 10m
                // Nivel 2: intre 6m si 8m
                // Nivel 3: intre 4m si 6m
                // Nivel 4: intre 2m si 4m
                // Nivel 5: sub 2 metri (panica!)
                currentEMFLevel = Mathf.Clamp(Mathf.CeilToInt((1f - (currentDistanceToGhost / maxDetectionRange)) * 5f), 1, 5);
                return;
            }
        }

        isDetectingGhost = false;
    }

    void ChangeEMFSound(int level)
    {
        AudioClip targetClip = null;

        switch (level)
        {
            case 1: targetClip = emfLevel1Clip; break;
            case 2: targetClip = emfLevel2Clip; break;
            case 3: targetClip = emfLevel3Clip; break;
            case 4: targetClip = emfLevel4Clip; break;
            case 5: targetClip = emfLevel5Clip; break;
        }

        if (targetClip != null)
        {
            audioSource.clip = targetClip;
            audioSource.Play();
            Debug.Log($"[EMF] Schimbat pe Nivelul: {level} (Distanta aproximativa: {currentDistanceToGhost:F1}m)");
        }
    }
}