using System;
using System.Collections.Generic;
using System.IO;
using AssetBundleLoader;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Hideout;
using HideoutCat.CatData;
using HideoutCat.Patches.AreaScreenSubstratePatches;
using HideoutCat.Patches.BonusPanelPatches;
using HideoutCat.Patches.HideoutControllerPatches;
using HideoutCat.Patches.HideoutPlayerOwnerPatches;
using HideoutCat.Patches.InteractionContextHelperPatches;
using HideoutCat.Pathfinding;
using HideoutCat.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.UnityConverters.Math;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HideoutCat;

[BepInPlugin("com.tarkin.hideoutcat", "hideoutcat", "1.1.0")]
public class Plugin : BaseUnityPlugin
{
    private static ConfigEntry<ECatCoat>? _coat;
    private static ConfigEntry<Color>? _eyeColor;

    internal static ManualLogSource? Log;
    public static Graph? CatGraph;

    private static bool _catSpawned;

    private void Start()
    {
        Log = Logger;

        InitConfiguration();

        if (!LoadCatAreaData())
        {
            return;
        }

        new HideoutAwakePatch().Enable();
        new SelectAreaPatch().Enable();
        new UpdateViewPatch().Enable();
        new PrepareWorkoutPatch().Enable();
        new StopWorkoutPatch().Enable();
        new GetAvailableHideoutActionsPatch().Enable();

        HideoutAwakePatch.OnHideoutAwake += () =>
        {
            _catSpawned = false;
            SpawnCat();
        };

        SelectAreaPatch.OnAreaLevelUpdated += delegate 
        {
            SpawnCat();
        };

        PropManager.Init();
    }

    private void InitConfiguration()
    {
        _coat = Config.Bind(
            "Cat",
            "Coat",
            ECatCoat.Grey,
            "Applies on the next hideout load"
        );

        _eyeColor = Config.Bind(
            "Cat",
            "Eye Colour",
            new Color(0.56f, 0.75f, 0.4f),
            "Applies on the next hideout load"
        );
    }

    private static bool LoadCatAreaData()
    {
        try
        {
            var path = Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                "BepInEx",
                "plugins",
                "tarkin-HideoutCat",
                "bundles",
                "CatNodeGraph.json"
            );

            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new Vector3Converter());

            var nodes = JsonConvert.DeserializeObject<List<Node>>(File.ReadAllText(path));
            
            if (nodes == null) { throw new NullReferenceException(); }

            foreach (var node in nodes)
            {
                foreach (var connectedName in node.connectedToNamesForSerialization!)
                {
                    Node? target = null;
                    foreach (var n in nodes)
                    {
                        if (n.name != connectedName) { continue; }

                        target = n;
                        break;
                    }
                    if (target != null)
                    {
                        node.connectedTo.Add(target);
                    }
                    else
                    {
                        Log!.LogWarning(
                            $"Node '{node.name}': Connected node name '{connectedName}' not found in deserialized nodes."
                        );
                    }
                }

                node.connectedToNamesForSerialization = null;
            }

            CatGraph = new Graph(nodes);
            return true;
        }
        catch (Exception ex)
        {
            Log!.LogError("error loading cat config file: " + ex);
            return false;
        }
    }

    private static bool RequirementsMet()
    {
        AreaData? areaData = null;
        foreach (var x in Singleton<HideoutRepresentation>.Instance.AreaDatas)
        {
            if (x.Template.Type != EAreaType.Kitchen) { continue; }

            areaData = x;
            break;
        }

        return areaData is { CurrentLevel: > 0 };
    }

    private static void SpawnCat()
    {
        if (_catSpawned)
            return;

        if (!RequirementsMet())
            return;

        _catSpawned = true;

        var bundle = BundleLoader.LoadAssetBundle("hideoutcat");
        var prefab = bundle?.LoadAsset<GameObject>("hideoutcat");

        var catObj = Instantiate(prefab);
        if (!catObj)
        {
            throw new NullReferenceException();
        }

        BundleLoader.ReplaceShadersToNative(catObj!);

        var renderer = catObj!.GetComponentInChildren<SkinnedMeshRenderer>();
        renderer.materials[1].color = _eyeColor!.Value;

        if (_coat!.Value != (ECatCoat)_coat.DefaultValue)
        {
            var texName = "MAINTEX_" + _coat.Value.ToString().ToUpper();
            var coatTex = bundle?.LoadAsset<Texture2D>(texName);

            if (coatTex)
            {
                renderer.materials[0].mainTexture = coatTex;
            }
            else
            {
                Log!.LogError($"Error loading {_coat.Value} coat texture");
            }
        }

        var cat = catObj.AddComponent<Cat>();

        var availableAreas = new List<AreaData>();
        foreach (var a in Singleton<HideoutRepresentation>.Instance.AreaDatas)
        {
            if (a.CurrentLevel > 0)
            {
                availableAreas.Add(a);
            }
        }

        if (availableAreas.Count > 0)
        {
            Log!.LogInfo($"{availableAreas.Count} avaiable areas");

            Random.InitState((int)DateTime.Now.Ticks);
            for (var i = availableAreas.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (availableAreas[i], availableAreas[j]) = (availableAreas[j], availableAreas[i]);
            }

            foreach (var area in availableAreas)
            {
                var deadEnds = CatGraph!.FindDeadEndNodesByAreaTypeAndLevel(
                    area.Template.Type,
                    area.CurrentLevel
                );

                if (deadEnds.Count <= 0)
                {
                    continue;
                }

                var chosen = deadEnds[Random.Range(0, deadEnds.Count)];
                cat.transform.position = CatGraph.GetNodeClosestWaypoint(chosen.position)!.position;
                cat.SetTargetNode(chosen);
                return;
            }
        }

        Log!.LogInfo("No available areas, defaulting to a random waypoint node");

        var fallback = CatGraph!.GetNodeClosestWaypoint(
            new Vector3(Random.value * 16f, 0f, 0f)
        );

        cat.transform.position = fallback!.position;
        cat.SetTargetNode(fallback);
    }
}