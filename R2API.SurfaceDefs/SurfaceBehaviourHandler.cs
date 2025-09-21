using HG.Reflection;
using JetBrains.Annotations;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API;

/// <summary>
/// A Component that runs the SurfaceBehaviour of the Surface the body is on.
/// <br></br>
/// You should not add this component directly! it's automatically added by the plugin to any Characterbody that has a CharacterMotor.
/// <para></para>
/// To add a new behaviour, look at <see cref="SurfaceBehaviour"/>
/// </summary>
[AddComponentMenu("")]
public class SurfaceBehaviourHandler : MonoBehaviour
{
    /// <summary>
    /// The CharacterBody that's attached to this handler, cannot be null
    /// </summary>
    public CharacterBody characterBody { get; private set; }

    /// <summary>
    /// The currently executing SurfaceBehaviour, may be null
    /// </summary>
    public SurfaceBehaviour surfaceBehaviour { get; private set; }

    /// <summary>
    /// The current index we're sitting on, can be <see cref="SurfaceDefIndex.Invalid"/>
    /// </summary>
    public SurfaceDefIndex currentIndex { get; private set; }

    private void Awake()
    {
        characterBody = GetComponent<CharacterBody>();
    }

    private void Update()
    {
        surfaceBehaviour?.Update();
    }

    private void FixedUpdate()
    {
        surfaceBehaviour?.FixedUpdate();
    }

    private void OnDestroy()
    {
        surfaceBehaviour?.OnExit();
    }

    //This gets called by the SyncSurfaceIndices message, networking the surface change so the machine that has authority on the body tells everyone else what surface they're on.
    internal void OnSurfaceChanged(SurfaceDefIndex newIndex)
    {
        //Same index? do nothing.
        if (newIndex == currentIndex)
            return;

        Type incomingBehaviour = SurfaceBehaviourManager.GetBehaviourType(newIndex);
        Type activeBehaviourType = SurfaceBehaviourManager.GetBehaviourType(currentIndex);

        currentIndex = newIndex;

        //Same behaviour? do nothing.
        if (incomingBehaviour == activeBehaviourType)
            return;

        SurfaceBehaviour newBehaviour = null;
        if(incomingBehaviour != null)
        {
            newBehaviour = (SurfaceBehaviour)Activator.CreateInstance(incomingBehaviour);
        }

        surfaceBehaviour?.OnExit();
        newBehaviour?.outer = this;
        surfaceBehaviour = newBehaviour;
        surfaceBehaviour?.OnEnter();
    }
}

/// <summary>
/// The base class from which all Surface Behaviours inherit from. You can inherit from this class and then use the attribute <see cref="SurfaceDefAssociation"/> to create new behaviours. Networking which behaviour is currently active handled by the Submodule but the SurfaceBehaviour itself is not a unity object.
/// <br></br>
/// In case you need networking on your surface behaviour, consider utilizing a NetworkedBodyAttachment.
/// <code>
/// public class TestBehaviour : SurfaceBehaviour
/// {
///     private static SurfaceDef GetSurfaceDef()
///     {
///         return MyAssets.SurfaceDefs.mySurfaceDef;
///     }
///
///     public override OnEnter()
///     {
///         if(NetworkServer.active &#38;&#38; characterBody &#38;&#38; characterBody.healthComponent)
///         {
///             characterBody.healthComponent.TakeDamage(new DamageInfo
///             {
///                 damage = 10
///             });
///         }
///     }
/// }
/// </code>
/// </summary>
public abstract class SurfaceBehaviour
{
    /// <summary>
    /// An Attribute that's used to demark that a specific SurfaceBehaviour is attached to a specific SurfaceDef, similar to the base game <see cref="RoR2.Items.BaseItemBodyBehavior.ItemDefAssociationAttribute"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [MeansImplicitUse]
    public class SurfaceDefAssociation : SearchableAttribute { }

    /// <summary>
    /// The SurfaceBehaviourHandler thats executing the current behaviour
    /// </summary>
    public SurfaceBehaviourHandler outer { get; internal set; }

    /// <summary>
    /// The CharacterBody that's on the surface
    /// </summary>
    public CharacterBody characterBody => outer.characterBody;

    /// <summary>
    /// Called when the SurfaceBehaviour is first assigned.
    /// </summary>
    public virtual void OnEnter() { }

    /// <summary>
    /// Called when the SurfaceBehaviour is being unassigned, this method is also called from <see cref="SurfaceBehaviourHandler"/>'s OnDestroy method.
    /// </summary>
    public virtual void OnExit() { }

    /// <summary>
    /// Called every Update
    /// </summary>
    public virtual void Update() { }

    /// <summary>
    /// Called every Fixed Update
    /// </summary>
    public virtual void FixedUpdate() { }
}
