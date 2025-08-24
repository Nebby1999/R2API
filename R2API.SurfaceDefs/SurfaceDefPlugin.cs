using BepInEx;
using BepInEx.Logging;

namespace R2API;

[BepInPlugin(SurfaceDefBehaviour.PluginGUID, SurfaceDefBehaviour.PluginName, SurfaceDefBehaviour.PluginVersion)]
public sealed class SurfaceDefPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

    private void OnDestroy()
    {
        SurfaceDefBehaviour.UnsetHooks();
    }
}
