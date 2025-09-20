using HG;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.AutoVersionGen;
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
public static partial class SurfaceBehaviorHandler
{
    public const string PluginGUID = R2API.PluginGUID + ".surfacedefs";
    public const string PluginName = R2API.PluginName + ".SurfaceDefs";
    private static Dictionary<SurfaceDefIndex, Type> _surfaceDefToBehaviourType = new();

    //While i'd like for there to be just one dictionary, CharacterMotor does not keep track of what surface its in, so we need to keep track of that ourselves. which explains the second dictionary.
    //Hopefully saving it as an enum will mean less memory consumed overall.
    private static Dictionary<UnityObjectWrapperKey<CharacterBody>, Dictionary<SurfaceDefIndex, SurfaceBehavior>> _bodyToSurfaceBehaviourDictionary = new();
    private static Dictionary<UnityObjectWrapperKey<CharacterBody>, SurfaceDefIndex> _bodyToCurrentlyStandingIndex = new();
    private static bool _hooksSet = false;

    private static void SetHooks()
    {
        if (_hooksSet)
            return;

        _hooksSet = true;

        CharacterBody.onBodyAwakeGlobal += HandleBodyAwake;

        CharacterBody.onBodyDestroyGlobal += HandleBodyDestroy;

        IL.RoR2.CharacterMotor.OnGroundHit += CharacterMotor_OnGroundHit;

        IL.RoR2.CharacterMotor.OnLeaveStableGround += CharacterMotor_OnLeaveStableGround;
    }

    internal static void UnsetHooks()
    {
        if (!_hooksSet)
            return;

        _hooksSet = false;

        CharacterBody.onBodyAwakeGlobal -= HandleBodyAwake;

        CharacterBody.onBodyDestroyGlobal -= HandleBodyDestroy;

        IL.RoR2.CharacterMotor.OnGroundHit -= CharacterMotor_OnGroundHit;

        IL.RoR2.CharacterMotor.OnLeaveStableGround -= CharacterMotor_OnLeaveStableGround;
    }

    [SystemInitializer(typeof(SurfaceDefCatalog))]
    private static IEnumerator Init()
    {
        List<SurfaceBehavior.SurfaceDefAssociation> surfaceDefAssociations = new List<SurfaceBehavior.SurfaceDefAssociation>();
        HG.Reflection.SearchableAttribute.GetInstances(surfaceDefAssociations);

        Type surfaceBehaviorType = typeof(SurfaceBehavior);
        Type surfaceDefType = typeof(SurfaceDef);
        bool anyAdded = false;
        foreach (var association in surfaceDefAssociations)
        {
            //TODO: add debug logs
            yield return null;
            MethodInfo methodInfo = (MethodInfo)association.target;
            if (!methodInfo.IsStatic)
                continue;

            var type = methodInfo.DeclaringType;
            if (!surfaceBehaviorType.IsAssignableFrom(type))
                continue;

            if (type.IsAbstract)
                continue;

            if (!surfaceDefType.IsAssignableFrom(methodInfo.ReturnType))
                continue;

            if (methodInfo.GetGenericArguments().Length != 0)
                continue;

            SurfaceDef surfaceDef = (SurfaceDef)methodInfo.Invoke(null, Array.Empty<object>());
            if (!surfaceDef)
                continue;

            if (surfaceDef.surfaceDefIndex < 0)
                continue;

            _surfaceDefToBehaviourType[surfaceDef.surfaceDefIndex] = type;
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

    /*
     * When a body comes into contact with a surface the following things need to happen.
     * Keep in mind that character movement happens _under authority_.
     * 
     * Authority side:
     */
    private static void HandleSurfaceContact(CharacterBody body, SurfaceDef sd)
    {
        if (!body || body.bodyIndex == BodyIndex.None)
            return;

        bool hasAuthority = body.hasAuthority;

        //Only the authority machine can know what we've contacted technically. Networking will happen later.
        if(!hasAuthority)
        {
            return;
        }

        //If a body isnt in this dictionary at this point, something is horribly wrong, as all bodies on awake should be added to this dictionary.
        if(!_bodyToSurfaceBehaviourDictionary.TryGetValue(body, out var perBodyBehaviourDictionary))
        {
            return;
        }

        //Some surfaces in the game have no surface def, we'll treat this as the body leaving the current surface.
        if(!sd)
        {
            HandleSurfaceExit(body);
            return;
        }


        SurfaceDefIndex incomingIndex = sd.surfaceDefIndex;

        //Do the two surfaces use the same behaviour? If so, we need to return.
        Type incomingBehaviourType = _surfaceDefToBehaviourType.GetValueOrDefault(incomingIndex);
        SurfaceDefIndex currentlyStandingIndex = _bodyToCurrentlyStandingIndex[body];
        Type activeBehaviourType = _surfaceDefToBehaviourType.GetValueOrDefault(currentlyStandingIndex);

        //Update the currently standing index.
        _bodyToCurrentlyStandingIndex[body] = incomingIndex;
        if (incomingBehaviourType == activeBehaviourType)
            return;

        //If the incoming surface has no behaviour, we need to disable the currently active one. Nothing else needs to be done.
        if (incomingBehaviourType == null)
        {
            if(perBodyBehaviourDictionary.TryGetValue(currentlyStandingIndex, out var surfaceDefBehaviour))
            {
                surfaceDefBehaviour.enabled = false;
                //Send a message that we need to disable the currently standing index.
            }
            return;
        }

        //Try get the new behaviour that needs to be enabled, if it doesnt exist, add it to the body.
        SurfaceBehavior newBehaviour;
        if(!perBodyBehaviourDictionary.TryGetValue(currentlyStandingIndex, out newBehaviour))
        {
            newBehaviour = (SurfaceBehavior)body.gameObject.AddComponent(incomingBehaviourType);
            newBehaviour.surfaceIndex = incomingIndex;
            newBehaviour.body = body;
            perBodyBehaviourDictionary.Add(currentlyStandingIndex, newBehaviour);
        }

        //Enable it, in case the new behaviour already existed.
        newBehaviour.enabled = true;
    }

    private static void HandleSurfaceExit(CharacterBody body)
    {

        if (!body || body.bodyIndex == BodyIndex.None)
            return;

        //Only run if the machine has authority on the body. networking happens after.
        bool hasAuthority = body.hasAuthority;
        if(!hasAuthority)
        {
            return;
        }

        SurfaceDefIndex index = SurfaceDefIndex.Invalid;
        //The character motor doesnt save any data regarding what surfaceDef its in, so we need to get the index we have currently for this body.
        if(_bodyToCurrentlyStandingIndex.TryGetValue(body, out index))
        {
            _bodyToCurrentlyStandingIndex[body] = SurfaceDefIndex.Invalid;
        }

        //Try to get the body's behaviour via it's internal dictionary to disable it..
        if (_bodyToSurfaceBehaviourDictionary.TryGetValue(body, out var behaviourDictionary))
        {
            if (behaviourDictionary.TryGetValue(index, out var behaviour))
            {
                behaviour.enabled = false;
            }
        }

        //Send over the network that we need to disable the behaviour on the other machines.
        if(hasAuthority && NetworkServer.active)
        {
            //Send from server to the other clients
        }
        else if(hasAuthority && !NetworkServer.active)
        {
            //Send from client to the other clients, and the server.
        }
    }

    //Add a new entry to the dictionary, for later use.
    private static void HandleBodyAwake(CharacterBody body)
    {
        _bodyToCurrentlyStandingIndex.Add(body, SurfaceDefIndex.Invalid);
        _bodyToSurfaceBehaviourDictionary.Add(body, new Dictionary<SurfaceDefIndex, SurfaceBehavior>());
    }

    //Remove the entry from the dictionary, body destruction should cause the other behaviours to be destroyed as well.
    private static void HandleBodyDestroy(CharacterBody body)
    {
        if(_bodyToCurrentlyStandingIndex.ContainsKey(body))
            _bodyToCurrentlyStandingIndex.Remove(body);

        if (_bodyToSurfaceBehaviourDictionary.ContainsKey(body))
            _bodyToSurfaceBehaviourDictionary.Remove(body);
    }
}
public class SurfaceBehavior : MonoBehaviour
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SurfaceDefAssociation : HG.Reflection.SearchableAttribute
    {
    }

    public SurfaceDefIndex surfaceIndex { get; internal set; }
    public CharacterBody body { get; internal set; }


    private void OnEnable()
    {
    }
    private void OnDisable()
    {
    }
}
