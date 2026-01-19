using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Persistent storage for lobby data. Manages the player list and 
/// enforces the unique God role constraint.
/// </summary>
public class NetStore : NetworkBehaviour
{
    public static NetStore Instance;

    /// <summary>
    /// Synchronized list of all players currently in the lobby.
    /// </summary>
    public NetworkList<NetPlayerData> playerData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Fix: Destroy the entire GameObject to prevent duplicates.
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        playerData = new();
    }

    public override void OnNetworkSpawn()
    {
        // Safety: Ensure the list is empty when a new session starts.
        if (IsServer)
        {
            playerData.Clear();
        }
    }

    /// <summary>
    /// Adds a new player to the shared list. Assigns the God role 
    /// to the host if the slot is available.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddPlayerDataServerRpc(NetPlayerData data,
                                       RpcParams rpc = default)
    {
        ulong senderId = rpc.Receive.SenderClientId;
        data.clientID = senderId;

        if (senderId == 0 && !IsGodSlotOccupied())
        {
            data.role = PlayerRole.God;
        }
        else
        {
            data.role = PlayerRole.Survivor;
        }

        playerData.Add(data);
        Debug.Log($"[NetStore] Added {data.username} as {data.role}");
    }

    /// <summary>
    /// Removes a player from the shared list based on Client ID.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RemovePlayerDataServerRpc(ulong clientId)
    {
        for (int i = 0; i < playerData.Count; i++)
        {
            if (playerData[i].clientID == clientId)
            {
                playerData.RemoveAt(i);
                break;
            }
        }
    }

    /// <summary>
    /// Updates a player's role. Enforces the single-God constraint.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void UpdatePlayerRoleServerRpc(ulong clientId,
                                          PlayerRole newRole)
    {
        if (newRole == PlayerRole.Survivor)
        {
            ApplyRoleChange(clientId, newRole);
            return;
        }

        if (newRole == PlayerRole.God)
        {
            if (!IsGodSlotOccupied())
            {
                ApplyRoleChange(clientId, newRole);
            }
            else
            {
                Debug.LogWarning("[NetStore] Role change denied. " +
                                 "God slot is already occupied.");
            }
        }
    }

    private bool IsGodSlotOccupied()
    {
        foreach (var p in playerData)
        {
            if (p.role == PlayerRole.God) return true;
        }
        return false;
    }

    private void ApplyRoleChange(ulong clientId, PlayerRole role)
    {
        for (int i = 0; i < playerData.Count; i++)
        {
            if (playerData[i].clientID == clientId)
            {
                var updatedData = playerData[i];
                updatedData.role = role;
                playerData[i] = updatedData;
                break;
            }
        }
    }
}