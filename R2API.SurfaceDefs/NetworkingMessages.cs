using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Networking;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace R2API;
internal static class NetworkingMessages
{
    /// <summary>
    /// Adds a behaviour to the specified body on all machines.
    /// </summary>
    public class AddOrEnableBehaviourMessage : INetMessage
    {
        public SurfaceDefIndex indexWeAreOn;
        public CharacterBody body;
        public void Deserialize(NetworkReader reader)
        {
            body = reader.ReadNetworkIdentity().GetComponent<CharacterBody>();
            indexWeAreOn = reader.ReadSurfaceDefIndex();
        }

        public void OnReceived()
        {
            //SurfaceBehaviorHandler.AddOrEnableBehaviour(indexWeAreOn, body);
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(body.networkIdentity);
            writer.WriteSurfaceDefIndex(indexWeAreOn);
        }
    }

    public static void WriteSurfaceDefIndex(this NetworkWriter writer, SurfaceDefIndex surfaceDefIndex)
    {
        writer.Write((int)surfaceDefIndex);
    }

    public static SurfaceDefIndex ReadSurfaceDefIndex(this NetworkReader reader)
    {
        return (SurfaceDefIndex)reader.ReadInt32();
    }
}
