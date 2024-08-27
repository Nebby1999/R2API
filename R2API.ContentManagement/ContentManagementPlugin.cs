using System.Collections.Generic;
using System.Reflection;
using System;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Logging;
using RoR2.ContentManagement;
using System.Collections;
using System.Linq;
using UnityObject = UnityEngine.Object;

[assembly: InternalsVisibleTo("R2API.Items")]
[assembly: InternalsVisibleTo("R2API.Elites")]
[assembly: InternalsVisibleTo("R2API.Unlockable")]
[assembly: InternalsVisibleTo("R2API.TempVisualEffect")]
[assembly: InternalsVisibleTo("R2API.Loadout")]
[assembly: InternalsVisibleTo("R2API.Sound")]
[assembly: InternalsVisibleTo("R2API.Stages")]
[assembly: InternalsVisibleTo("R2API.Prefab")]
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace R2API.ContentManagement;

[BepInPlugin(R2APIContentManagement.PluginGUID, R2APIContentManagement.PluginName, R2APIContentManagement.PluginVersion)]
internal sealed class ContentManagementPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;

        R2APIContentManagement.Init();
    }

    private void OnEnable()
    {
        On.RoR2.ContentManagement.ContentManager.SetContentPacks += EnsureUniqueNames;
    }

    private void OnDisable()
    {
        On.RoR2.ContentManagement.ContentManager.SetContentPacks -= EnsureUniqueNames;
    }

    private static void EnsureUniqueNames(On.RoR2.ContentManagement.ContentManager.orig_SetContentPacks orig, List<ReadOnlyContentPack> newContentPacks)
    {
        if (newContentPacks.Count > 1)
        {
            IEnumerable<PropertyInfo> allReadOnlyNamedAssetCollectionProperty = GetAllAssetCollectionPropertiesOfAReadOnlyContentPack();

            // Compare each content pack with each others for any potential duplicate asset names
            for (int i = 0; i < newContentPacks.Count - 1; i++)
            {
                for (int j = i + 1; j < newContentPacks.Count; j++)
                {

                    var firstContentPack = newContentPacks[i];
                    var secondContentPack = newContentPacks[j];

                    var isFirstContentPackVanilla = IsVanillaContentPack(firstContentPack);
                    var isSecondContentPackVanilla = IsVanillaContentPack(secondContentPack);

                    foreach (var assetCollectionProperty in allReadOnlyNamedAssetCollectionProperty)
                    {
                        var firstAssetCollection = ((IEnumerable)assetCollectionProperty.GetValue(firstContentPack)).Cast<UnityObject>();
                        var secondAssetCollection = ((IEnumerable)assetCollectionProperty.GetValue(secondContentPack)).Cast<UnityObject>();

                        var firstAssetIndex = 0;
                        foreach (var firstAsset in firstAssetCollection)
                        {
                            var secondAssetIndex = 0;
                            foreach (var secondAsset in secondAssetCollection)
                            {
                                var differentReferences = firstAsset != secondAsset;
                                if (differentReferences)
                                {
                                    ChangeAssetNameIfNeeded(firstContentPack, firstAsset, ref firstAssetIndex,
                                        secondContentPack, secondAsset, ref secondAssetIndex,
                                        isFirstContentPackVanilla, isSecondContentPackVanilla);
                                }
                                else
                                {
                                    ContentManagementPlugin.Logger.LogError($"The exact same asset {firstAsset} is being added by two different content packs : {firstContentPack.identifier} and {secondContentPack.identifier}");
                                    // Todo, try removing it from the non-vanilla contentPack, lot of annoying code to write that I cant bother writing right now
                                }
                            }
                        }
                    }
                }
            }
        }

        orig(newContentPacks);
    }

    private static void ChangeAssetNameIfNeeded(ReadOnlyContentPack firstContentPack, UnityObject firstAsset, ref int firstAssetIndex,
        ReadOnlyContentPack secondContentPack, UnityObject secondAsset, ref int secondAssetIndex,
        bool isFirstContentPackVanilla, bool isSecondContentPackVanilla)
    {
        if (firstAsset.name.Equals(secondAsset.name, StringComparison.InvariantCulture))
        {
            if (isFirstContentPackVanilla)
            {
                ChangeAssetName(secondContentPack, ref secondAssetIndex, secondAsset, firstContentPack);
            }
            else if (isSecondContentPackVanilla)
            {
                ChangeAssetName(firstContentPack, ref firstAssetIndex, firstAsset, secondContentPack);
            }
        }
    }

    private static bool IsVanillaContentPack(ReadOnlyContentPack contentPack)
    {
        return contentPack.identifier.StartsWith("RoR2.");
    }

    private static void ChangeAssetName(ReadOnlyContentPack changingContentPack, ref int assetIndex, UnityObject changingAsset, ReadOnlyContentPack notChangingContentPack)
    {
        var newName = $"{changingContentPack.identifier}_{changingAsset.name}_{assetIndex++}";

        ContentManagementPlugin.Logger.LogWarning($"Asset name from {changingContentPack.identifier} is conflicting with {notChangingContentPack.identifier}. " +
            $"Old name : {changingAsset.name}, new name : {newName}");
        changingAsset.name = newName;
    }

    private static IEnumerable<PropertyInfo> GetAllAssetCollectionPropertiesOfAReadOnlyContentPack()
    {
        const BindingFlags allFlags = (BindingFlags)(-1);

        var allReadOnlyNamedAssetCollectionProperty =
            typeof(ReadOnlyContentPack).GetProperties(allFlags).
                Where(
                    p => p.PropertyType.GenericTypeArguments.Length > 0 &&
                        (p.PropertyType.GenericTypeArguments[0].IsSubclassOf(typeof(UnityObject)) ||
                            typeof(UnityObject) == p.PropertyType.GenericTypeArguments[0]));

        return allReadOnlyNamedAssetCollectionProperty;
    }
}
