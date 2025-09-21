using BepInEx;
using BepInEx.Logging;
using R2API.Networking;

#if DEBUG
[assembly: HG.Reflection.SearchableAttribute.OptIn]
#endif

namespace R2API;

/// <summary>
/// Surface Behaviour Plugin
/// </summary>
[BepInPlugin(SurfaceBehaviourManager.PluginGUID, SurfaceBehaviourManager.PluginName, SurfaceBehaviourManager.PluginVersion)]
public sealed class SurfaceDefPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
        NetworkingAPI.RegisterMessageType<SyncSurfaceIndices>();
    }

    private void OnDestroy()
    {
        SurfaceBehaviourManager.UnsetHooks();
    }
}
