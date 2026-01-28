using System;
using Unity.Collections;
using Unity.Netcode;

public enum PlayerRole
{
    Survivor,
    God
}

/// <summary>
/// Networked data structure for a player in the lobby.
/// </summary>
public struct NetPlayerData : INetworkSerializable, IEquatable<NetPlayerData>
{
    public NetString username;
    public ulong clientID;
    public PlayerRole role;
    public int skinIndex; // Added for customization

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out username);
            reader.ReadValueSafe(out clientID);
            reader.ReadValueSafe(out role);
            reader.ReadValueSafe(out skinIndex);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(username);
            writer.WriteValueSafe(clientID);
            writer.WriteValueSafe(role);
            writer.WriteValueSafe(skinIndex);
        }
    }

    public bool Equals(NetPlayerData other)
    {
        return other.clientID == clientID &&
               other.role == role &&
               other.skinIndex == skinIndex &&
               other.username.Equals(this.username);
    }

    public NetPlayerData(NetString user, ulong id, PlayerRole r, int skin = 0)
    {
        username = user;
        clientID = id;
        role = r;
        skinIndex = skin;
    }
}