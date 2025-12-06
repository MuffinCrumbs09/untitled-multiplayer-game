using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class NetStore : NetworkBehaviour
{
    public static NetStore Instance;
    // Global storage
    public NetworkList<NetString> usernames;

    // Server-Only Storage
    private Dictionary<ulong, string> _clientIdToName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);

        Instance = this;

        usernames = new();
        _clientIdToName = new();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddUsernameServerRpc(string username, RpcParams rpc = default)
    {
        _clientIdToName[rpc.Receive.SenderClientId] = username;
        usernames.Add(username);

        Debug.Log($"Added username '{username}' to shared list");
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RemoveUsernameServerRpc(ulong clientId)
    {
        if (_clientIdToName.TryGetValue(clientId, out string disconnectedName))
        {
            for (int i = 0; i < usernames.Count; i++)
                if (usernames[i] == disconnectedName)
                {
                    usernames.RemoveAt(i);
                    _clientIdToName.Remove(clientId);

                    Debug.Log($"Removed disconnected player: {disconnectedName}");
                }
        }
        else
        {
            Debug.LogWarning($"Attempted to remove ClientId {clientId} but key not found in dictionary.");
        }
    }
}