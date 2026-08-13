using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace HideoutCat.Patches.HideoutPlayerOwnerPatches;

public class StopWorkoutPatch : ModulePatch
{
    public static event Action? OnPlayerStopWorkout;

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(HideoutPlayerOwner), "StopWorkout");
    }

    [PatchPostfix]
    private static void Postfix()
    {
        try
        {
            OnPlayerStopWorkout!.Invoke();
        }
        catch (Exception ex)
        {
            Plugin.Log!.LogError(ex);
        }
    }
}