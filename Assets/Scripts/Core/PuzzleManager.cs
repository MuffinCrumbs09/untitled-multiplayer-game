using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PuzzleManager : NetworkBehaviour
{
    // Instance
    public static PuzzleManager Instance { get; private set; }

    [Header("Completed Puzzles")]
    public NetworkVariable<bool> BoneCollected = new(false);
    public NetworkVariable<bool> FireStoked = new(false);
    public NetworkVariable<bool> StatuesDestroyed = new(false);
    public NetworkVariable<bool> MazeComplete = new(false);

    [Header("Puzzle Settings")]
    public List<Grave> Graves = new();
    public NetworkVariable<int> CorrectGraveIndex = new(0);
    public NetworkVariable<int> PlanksCollected = new(0);
    public NetworkVariable<int> TotalStatuesDestroyed = new(0);
    public int PlanksToCollect = 7;
    public Statue[] Statues;

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

    [Rpc(SendTo.Server)]
    public void CollectPlankServerRpc()
    {
        PlanksCollected.Value++;

        if(PlanksCollected.Value >= PlanksToCollect)
        {
            FireStoked.Value = true;
        }
    }

    [Rpc(SendTo.Server)]
    public void DestroyStatueServerRpc(int statue)
    {
        TotalStatuesDestroyed.Value++;
        DestroyStatueClientRpc(statue);

        if(TotalStatuesDestroyed.Value == 4)
        {
            StatuesDestroyed.Value = true;
        }
    }

    [Rpc(SendTo.NotServer)]
    private void DestroyStatueClientRpc(int statue)
    {
        Statues[statue].DestroyStatue();
    }
}