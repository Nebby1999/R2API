using HG.Reflection;
using JetBrains.Annotations;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

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
        surfaceBehaviour.FixedUpdate();
    }

    private void OnDestroy()
    {
        surfaceBehaviour.OnExit();
    }

    internal void OnSurfaceChanged(SurfaceDefIndex newIndex)
    {

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
