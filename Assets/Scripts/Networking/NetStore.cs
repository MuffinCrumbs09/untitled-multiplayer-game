using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class NetStore : NetworkBehaviour
{
    public static NetStore Instance;
    // Global storage
    public NetworkList<NetPlayerData> playerData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this);

        Instance = this;

        playerData = new();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddPlayerDataServerRpc(NetPlayerData data, RpcParams rpc = default)
    {
        ulong clientID = rpc.Receive.SenderClientId;
        data.clientID = clientID;
        playerData.Add(data);

        Debug.Log($"Added '{data.username}' to shared list");
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RemovePlayerDataServerRpc(ulong clientId)
    {
        for(int i = 0; i < playerData.Count; i++)
        {
            if (playerData[i].clientID == clientId)
            {
                string user = playerData[i].username;
                playerData.RemoveAt(i);

                Debug.Log($"Removed player data of: {user}");
                break;
            }
        }
    }
}