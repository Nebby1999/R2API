using BepInEx;
using R2API.AutoVersionGen;
using RoR2.ContentManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace R2API.ContentManagement;

public static class ContentManagementExtensions
{
    public static void Add<T>(this NamedAssetCollection<T> assetCollection, T asset)
    {
        string name = assetCollection.nameProvider(asset);
        string backupName = $"{asset.GetType().Name}_{assetCollection.Count}";

        bool assetInCollection = assetCollection.assetToName.ContainsKey(asset);
        if (assetInCollection)
        {
            return;
        }

        if (name.IsNullOrWhiteSpace())
        {
            ContentManagementPlugin.Logger.LogWarning($"Asset {asset} does not have a valid name! ({name}). assigning a generic name...");
            name = backupName;
        }

        if (assetCollection.nameToAsset.ContainsKey(name))
        {
            ContentManagementPlugin.Logger.LogWarning($"Asset {asset} cant be added because an asset with the name \"{name}\" is already registered. Using a generic name.");
                name = backupName;
        }

        HG.ArrayUtils.ArrayAppend(ref assetCollection.assetInfos, new NamedAssetCollection<T>.AssetInfo
        {
            asset = asset,
            assetName = name
        });
        assetCollection.nameToAsset[name] = asset;
        assetCollection.assetToName[asset] = name;

        Array.Sort(assetCollection.assetInfos);
    }
}
