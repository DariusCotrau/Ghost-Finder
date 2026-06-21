using UnityEngine;

/// <summary>
/// Detine spawn point-urile pentru fiecare rol. Pune-l pe un GameObject in scena
/// si trage Transform-uri goale (empty objects) ca puncte de spawn.
/// GameManager il foloseste la pornirea meciului ca sa pozitioneze jucatorii.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Spawn Points")]
    public Transform[] hunterSpawns;
    public Transform[] ghostSpawns;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Returneaza un spawn point pentru rolul cerut. index = al catelea jucator
    /// din acel rol (ca sa nu se suprapuna). Daca lipsesc puncte, intoarce null.
    /// </summary>
    public Transform GetSpawn(PlayerRole role, int index)
    {
        Transform[] list = role == PlayerRole.Ghost ? ghostSpawns : hunterSpawns;
        if (list == null || list.Length == 0) return null;
        return list[index % list.Length];
    }
}
