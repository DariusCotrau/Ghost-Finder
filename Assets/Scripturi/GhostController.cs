using UnityEngine;

public class GhostController : MonoBehaviour
{
    private Renderer[] ghostRenderers; 
    
    [Header("Setari Transparenta")]
    [Range(0f, 1f)]
    public float targetAlpha = 0.3f; // Cat de vizibila sa fie umbra sub lanterna
    
    private bool isBeingLit = false;
    private float revealTimer = 0f;
    public float fadeDelay = 0.2f; 
    
    private Material[] uniqueMaterials;

    void Start()
    {
        ghostRenderers = GetComponentsInChildren<Renderer>();
        uniqueMaterials = new Material[ghostRenderers.Length];
        
        for (int i = 0; i < ghostRenderers.Length; i++)
        {
            uniqueMaterials[i] = ghostRenderers[i].material;
            SetAlpha(uniqueMaterials[i], 0f);
        }

        // O facem complet invizibila la inceputul jocului
        SetGhostVisibility(false); 
    }

    void Update()
    {
        if (isBeingLit)
        {
            revealTimer = fadeDelay;
            isBeingLit = false; 
            
            // Daca e luminata, ne asiguram ca renderers sunt pornite si crestem opacitatea
            SetGhostVisibility(true);
            for (int i = 0; i < uniqueMaterials.Length; i++)
            {
                FadeAlpha(uniqueMaterials[i], targetAlpha);
            }
        }
        else
        {
            if (revealTimer > 0)
            {
                revealTimer -= Time.deltaTime;
                if (revealTimer <= 0)
                {
                    // Cand se termina timerul, setam alpha la 0 si ASCUNDEM de tot obiectul
                    for (int i = 0; i < uniqueMaterials.Length; i++)
                    {
                        SetAlpha(uniqueMaterials[i], 0f);
                    }
                    SetGhostVisibility(false);
                }
            }
        }
    }

    public void RevealGhost()
    {
        isBeingLit = true;
    }

    void SetGhostVisibility(bool visible)
    {
        if (ghostRenderers == null) return;
        foreach (Renderer rend in ghostRenderers)
        {
            if (rend != null)
            {
                rend.enabled = visible;
            }
        }
    }

    void SetAlpha(Material mat, float alpha)
    {
        if (mat.HasProperty("_BaseColor"))
        {
            Color c = mat.GetColor("_BaseColor");
            c.a = alpha;
            mat.SetColor("_BaseColor", c);
        }
    }

    void FadeAlpha(Material mat, float alphaTarget)
    {
        if (mat.HasProperty("_BaseColor"))
        {
            Color c = mat.GetColor("_BaseColor");
            c.a = Mathf.Lerp(c.a, alphaTarget, Time.deltaTime * 10f);
            mat.SetColor("_BaseColor", c);
        }
    }
}