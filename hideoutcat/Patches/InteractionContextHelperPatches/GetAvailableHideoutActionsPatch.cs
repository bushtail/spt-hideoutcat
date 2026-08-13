using System.Reflection;
using EFT;
using EFT.UI;
using HarmonyLib;
using HideoutCat.CatData;
using HideoutCat.Utils;
using SPT.Reflection.Patching;

namespace HideoutCat.Patches.InteractionContextHelperPatches;

public class GetAvailableHideoutActionsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(InteractionContextHelper),
            "GetAvailableHideoutActions"
        );
    }

    [PatchPrefix]
    private static bool Prefix(ref AvailableInteractionState __result, HideoutPlayerOwner owner, IInteractive interactive)
    {
        var cat = interactive as Cat;

        if (!cat)
        {
            return true;
        }

        __result = InteractionStateUtils.GetCatAvailableActions(cat!, owner);
        return false;
    }
}