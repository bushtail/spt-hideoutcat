using AssetBundleLoader;
using Comfort.Common;
using EFT;
using EFT.Hideout;
using HideoutCat.Patches.AreaScreenSubstratePatches;
using HideoutCat.Patches.HideoutControllerPatches;
using UnityEngine;

namespace HideoutCat.Utils;

public static class PropManager
{
    private static GameObject? _herring;

    public static void Init()
    {
        HideoutAwakePatch.OnHideoutAwake += UpdateProps;

        SelectAreaPatch.OnAreaLevelUpdated += _ =>
        {
            UpdateProps();
        };
    }

    private static void UpdateProps()
    {
        HideUnwantedSceneObjects();
        LoadProps();
    }

    private static void LoadProps()
    {
        AreaData? area = null;
        foreach (var x in Singleton<HideoutRepresentation>.Instance.AreaDatas)
        {
            if (x.Template.Type != EAreaType.Kitchen) { continue; }

            area = x;
            break;
        }

        if (area == null) { return; }

        if (!_herring)
        {
            var bundle = BundleLoader.LoadAssetBundle("hideoutcat_props");
            _herring = Object.Instantiate(bundle!.LoadAsset<GameObject>("herring_opened"));
            BundleLoader.ReplaceShadersToNative(_herring);
        }

        _herring!.SetActive(area.CurrentLevel > 0);
        _herring.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);

        _herring.transform.position = area.CurrentLevel switch
        {
            1 => new Vector3(5.5347f, 0.848f, -5.6833f),
            2 or 3 => new Vector3(5.432f, 0.759f, -4.9755f),
            _ => _herring.transform.position
        };
    }

    private static void HideUnwantedSceneObjects()
    {
        AreaData? heatingArea = null;
        foreach (var x in Singleton<HideoutRepresentation>.Instance.AreaDatas)
        {
            if (x.Template.Type != EAreaType.Heating) { continue; }

            heatingArea = x;
            break;
        }

        if (heatingArea != null)
        {
            switch (heatingArea.CurrentLevel)
            {
                case 1:
                {
                    Disable(heatingArea.HighlightTransform.Find("books_01 (1)"));
                    break;
                }

                case 2:
                {
                    Disable(heatingArea.HighlightTransform.Find("books_01 (2)"));
                    break;
                }

                case 3:
                {
                    Disable(heatingArea.HighlightTransform.Find("paper3 (1)"));
                    Disable(heatingArea.HighlightTransform.Find("paper3 (2)"));
                    Disable(heatingArea.HighlightTransform.Find("Firewood_4 (7)"));
                    Disable(heatingArea.HighlightTransform.Find("Firewood_4 (6)"));
                    break;
                }
            }
        }

        AreaData? kitchenArea = null;
        foreach (var areaData in Singleton<HideoutRepresentation>.Instance.AreaDatas)
        {
            if (areaData.Template.Type != EAreaType.Kitchen) { continue; }

            kitchenArea = areaData;
            break;
        }

        if (kitchenArea == null) { return; }

        switch (kitchenArea.CurrentLevel)
        {
            case 1:
            {
                Disable(kitchenArea.HighlightTransform.Find("dish_1"));
                break;
            }

            case 2:
            {
                Disable(kitchenArea.HighlightTransform.Find("dish_1 (1)"));
                Disable(kitchenArea.HighlightTransform.Find("fork (1)"));
                break;
            }

            case 3:
            {
                Disable(kitchenArea.HighlightTransform.Find("dish_1 (4)"));
                Disable(kitchenArea.HighlightTransform.Find("fork (2)"));
                break;
            }
        }
    }

    private static void Disable(Transform transform)
    {
        if (transform)
        {
            transform.gameObject.SetActive(false);
        }
    }
}