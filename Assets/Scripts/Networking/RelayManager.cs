using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages connection to Unity Relay and Lobby services, handles UI for 
/// session creation/joining, and manages scene transitions.
/// </summary>
public class RelayManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField]
    [Tooltip("Button to start a host session.")]
    private Button startHostButton;

    [SerializeField]
    [Tooltip("Button to join a session via code.")]
    private Button joinClientButton;

    [SerializeField]
    [Tooltip("Button to quick join a public lobby.")]
    private Button quickJoinButton;

    [SerializeField]
    [Tooltip("Button to leave the current lobby.")]
    private Button quitLobbyButton;

    [SerializeField]
    [Tooltip("Button to start the gameplay scene (Host only).")]
    private Button startGameButton;

    [Header("Lobby Settings")]
    [SerializeField]
    [Tooltip("Toggle to determine if the lobby is publicly searchable.")]
    private Toggle lobbyPublicToggle;

    [SerializeField]
    [Tooltip("Input field for entering a join code.")]
    private TMP_InputField input;

    [SerializeField]
    [Tooltip("Input field for the player's username.")]
    private TMP_InputField username;

    [SerializeField]
    [Tooltip("Text display for the generated join code.")]
    private TMP_Text code;

    [Header("Scene Management")]
    [SerializeField]
    [Tooltip("Name of the scene to load when the game starts.")]
    private string gameplaySceneName = "GameplayScene";

    private static string _joinCode;
    private static Lobby _currentLobby;
    private float _heartbeatTimer;

    private async void Start()
    {
        // Force cursor unlock to ensure menu usability after returning from game
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Create initialization options to handle profile switching
        InitializationOptions options = new InitializationOptions();

#if UNITY_EDITOR
        // In Editor, we generally want the default profile (or ParrelSync handles it).
        // No specific profile set needed here usually.
#else
        // In Builds, generate a random profile ID.
        // This ensures every build instance on the same machine gets a unique Identity/PlayerID.
        // Without this, 2 builds on 1 PC look like the same user to the Lobby Service, causing Quick Join to fail.
        options.SetProfile("Build_" + UnityEngine.Random.Range(0, 10000).ToString());
#endif

        await UnityServices.InitializeAsync(options);

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        if (startHostButton) startHostButton.onClick.AddListener(StartRelayAndLobby);
        if (joinClientButton) joinClientButton.onClick.AddListener(() => JoinRelay(input.text));
        if (quickJoinButton) quickJoinButton.onClick.AddListener(QuickJoinLobby);
        if (quitLobbyButton) quitLobbyButton.onClick.AddListener(QuitLobby);
        if (lobbyPublicToggle) lobbyPublicToggle.onValueChanged.AddListener(OnPublicToggleChanged);

        if (startGameButton)
        {
            startGameButton.onClick.AddListener(StartGame);
            startGameButton.gameObject.SetActive(false);
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnConnectionEvent += ConnectionEvent;

            // Restore UI state if returning to menu while still connected
            if (NetworkManager.Singleton.IsListening)
            {
                RestoreLobbyState();
            }
        }
    }

    private void RestoreLobbyState()
    {
        CanvasManager.Instance.PickCanvas(CurrentCanvas.InLobby);
        code.text = _joinCode;

        if (NetworkManager.Singleton.IsHost && startGameButton != null)
        {
            startGameButton.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnConnectionEvent -= ConnectionEvent;
        }
    }

    #region Lobby & Relay Integration

    private async void StartRelayAndLobby()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            _joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();

            CreateLobbyOptions options = new CreateLobbyOptions();
            options.IsPrivate = !lobbyPublicToggle.isOn;
            options.Data = new Dictionary<string, DataObject>()
            {
                { "RelayJoinCode", new DataObject(visibility: DataObject.VisibilityOptions.Member, value: _joinCode) }
            };

            _currentLobby = await LobbyService.Instance.CreateLobbyAsync("My Game Lobby", 4, options);

            code.text = _joinCode;
            CanvasManager.Instance.PickCanvas(CurrentCanvas.InLobby);

            if (startGameButton != null)
                startGameButton.gameObject.SetActive(true);

            string sUser = string.IsNullOrEmpty(username.text) ? "Host" : username.text;
            NetStore.Instance.AddPlayerDataServerRpc(new NetPlayerData(sUser, 0, PlayerRole.God));
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start Host: {e.Message}");
        }
    }

    private async void QuickJoinLobby()
    {
        try
        {
            _currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            string relayCode = _currentLobby.Data["RelayJoinCode"].Value;
            JoinRelay(relayCode);
        }
        catch (Exception e)
        {
            Debug.LogError($"Quick Join Failed: {e.Message}");
        }
    }

    private async void JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            var relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartClient();
            _joinCode = joinCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"Join Relay Failed: {e.Message}");
        }
    }

    private async void HandleLobbyHeartbeat()
    {
        if (_currentLobby != null && NetworkManager.Singleton.IsHost)
        {
            _heartbeatTimer -= Time.deltaTime;
            if (_heartbeatTimer <= 0f)
            {
                _heartbeatTimer = 15f;
                await LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
            }
        }
    }

    private async void OnPublicToggleChanged(bool isPublic)
    {
        if (_currentLobby == null || !NetworkManager.Singleton.IsHost) return;
        try
        {
            UpdateLobbyOptions options = new UpdateLobbyOptions { IsPrivate = !isPublic };
            _currentLobby = await LobbyService.Instance.UpdateLobbyAsync(_currentLobby.Id, options);
        }
        catch (Exception e) { Debug.LogError($"Failed to update lobby: {e.Message}"); }
    }

    #endregion

    #region Scene & Cleanup

    private void QuitLobby()
    {
        if (_currentLobby != null && NetworkManager.Singleton.IsHost)
        {
            LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
        }

        if (NetStore.Instance != null) Destroy(NetStore.Instance.gameObject);
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();

        _currentLobby = null;
        _joinCode = null;

        SceneManager.LoadScene(0);
    }

    private void StartGame()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    private void ConnectionEvent(NetworkManager manager, ConnectionEventData data)
    {
        if (data.EventType == Unity.Netcode.ConnectionEvent.ClientConnected)
        {
            if (data.ClientId != manager.LocalClientId) return;

            code.text = _joinCode;

            if (!manager.IsHost)
            {
                ulong playerNum = data.ClientId + 1;
                string sUser = string.IsNullOrEmpty(username.text) ? $"Player {playerNum}" : username.text;
                NetStore.Instance.AddPlayerDataServerRpc(new NetPlayerData(sUser, data.ClientId, PlayerRole.Survivor));
            }

            CanvasManager.Instance.PickCanvas(CurrentCanvas.InLobby);

            if (manager.IsHost && startGameButton != null)
            {
                startGameButton.gameObject.SetActive(true);
            }
        }
        else if (data.EventType == Unity.Netcode.ConnectionEvent.ClientDisconnected)
        {
            if (manager.IsClient && !manager.IsHost)
            {
                QuitLobby();
                return;
            }
            if (manager.IsServer && manager.IsListening)
            {
                NetStore.Instance.RemovePlayerDataServerRpc(data.ClientId);
            }
        }
    }

    #endregion
}