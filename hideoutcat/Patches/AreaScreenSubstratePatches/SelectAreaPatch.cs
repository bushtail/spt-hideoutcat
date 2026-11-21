using System;
using System.Collections.Generic;
using System.Reflection;
using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace HideoutCat.Patches.AreaScreenSubstratePatches;

public class SelectAreaPatch : ModulePatch
{
    public static event Action<AreaData>? OnAreaSelected;
    public static event Action<AreaData>? OnAreaLevelUpdated;

    private static readonly Dictionary<AreaData, Action> UnsubscribeActions;

    static SelectAreaPatch()
    {
        OnAreaSelected = null;
        OnAreaLevelUpdated = null;
        UnsubscribeActions = new Dictionary<AreaData, Action>();
    }

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(AreaScreenSubstrate), "SelectArea");
    }

    [PatchPostfix]
    private static void Postfix(AreaData areaData)
    {
        if (!UnsubscribeActions.ContainsKey(areaData))
        {
            var unsubscribe = areaData.LevelUpdated.Subscribe(delegate
            {
                try
                {
                    OnAreaLevelUpdated?.Invoke(areaData);
                }
                catch (Exception ex)
                {
                    Plugin.Log!.LogError(ex);
                }
            });

            UnsubscribeActions[areaData] = unsubscribe;
        }

        try
        {
            OnAreaSelected?.Invoke(areaData);
        }
        catch (Exception ex)
        {
            Plugin.Log!.LogError(ex);
        }
    }
}