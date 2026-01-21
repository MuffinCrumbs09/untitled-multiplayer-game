using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PuzzleManager : NetworkBehaviour
{
    // Instance
    public static PuzzleManager Instance { get; private set; }

    [Header("Puzzle Settings")]
    public List<Grave> Graves = new();
    public NetworkVariable<int> CorrectGraveIndex = new(0);

    #region Unity Functions
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (!IsServer) return;

        CorrectGraveIndex.Value = Random.Range(0, Graves.Count);
    }
    #endregion

    public bool IsCorrectGrave(Grave grave)
    {
        return Graves.IndexOf(grave) == CorrectGraveIndex.Value;
    }
}