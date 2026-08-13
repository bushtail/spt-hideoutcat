using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AssetBundleLoader;

public static class BundleLoader
{
    public static AssetBundle? LoadAssetBundle(string filename)
    {
        var directoryName = Path.GetDirectoryName(Application.dataPath);
        var text = Path.Combine(AddPathToApplicationDataPath, filename);
        var text2 = Path.Combine(directoryName!, text);
        var flag = LoadedAssetBundles.TryGetValue(text2, out var assetBundle);
        AssetBundle? assetBundle2;
        if (flag)
        {
            assetBundle2 = assetBundle;
        }
        else
        {
            var assetBundle3 = AssetBundle.LoadFromFile(text2);
            var flag2 = !assetBundle3;
            if (flag2)
            {
                assetBundle2 = null;
            }
            else
            {
                LoadedAssetBundles.Add(text2, assetBundle3);
                assetBundle2 = assetBundle3;
            }
        }
        return assetBundle2;
    }

    public static void ReplaceShadersToNative(GameObject gameObject)
    {
        var componentsInChildren = gameObject.GetComponentsInChildren<Renderer>();
        foreach (var renderer in componentsInChildren)
        {
            foreach (var material in renderer.materials)
            {
                var shader = Shader.Find(material.shader.name);
                bool flag = shader;
                if (flag)
                {
                    material.shader = shader;
                }
            }
        }
    }

    private static readonly Dictionary<string, AssetBundle> LoadedAssetBundles = new();

    private static readonly string AddPathToApplicationDataPath = Path.Combine("BepInEx", "plugins", "tarkin-HideoutCat", "bundles");
}
