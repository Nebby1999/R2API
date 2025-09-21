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

[AddComponentMenu("")]
public class SurfaceBehaviourHandler : MonoBehaviour
{
    public CharacterBody characterBody { get; private set; }
    public SurfaceBehaviour surfaceBehaviour { get; private set; }
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
        //if the incoming index is invalid then we need to delete the current behaviour.
        if (newIndex == SurfaceDefIndex.Invalid)
        {

            return;
        }
    }
}

public abstract class SurfaceBehaviour
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [MeansImplicitUse]
    public class SurfaceDefAssociation : SearchableAttribute { }
    public SurfaceBehaviourHandler outer { get; internal set; }
    public CharacterBody characterBody => outer.characterBody;

    public virtual void OnEnter() { }
    public virtual void OnExit() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
}
