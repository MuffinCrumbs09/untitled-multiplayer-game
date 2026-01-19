using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Unity.Collections;

/// <summary>
/// Manages the primary game loop, including win conditions and returning to 
/// the lobby. Tracks active survivors and synchronizes the game over state.
/// </summary>
public class GameLoopManager : NetworkBehaviour
{
    public static GameLoopManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField]
    [Tooltip("The name of the lobby scene to load on reset.")]
    private string lobbySceneName = "MainMenu";

    /// <summary>
    /// Synchronizes the Game Over state to all clients.
    /// </summary>
    public NetworkVariable<bool> IsGameOver = new NetworkVariable<bool>(false);

    /// <summary>
    /// Synchronizes the win message string to all clients.
    /// </summary>
    public NetworkVariable<FixedString64Bytes> WinnerMessage =
        new NetworkVariable<FixedString64Bytes>("");

    private List<HealthComponent> _activeSurvivors = new List<HealthComponent>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback +=
                HandleClientDisconnect;

            // Ensure state is clean on spawn
            IsGameOver.Value = false;
            WinnerMessage.Value = "";
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -=
                HandleClientDisconnect;
        }
    }

    /// <summary>
    /// Registers a survivor's health component to track their life state.
    /// Should be called when the survivor spawns.
    /// </summary>
    public void RegisterSurvivor(HealthComponent survivorHealth)
    {
        if (!IsServer) return;

        if (!_activeSurvivors.Contains(survivorHealth))
        {
            _activeSurvivors.Add(survivorHealth);
            survivorHealth.OnDeath += CheckGameState;
        }
    }

    /// <summary>
    /// Initiates the sequence to return all players to the main menu lobby.
    /// Can only be called by the Server/Host.
    /// </summary>
    public void ReturnToLobby()
    {
        if (!IsServer) return;

        // Despawn all Enemies to ensure clean state on clients
        var enemies = FindObjectsByType<EnemyController>(
            FindObjectsSortMode.None);

        foreach (var enemy in enemies)
        {
            if (enemy.TryGetComponent<NetworkObject>(out var netObj) &&
                netObj.IsSpawned)
            {
                netObj.Despawn();
            }
        }

        // Despawn all Player Objects to prevent them carrying over to the menu
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null && client.PlayerObject.IsSpawned)
            {
                client.PlayerObject.Despawn(true);
            }
        }

        IsGameOver.Value = false;
        _activeSurvivors.Clear();

        NetworkManager.Singleton.SceneManager.LoadScene(
            lobbySceneName,
            LoadSceneMode.Single);
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        if (!IsServer) return;

        _activeSurvivors.RemoveAll(h => h == null);
        CheckGameState();
    }

    private void CheckGameState()
    {
        if (IsGameOver.Value) return;

        _activeSurvivors.RemoveAll(h => h == null);

        int aliveCount = 0;
        foreach (var survivor in _activeSurvivors)
        {
            if (!survivor.IsDead)
            {
                aliveCount++;
            }
        }

        if (aliveCount == 0 && _activeSurvivors.Count > 0)
        {
            EndGame("GOD WINS!");
        }
    }

    private void EndGame(string message)
    {
        IsGameOver.Value = true;
        WinnerMessage.Value = message;
    }
}