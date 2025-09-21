using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace R2API;
public class SyncSurfaceIndices : INetMessage
{
    public NetworkIdentity networkIdentity;
    public SurfaceDefIndex newIndex;

    public void Deserialize(NetworkReader reader)
    {
        networkIdentity = reader.ReadNetworkIdentity();
        newIndex = reader.ReadSurfaceIndex();
    }

    public void OnReceived()
    {
        if(networkIdentity.TryGetComponent<SurfaceBehaviourHandler>(out var handler))
        {
            handler.OnSurfaceChanged(newIndex);
        }
    }

    public void Serialize(NetworkWriter writer)
    {
        writer.Write(networkIdentity);
        writer.Write(newIndex);
    }

    public SyncSurfaceIndices(CharacterBody body, SurfaceDefIndex newIndex)
    {
        this.networkIdentity = body.networkIdentity;
        this.newIndex = newIndex;
    }
}

public static class SurfaceDefNetworkExtensions
{
    public static void Write(this NetworkWriter writer, SurfaceDefIndex index)
    {
        writer.Write((int)index);
    }

    public static SurfaceDefIndex ReadSurfaceIndex(this NetworkReader reader)
    {
        return (SurfaceDefIndex)reader.ReadInt32();
    }
}
