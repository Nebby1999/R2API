using R2API.ScriptableObjects;
using RoR2.ContentManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using R2API.Utils;
using R2API.AutoVersionGen;

namespace R2API.ContentManagement;

#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class R2APIContentManagement
{
    public const string PluginGUID = R2API.PluginGUID + ".content_management";
    public const string PluginName = R2API.PluginName + ".ContentManagement";

    private static Dictionary<string, (ContentPack, IContentPackProvider)> _identifierToProvider = new Dictionary<string, (ContentPack, IContentPackProvider)>();
    public static IContentPackProvider CreateContentPackProvider(R2APISerializableContentPack r2apiSerializableContentPack)
    {
        var identifier = r2apiSerializableContentPack.name;
        if(_identifierToProvider.ContainsKey(identifier))
        {
            //Log something
        }

        var contentPack = r2apiSerializableContentPack.GetOrCreateContentPack();
        var provider = new ContentProvider
        {
            contentPack = contentPack
        };

        _identifierToProvider[identifier] = (contentPack, provider);
        return provider;
    }

    public static IContentPackProvider CreateContentPackProvider(ContentPack contentPack)
    {
        var identifier = contentPack.identifier;
        if (_identifierToProvider.ContainsKey(identifier))
        {
            //Log something
        }

        var provider = new ContentProvider
        {
            contentPack = contentPack
        };
        _identifierToProvider[identifier] = (contentPack, provider);
        return provider;
    }

    public static IContentPackProvider CreateContentPackProvider(string identifier, out ContentPack contentPack)
    {
        if (_identifierToProvider.ContainsKey(identifier))
        {
            //Log something
        }

        contentPack = new ContentPack();
        contentPack.identifier = identifier;
        var provider = new ContentProvider
        {
            contentPack = contentPack
        };
        _identifierToProvider[identifier] = (contentPack, provider);
        return provider;
    }


    internal static void Init()
    {
        ContentManager.collectContentPackProviders += ContentManager_collectContentPackProviders;
    }

    private static void ContentManager_collectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
    {
        foreach(var kvp in _identifierToProvider)
        {
            string identifier = kvp.Key;
            IContentPackProvider provider = kvp.Value.Item2;

            // Log adding i guess

            addContentPackProvider(provider);
        }
    }

    internal class ContentProvider : IContentPackProvider
    {
        public string identifier => contentPack.identifier;
        public ContentPack contentPack;

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            args.ReportProgress(1f);
            ContentPack.Copy(contentPack, args.output);
            yield break;
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }
    }
}
