using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles spawning Player objects (God vs Survivor) based on Lobby roles.
/// </summary>
public class GameConnectionManager : MonoBehaviour
{
    [Header("Player Prefabs")]
    [Tooltip("Prefab for the God role.")]
    [SerializeField] private GameObject godPrefab;
    [Tooltip("Prefab for the Survivor role.")]
    [SerializeField] private GameObject survivorPrefab;

    private void Start()
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsServer)
        {
            // Handle late joiners
            NetworkManager.Singleton.OnClientConnectedCallback +=
                HandleClientConnect;

            // Spawn for existing players
            foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds)
            {
                SpawnCorrectPlayer(id);
            }
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -=
                HandleClientConnect;
        }
    }

    private void HandleClientConnect(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        SpawnCorrectPlayer(clientId);
    }

    private void SpawnCorrectPlayer(ulong clientId)
    {
        GameObject prefabToSpawn = survivorPrefab; // Default

        // 1. Check NetStore for the chosen role
        if (NetStore.Instance != null)
        {
            foreach (var data in NetStore.Instance.playerData)
            {
                if (data.clientID == clientId)
                {
                    if (data.role == PlayerRole.God)
                    {
                        prefabToSpawn = godPrefab;
                    }
                    break;
                }
            }
        }
        else
        {
            // Fallback for testing without Lobby: Host is always God
            if (clientId == 0) prefabToSpawn = godPrefab;
        }

        Debug.Log($"[ConnectionManager] Spawning {prefabToSpawn.name} " +
                  $"for Client {clientId}");

        GameObject instance = Instantiate(prefabToSpawn);
        instance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }
}