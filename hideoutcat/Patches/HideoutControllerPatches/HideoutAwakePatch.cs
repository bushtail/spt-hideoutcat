using System;
using System.Reflection;
using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace HideoutCat.Patches.HideoutControllerPatches;

public class HideoutAwakePatch : ModulePatch
{
    public static event Action? OnHideoutAwake;

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(HideoutController), "HideoutAwake");
    }

    [PatchPostfix]
    private static void Postfix(HideoutController __instance)
    {
        OnHideoutAwake!.Invoke();
    }
}