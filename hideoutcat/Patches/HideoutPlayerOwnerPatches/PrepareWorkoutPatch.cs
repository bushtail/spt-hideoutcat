using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace HideoutCat.Patches.HideoutPlayerOwnerPatches;

public class PrepareWorkoutPatch : ModulePatch
{
    public static event Action? OnPlayerPrepareWorkout;

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(HideoutPlayerOwner), "PrepareWorkout");
    }

    [PatchPostfix]
    private static void Postfix()
    {
        try
        {
            OnPlayerPrepareWorkout!.Invoke();
        }
        catch (Exception ex)
        {
            Plugin.Log!.LogError(ex);
        }
    }
}