using System;
using Unity.Collections;
using Unity.Netcode;

public struct NetPlayerData : INetworkSerializable, IEquatable<NetPlayerData>
{
    public NetString username;
    public ulong clientID;
    public bool isSurvivor;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if(serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out username);
            reader.ReadValueSafe(out clientID);
            reader.ReadValueSafe(out isSurvivor);
        } else {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(username);
            writer.WriteValueSafe(clientID);
            writer.WriteValueSafe(isSurvivor);
        }
    }

    public bool Equals(NetPlayerData other)
    {
        return other.Equals(this);
    }

    // Constructor
    public NetPlayerData(NetString user,  ulong id, bool survivor)
    {
        username = user;
        clientID = id;
        isSurvivor = survivor;
    }
}