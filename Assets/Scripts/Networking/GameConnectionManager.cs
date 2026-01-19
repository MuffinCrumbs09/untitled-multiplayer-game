using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles spawning the correct Player Object (God vs Survivor) based on 
/// the role selected in the lobby.
/// </summary>
public class GameConnectionManager : MonoBehaviour
{
    [Header("Player Prefabs")]
    [SerializeField]
    [Tooltip("Prefab instantiated for players with the God role.")]
    private GameObject godPrefab;

    [SerializeField]
    [Tooltip("Prefab instantiated for players with the Survivor role.")]
    private GameObject survivorPrefab;

    private void Start()
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback +=
                HandleClientConnect;

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
        GameObject prefabToSpawn = survivorPrefab;

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
            // Fallback for testing without Lobby
            if (clientId == 0) prefabToSpawn = godPrefab;
        }

        GameObject instance = Instantiate(prefabToSpawn);
        instance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        if (prefabToSpawn == survivorPrefab)
        {
            if (GameLoopManager.Instance != null)
            {
                var health = instance.GetComponent<HealthComponent>();
                if (health != null)
                {
                    GameLoopManager.Instance.RegisterSurvivor(health);
                }
            }
        }
    }
}