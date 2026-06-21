using UnityEngine;

/// <summary>
/// Singleton de scena (doar in scena de joc). Tine punctele de spawn pe roluri.
/// GameManager il foloseste pentru a teleporta jucatorii la inceputul meciului.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    public Transform[] hunterSpawns;
    public Transform[] ghostSpawns;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Punct de spawn pentru un rol + index (wrap daca sunt prea putine).</summary>
    public Transform GetSpawn(PlayerRole role, int index)
    {
        Transform[] arr = role == PlayerRole.Ghost ? ghostSpawns : hunterSpawns;
        if (arr == null || arr.Length == 0) return null;
        return arr[index % arr.Length];
    }
}
