using HG;
using R2API.AutoVersionGen;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace R2API;


#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class SurfaceBehaviorHandler
{
    public const string PluginGUID = R2API.PluginGUID + ".surfacedefs";
    public const string PluginName = R2API.PluginName + ".SurfaceDefs";
    private static Type[] _surfaceIndexToBehaviorType = Array.Empty<Type>();
    private static Dictionary<UnityObjectWrapperKey<CharacterBody>, Dictionary<SurfaceDefIndex, SurfaceBehavior>> _bodyToSurfaceBehaviourDictionary = new Dictionary<UnityObjectWrapperKey<CharacterBody>, Dictionary<SurfaceDefIndex, SurfaceBehavior>>();
    private static Dictionary<UnityObjectWrapperKey<CharacterBody>, SurfaceBehavior> _bodyToActiveBehavior = new Dictionary<UnityObjectWrapperKey<CharacterBody>, SurfaceBehavior>();

    [SystemInitializer(typeof(SurfaceDefCatalog))]
    private static IEnumerator Init()
    {
        List<SurfaceBehavior.SurfaceDefAssociation> surfaceDefAssociations = new List<SurfaceBehavior.SurfaceDefAssociation>();
        HG.Reflection.SearchableAttribute.GetInstances(surfaceDefAssociations);

        _surfaceIndexToBehaviorType = new Type[SurfaceDefCatalog.surfaceDefs.Length];
        Type surfaceBehaviorType = typeof(SurfaceBehavior);
        Type surfaceDefType = typeof(SurfaceDef);
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

            _surfaceIndexToBehaviorType[(int)surfaceDef.surfaceDefIndex] = type;
        }
    }

    public static bool TryGetBehaviorType(SurfaceDef sd, out Type behaviorType)
    {
        behaviorType = _surfaceIndexToBehaviorType[(int)sd.surfaceDefIndex];
        return !(behaviorType is null);
    }
    public static bool TryGetBehaviorType(SurfaceDefIndex sdi, out Type behaviorType)
    {
        behaviorType = _surfaceIndexToBehaviorType[(int)sdi];
        return !(behaviorType is null);
    }

    public static void HandleSurfaceContact(CharacterBody body, SurfaceDef sd)
    {
        //If there is no surface to handle, hand it over to HandleSurfaceExit
        if (sd is null) HandleSurfaceExit(body);

        //If there is no body, return
        if (body.bodyIndex == BodyIndex.None || !body)
            return;

        //If the incoming surface has the same behavior as the currently active surface, return
        SurfaceDefIndex sdi = sd.surfaceDefIndex;
        Type incoming;
        bool incomingHasBehavior = TryGetBehaviorType(sdi, out incoming);
        Type active = _bodyToActiveBehavior[body]?.GetType();
        if (incoming == active)
            return;

        //Try to grab behavior of incoming surface. If it has no behavior, disable currently active behavior (if there is one) and return  
        if (!incomingHasBehavior)
        {
            if (!(active is null))
            {
                _bodyToActiveBehavior[body].enabled = false;
                _bodyToActiveBehavior[body] = null;
            }
            return;
        }

        //Create new surface behavior if there isn't one on the body already
        if (!_bodyToSurfaceBehaviourDictionary.ContainsKey(body))
            _bodyToSurfaceBehaviourDictionary[body] = new Dictionary<SurfaceDefIndex, SurfaceBehavior>();

        var bodySurfaceBehaviors = _bodyToSurfaceBehaviourDictionary[body];
        SurfaceBehavior newBehavior;
        if (bodySurfaceBehaviors.ContainsKey(sdi))
            newBehavior = bodySurfaceBehaviors[sdi];
        else
        {
            newBehavior = (SurfaceBehavior)body.gameObject.AddComponent(_surfaceIndexToBehaviorType[(int)sdi]);
            newBehavior.surfaceIndex = sdi;
            newBehavior.body = body;
            bodySurfaceBehaviors.Add(sdi, newBehavior);
        }

        //Replace current active behavior with the incoming behavior
        if (!(active is null))
            _bodyToActiveBehavior[body].enabled = false;
        _bodyToActiveBehavior[body] = newBehavior;
        newBehavior.enabled = true;
    }

    public static void HandleSurfaceExit(CharacterBody body)
    {
        //If there is no body, return
        if (body.bodyIndex == BodyIndex.None || !body)
            return;

        //If the currently active behavior isn't null, disable it
        if (!(_bodyToActiveBehavior[body] is null))
        {
            _bodyToActiveBehavior[body].enabled = false;
            _bodyToActiveBehavior[body] = null;
        }
    }

    public static void HandleBodyAwake(CharacterBody body)
    {
        _bodyToActiveBehavior.Add(body, null);
    }

    public static void HandleBodyDestroy(CharacterBody body)
    {
        if (_bodyToSurfaceBehaviourDictionary.ContainsKey(body))
            _bodyToSurfaceBehaviourDictionary.Remove(body);
        if (_bodyToActiveBehavior.ContainsKey(body))
            _bodyToActiveBehavior.Remove(body);
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
