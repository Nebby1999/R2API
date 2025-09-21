using HG;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.AutoVersionGen;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API;

/*
 * This entire class is a bit fucky, but basically the concept of a SurfaceDefBehaviour, while useful, cannot really exist on its own. This is because for players movement is controlled by the 
 * Client machine, not the server. The client sends updates to the server on where it is.
 * Due to this, all the movement, including the surfaceDef handling, is handled on the CharacterMotor under authority, which for enemies its the server and for players its mostly the clients.
 * As a result, this class needs to handle its own networking via custom messages. In scenarios where the body is not player controlled we know its an enemy, therefore we need to send
 * a message to the clients. If the body is player controlled then we need to decide how to send the update to the other machines.
 * 
 * Turns out the "inLava" boolean of characterBody wasnt that jank at all, easiest form to do it if anything.
 */

/// <summary>
/// Class for the handler, you shouldn't use this directly.
/// </summary>
#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class SurfaceBehaviourManager
{
    public const string PluginGUID = R2API.PluginGUID + ".surfacedefs";
    public const string PluginName = R2API.PluginName + ".SurfaceDefs";

    private static Type[] _surfaceIndexToBehaviour = null;
    private static bool _hooksSet = false;

    private static void SetHooks()
    {
        if (_hooksSet)
            return;

        _hooksSet = true;

        IL.RoR2.CharacterMotor.OnGroundHit += CharacterMotor_OnGroundHit;

        IL.RoR2.CharacterMotor.OnLeaveStableGround += CharacterMotor_OnLeaveStableGround;
    }

    private static void UnsetHooks()
    {
        if (!_hooksSet)
            return;

        _hooksSet = false;

        IL.RoR2.CharacterMotor.OnGroundHit -= CharacterMotor_OnGroundHit;

        IL.RoR2.CharacterMotor.OnLeaveStableGround -= CharacterMotor_OnLeaveStableGround;
    }

    [SystemInitializer(typeof(SurfaceDefCatalog), typeof(BodyCatalog))]
    private static IEnumerator OnSurfaceDefCatalogInit()
    {
        foreach(var body in BodyCatalog.allBodyPrefabs)
        {
            if(!body.TryGetComponent<CharacterMotor>(out var motor))
            {
                continue;
            }
            body.AddComponent<SurfaceBehaviourHandler>();
        }


        SurfaceDefPlugin.Logger.LogMessage($"Allocating a Type array of {SurfaceDefCatalog.surfaceDefs.Length} length for SurfaceDefBehaviours");
        _surfaceIndexToBehaviour = new Type[SurfaceDefCatalog.surfaceDefs.Length];
        List<SurfaceBehaviour.SurfaceDefAssociation> surfaceDefAssociations = new List<SurfaceBehaviour.SurfaceDefAssociation>();
        HG.Reflection.SearchableAttribute.GetInstances(surfaceDefAssociations);

        Type surfaceBehaviourType = typeof(SurfaceBehaviour);
        Type surfaceDefType = typeof(SurfaceDef);
        bool anyAdded = false;
        foreach(var association in surfaceDefAssociations)
        {
            yield return null;
            MethodInfo methodInfo = (MethodInfo)association.target;
            if (!methodInfo.IsStatic)
                continue;

            var type = methodInfo.DeclaringType;
            if (!surfaceBehaviourType.IsAssignableFrom(type))
                continue;

            if (type.IsAbstract)
                continue;

            if (surfaceDefType.IsAssignableFrom(methodInfo.ReturnType))
                continue;

            if (methodInfo.GetGenericArguments().Length != 0)
                continue;

            SurfaceDef surfaceDef = (SurfaceDef)methodInfo.Invoke(null, Array.Empty<object>());
            if (!surfaceDef)
                continue;

            if (surfaceDef.surfaceDefIndex == SurfaceDefIndex.Invalid)
                continue;

            _surfaceIndexToBehaviour[(int)surfaceDef.surfaceDefIndex] = type;
            anyAdded = true;
        }

        if(anyAdded)
        {
            SetHooks();
        }
    }

    private static void CharacterMotor_OnLeaveStableGround(MonoMod.Cil.ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchCallvirt<CharacterBody>(nameof(CharacterBody.SetInLava))))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, typeof(CharacterMotor).GetField(nameof(CharacterMotor.body), BindingFlags.Instance | BindingFlags.NonPublic));
            c.EmitDelegate(HandleSurfaceExit);
        }
        else
        {
            SurfaceDefPlugin.Logger.LogError($"Failed to apply {nameof(CharacterMotor_OnLeaveStableGround)} hook");
        }
    }

    private static void CharacterMotor_OnGroundHit(MonoMod.Cil.ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchBrfalse(out _)))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, typeof(CharacterMotor).GetField(nameof(CharacterMotor.body), BindingFlags.Instance | BindingFlags.NonPublic));
            c.Emit(OpCodes.Ldloc, 0);
            c.EmitDelegate(HandleSurfaceContact);
        }
        else
        {
            SurfaceDefPlugin.Logger.LogError($"Failed to apply {nameof(CharacterMotor_OnGroundHit)} hook");
        }
    }

    //Note: Only machines with authority should sync values up.
    private static void HandleSurfaceContact(CharacterBody body, SurfaceDef sd)
    {
        if (!body || body.bodyIndex == BodyIndex.None)
            return;

        if (!body.hasEffectiveAuthority)
            return;

        if (!body.TryGetComponent<SurfaceBehaviourHandler>(out var behaviourHandler))
            return;

        SurfaceDefIndex newIndex = SurfaceDefIndex.Invalid;
        //Some surfaces in the game have no surface def, so just change the surface to an invalid one.
        if (sd)
            newIndex = sd.surfaceDefIndex;

        SendMessageAcrossNetwork(body, newIndex);
        behaviourHandler.OnSurfaceChanged(newIndex);
    }

    private static void HandleSurfaceExit(CharacterBody body)
    {
        if (!body || body.bodyIndex == BodyIndex.None)
            return;

        if (!body.hasEffectiveAuthority)
            return;

        if (!body.TryGetComponent<SurfaceBehaviourHandler>(out var behaviourHandler))
            return;

        //When the surface is exited it means we should kill the behaviour we have currently.
        SurfaceDefIndex newIndex = SurfaceDefIndex.Invalid;
        SendMessageAcrossNetwork(body, newIndex);
        behaviourHandler.OnSurfaceChanged(newIndex);
    }

    /*
     * This method is always ran on the Authority machine that owns the characterBody, as such we need to network it properly.
     * 
     * Case 1: NetworkServer is Active.
     * This means that the characterBody is either a Monster, or a Player that's currently hosting the game. In this case we need to sync it to the Clients.
     * 
     * Case 2: NetworkServer is NOT active.
     * This means that the characterBody is a Client that's playing a game, as a result we need to sync it to the other clients (if any), and the server.
    */
    private static void SendMessageAcrossNetwork(CharacterBody targetBody, SurfaceDefIndex newIndex)
    {
        NetworkDestination destination;
        if (NetworkServer.active)
        {
            destination = NetworkDestination.Clients;
        }
        else
        {
            destination = NetworkDestination.Clients | NetworkDestination.Server;
        }
        new SyncSurfaceIndices(targetBody, newIndex).Send(destination);
    }
}
