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

//abcdefg
/// <summary>
/// Manages connection to Unity Relay and Lobby services.
/// Handles UI for session creation, joining, and role validation.
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

    [SerializeField]
    [Tooltip("Text component inside the Start Game button to show status.")]
    private TMP_Text startGameButtonText;

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
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        InitializationOptions options = new InitializationOptions();

#if !UNITY_EDITOR
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

            if (NetworkManager.Singleton.IsListening)
            {
                RestoreLobbyState();
            }
        }

        if (NetStore.Instance != null)
        {
            NetStore.Instance.playerData.OnListChanged += HandlePlayerListChanged;
        }
    }

    private void RestoreLobbyState()
    {
        CanvasManager.Instance.PickCanvas(CurrentCanvas.InLobby);
        code.text = _joinCode;

        if (NetworkManager.Singleton.IsHost && startGameButton != null)
        {
            startGameButton.gameObject.SetActive(true);
            ValidateGameStart();
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

        if (NetStore.Instance != null)
        {
            NetStore.Instance.playerData.OnListChanged -= HandlePlayerListChanged;
        }
    }

    private void HandlePlayerListChanged(NetworkListEvent<NetPlayerData> changeEvent)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            ValidateGameStart();
        }
    }

    private void ValidateGameStart()
    {
        if (startGameButton == null) return;

        bool hasGod = false;
        foreach (var player in NetStore.Instance.playerData)
        {
            if (player.role == PlayerRole.God)
            {
                hasGod = true;
                break;
            }
        }

        startGameButton.interactable = hasGod;

        if (startGameButtonText != null)
        {
            startGameButtonText.text = hasGod ? "START GAME" : "NEED GOD";
            startGameButtonText.color = hasGod ? Color.white : Color.red;
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
            {
                startGameButton.gameObject.SetActive(true);
                ValidateGameStart();
            }

            string sUser = username != null ? username.text : "";
            NetStore.Instance.AddPlayerDataServerRpc(new NetPlayerData(sUser, 0, PlayerRole.God, 0));
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

    private async void QuitLobby()
    {
        try
        {
            if (_currentLobby != null)
            {
                if (NetworkManager.Singleton.IsHost)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
                }
                else
                {
                    // Explicitly remove the local player from the lobby service
                    await LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, AuthenticationService.Instance.PlayerId);
                }
            }
        }
        catch (Exception e)
        {
            // Catch errors if the lobby was already closed or player already removed
            Debug.LogWarning($"Lobby cleanup error: {e.Message}");
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
                string sUser = username != null ? username.text : "";
                NetStore.Instance.AddPlayerDataServerRpc(new NetPlayerData(sUser, data.ClientId, PlayerRole.Survivor, 0));
            }

            CanvasManager.Instance.PickCanvas(CurrentCanvas.InLobby);

            if (manager.IsHost && startGameButton != null)
            {
                startGameButton.gameObject.SetActive(true);
                ValidateGameStart();
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