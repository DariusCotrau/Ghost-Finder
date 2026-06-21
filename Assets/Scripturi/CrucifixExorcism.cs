using UnityEngine;
using UnityEngine.SceneManagement;
public class CrucifixExorcism : MonoBehaviour
{
    [Header("Setari Exorcizare")]
    public float exorcismRange = 5f;       // Distanta maxima de la care poti distruge fantoma
    public float timeNeededToWin = 3f;     // Cate secunde trebuie sa tii tinta pe ea
    private float currentExorcismTime = 0f;

    [Header("UI / Debug")]
    // Daca ai un text simplu sau vrei doar sa vezi in consola cum creste timpul
    private bool isExorcising = false;
    [Header("UI Victorie")]
    public GameObject victoryPanel;

    void Start()
    {
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
    }

    void OnEnable()
    {
        // Resetam timpul cand echipam crucifixul
        currentExorcismTime = 0f;
        isExorcising = false;
    }

    void Update()
    {
        // Tragem o raza din centrul camerei, fix in fata
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        // Schimba "Ghost" cu tag-ul exact pe care il are fantoma ta in Inspector!
        if (Physics.Raycast(ray, out hit, exorcismRange))
        {
            if (hit.collider.CompareTag("Ghost")) 
            {
                isExorcising = true;
                currentExorcismTime += Time.deltaTime;

                Debug.Log($"[CRUCIFIX] Distrugi fantoma! Progres: {currentExorcismTime:F1} / {timeNeededToWin} secunde.");

                if (currentExorcismTime >= timeNeededToWin)
                {
                    WinGame();
                }
                return; // Iesim din update ca sa nu treaca pe fals jos
            }
        }

        // Daca ne-am luat privirea de la ea, timpul incepe sa scada incet (ca sa nu fie prea usor)
        if (isExorcising)
        {
            isExorcising = false;
            Debug.Log("[CRUCIFIX] Ai pierdut vizualul cu fantoma!");
        }
        
        if (currentExorcismTime > 0)
        {
            currentExorcismTime -= Time.deltaTime * 0.1f; // Scade mai lent decat creste
        }
    }

    void WinGame()
    {
        Debug.Log("[VICTORIE] Fantoma a fost exorcizata!");

        GameObject ghost =
            GameObject.FindWithTag("Ghost");

        if (ghost != null)
        {
            SimpleGhostMovement movement =
                ghost.GetComponent<SimpleGhostMovement>();

            if (movement != null)
            {
                movement.Exorcise();
            }
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);

            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;

            Time.timeScale = 0f;
        }
    }
}