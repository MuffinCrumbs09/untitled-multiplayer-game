using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Collider))]
public class RoomController : NetworkBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private EnemySpawner enemySpawner;

    [Header("Room Configuration")]
    [SerializeField] private List<GameObject> doorsToLock;

    private Collider _trigger;
    private bool _isCleared = false;

    private void Awake()
    {
        _trigger = GetComponent<Collider>();
        _trigger.isTrigger = true;
    }

    private void Start()
    {
        SetDoorsActive(false);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && enemySpawner != null)
        {
            enemySpawner.OnAllWavesCleared += HandleEncounterCompletion;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && enemySpawner != null)
        {
            enemySpawner.OnAllWavesCleared -= HandleEncounterCompletion;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only the Server processes triggers and starts game logic
        if (!IsServer) return;

        if (_isCleared || !other.CompareTag("Player")) return;

        StartEncounter(other.transform);
    }

    private void StartEncounter(Transform player)
    {
        _trigger.enabled = false;

        // Lock doors
        SetDoorsActive(true);

        // Tell Clients to lock doors
        LockDoorsClientRpc(true);

        enemySpawner.BeginEncounter(player);
    }

    private void HandleEncounterCompletion()
    {
        _isCleared = true;
        SetDoorsActive(false);
        LockDoorsClientRpc(false);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void LockDoorsClientRpc(bool locked)
    {
        SetDoorsActive(locked);
    }

    private void SetDoorsActive(bool isActive)
    {
        foreach (var door in doorsToLock)
        {
            if (door != null)
            {
                door.SetActive(isActive);
            }
        }
    }
}