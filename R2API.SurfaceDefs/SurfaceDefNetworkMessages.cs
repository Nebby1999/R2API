using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace R2API;

//Takes care of syncing the indices between the authority machine and the other clients
internal class SyncSurfaceIndices : INetMessage
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

    public SyncSurfaceIndices()
    {

    }
}

/// <summary>
/// Class that contains extensions for networking surface defs
/// </summary>
public static class SurfaceDefNetworkExtensions
{
    /// <summary>
    /// Writes the given <paramref name="index"/> as an <see cref="int"/> to the NetworkWriter
    /// </summary>
    /// <param name="writer">The NetworkWriter</param>
    /// <param name="index">The index to write</param>
    public static void Write(this NetworkWriter writer, SurfaceDefIndex index)
    {
        writer.Write((int)index);
    }

    /// <summary>
    /// Reads an <see cref="int"/> from the <paramref name="reader"/>, casting it into <see cref="SurfaceDefIndex"/>
    /// </summary>
    /// <param name="reader">The reader</param>
    /// <returns>The SurfaceDefIndex that has been read</returns>
    public static SurfaceDefIndex ReadSurfaceIndex(this NetworkReader reader)
    {
        return (SurfaceDefIndex)reader.ReadInt32();
    }
}
