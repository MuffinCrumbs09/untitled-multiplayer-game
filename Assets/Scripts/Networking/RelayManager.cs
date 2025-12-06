
using System;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WebSocketSharp;

public class RelayManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Button[] buttons;
    [SerializeField] private TMP_InputField input;
    [SerializeField] private TMP_InputField username;
    [SerializeField] private TMP_Text code;

    private string _joinCode;

    private async void Start()
    {
        // Connect to unity services
        await UnityServices.InitializeAsync();

        // Sign in if we havent
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        NetworkManager.Singleton.OnConnectionEvent += ConnectionEvent;

        buttons[0].onClick.AddListener(StartRelay);
        buttons[1].onClick.AddListener(() => JoinRelay(input.text));
        buttons[2].onClick.AddListener(QuitLobby);
    }

    #region Networking
    private async void StartRelay()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3); // Create a room with 3 peers and 1 host

        _joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId); // Get the join code

        // Create and set relay server data
        var relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartHost(); // Start the server as a host
    }

    private async void JoinRelay(string joinCode)
    {
        // Get lobby details and set relay server
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        var relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartClient(); // Join server as client

        _joinCode = joinCode; // Save display code
    }

    private void QuitLobby()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown(); // Disconnectt or Stops Server
            Destroy(NetworkManager.Singleton.gameObject); // Removes the dependant
        }

        SceneManager.LoadScene(0); // Creates a new dependant
    }
    #endregion

    #region Events
    private void ConnectionEvent(NetworkManager manager, ConnectionEventData data)
    {
        // Client Connected
        if (data.EventType == Unity.Netcode.ConnectionEvent.ClientConnected)
        {
            if (data.ClientId != manager.LocalClientId) return;

            code.text = _joinCode; // Display join code
            int plyer = NetStore.Instance.usernames.Count + 1;
            string sUser = username.text.IsNullOrEmpty() ? $"Player {plyer}" : username.text; // Default username check
            NetStore.Instance.AddUsernameServerRpc(sUser); // Add username to the list
            CanvasManager.Instance.PickCanvas(CurrentCanvas.InLobby);
        }
        // Client Disconnected
        else if (data.EventType == Unity.Netcode.ConnectionEvent.ClientDisconnected)
        {
            if (manager.IsClient && !manager.IsHost)
            {
                Debug.Log("Client detected disconnect - quitting lobby");
                QuitLobby();
                return;
            }

            if (manager.IsServer && manager.IsListening)
                NetStore.Instance.RemoveUsernameServerRpc(data.ClientId);
        }
    }
    #endregion
}