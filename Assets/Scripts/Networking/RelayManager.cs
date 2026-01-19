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
/// Manages Relay and Lobby services, including real-time privacy settings.
/// </summary>
public class RelayManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button joinClientButton;
    [SerializeField] private Button quickJoinButton;
    [SerializeField] private Button quitLobbyButton;
    [SerializeField] private Button startGameButton;

    [Space]
    [Tooltip("Toggle to determine if the lobby is searchable via Quick Join.")]
    [SerializeField] private Toggle lobbyPublicToggle;

    [Space]
    [SerializeField] private TMP_InputField input;
    [SerializeField] private TMP_InputField username;
    [SerializeField] private TMP_Text code;

    [Header("Scene Management")]
    [SerializeField] private string gameplaySceneName = "GameplayScene";

    private string _joinCode;
    private Lobby _currentLobby;
    private float _heartbeatTimer;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnConnectionEvent += ConnectionEvent;
        }

        if (startHostButton)
            startHostButton.onClick.AddListener(StartRelayAndLobby);

        if (joinClientButton)
            joinClientButton.onClick.AddListener(() => JoinRelay(input.text));

        if (quickJoinButton)
            quickJoinButton.onClick.AddListener(QuickJoinLobby);

        if (quitLobbyButton)
            quitLobbyButton.onClick.AddListener(QuitLobby);

        if (lobbyPublicToggle)
            lobbyPublicToggle.onValueChanged.AddListener(OnPublicToggleChanged);

        if (startGameButton)
        {
            startGameButton.onClick.AddListener(StartGame);
            startGameButton.gameObject.SetActive(false);
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
            Allocation allocation =
                await RelayService.Instance.CreateAllocationAsync(3);

            _joinCode =
                await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var relayServerData =
                AllocationUtils.ToRelayServerData(allocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();
            Debug.Log($"Host Started. Relay Code: {_joinCode}");

            CreateLobbyOptions options = new CreateLobbyOptions();
            options.IsPrivate = !lobbyPublicToggle.isOn;

            options.Data = new Dictionary<string, DataObject>()
            {
                {
                    "RelayJoinCode", new DataObject(
                        visibility: DataObject.VisibilityOptions.Member,
                        value: _joinCode)
                }
            };

            _currentLobby = await LobbyService.Instance.CreateLobbyAsync(
                "My Game Lobby", 4, options);

            Debug.Log($"Lobby Created: {_currentLobby.Id}");
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
            Debug.Log("Attempting Quick Join...");

            _currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync();

            string relayCode = _currentLobby.Data["RelayJoinCode"].Value;
            Debug.Log($"Joined Lobby. Found Relay Code: {relayCode}");

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
            JoinAllocation allocation =
                await RelayService.Instance.JoinAllocationAsync(joinCode);

            var relayServerData =
                AllocationUtils.ToRelayServerData(allocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(relayServerData);

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
                float heartbeatPeriod = 15f;
                _heartbeatTimer = heartbeatPeriod;

                await LobbyService.Instance.SendHeartbeatPingAsync(
                    _currentLobby.Id);
            }
        }
    }

    private async void OnPublicToggleChanged(bool isPublic)
    {
        if (_currentLobby == null || !NetworkManager.Singleton.IsHost) return;

        try
        {
            UpdateLobbyOptions options = new UpdateLobbyOptions
            {
                IsPrivate = !isPublic
            };

            _currentLobby = await LobbyService.Instance.UpdateLobbyAsync(
                _currentLobby.Id, options);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to update lobby privacy: {e.Message}");
        }
    }

    #endregion

    #region Scene & Cleanup

    private void QuitLobby()
    {
        if (_currentLobby != null && NetworkManager.Singleton.IsHost)
        {
            LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
        }

        // Fix: Explicitly destroy NetStore to clear old session data.
        if (NetStore.Instance != null)
        {
            Destroy(NetStore.Instance.gameObject);
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            Destroy(NetworkManager.Singleton.gameObject);
        }

        SceneManager.LoadScene(0);
    }

    private void StartGame()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        NetworkManager.Singleton.SceneManager.LoadScene(
            gameplaySceneName,
            LoadSceneMode.Single);
    }

    private void ConnectionEvent(NetworkManager manager, ConnectionEventData data)
    {
        if (data.EventType == Unity.Netcode.ConnectionEvent.ClientConnected)
        {
            if (data.ClientId != manager.LocalClientId) return;

            code.text = _joinCode;

            ulong playerNum = data.ClientId + 1;
            string sUser = string.IsNullOrEmpty(username.text)
                ? $"Player {playerNum}"
                : username.text;

            NetStore.Instance.AddPlayerDataServerRpc(
                new NetPlayerData(sUser, data.ClientId, PlayerRole.Survivor));

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
                NetStore.Instance.RemovePlayerDataServerRpc(data.ClientId);
        }
    }

    #endregion
}