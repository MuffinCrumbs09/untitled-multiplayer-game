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

public class RelayManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Button[] buttons;
    [SerializeField] private TMP_InputField input;
    [SerializeField] private TMP_Text code;

    private string _joinCode;

    private async void Start()
    {
        // Connect to unity services
        await UnityServices.InitializeAsync();

        // Sign in if we havent
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        NetworkManager.Singleton.OnConnectionEvent += OnConnected;

        buttons[0].onClick.AddListener(StartRelay);
        buttons[1].onClick.AddListener(() => JoinRelay(input.text));
        buttons[2].onClick.AddListener(QuitLobby);
    }

    private void OnConnected(NetworkManager manager, ConnectionEventData data)
    {
        if (data.EventType == ConnectionEvent.ClientConnected)
            CanvasManager.Instance.PickCanvas(CurrentCanvas.InLobby);
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

        code.text = _joinCode; // Display join code
        CanvasManager.Instance.PickCanvas(CurrentCanvas.InLobby);
    }

    private async void JoinRelay(string joinCode)
    {
        // Get lobby details and set relay server
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        var relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartClient(); // Join server as client

        code.text = joinCode; // Display join code
        CanvasManager.Instance.PickCanvas(CurrentCanvas.InLobby);
    }

    private void QuitLobby()
    {
        NetworkManager.Singleton.Shutdown(); // Disconnect client or Shut down server
        
        Destroy(NetworkManager.Singleton.gameObject); // Remove network dependencies

        SceneManager.LoadScene(0); // Main menu (creates new network dependencies)
    }
    #endregion
}