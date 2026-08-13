using System.Reflection;
using EFT;
using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;
using TMPro;

namespace HideoutCat.Patches.BonusPanelPatches;

public class UpdateViewPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BonusPanel), nameof(BonusPanel.UpdateView));
    }

    [PatchPostfix]
    private static void Postfix(BonusPanel __instance)
    {
        var bonusField = AccessTools.Field(typeof(BonusPanel), "_bonus");
        var descField = AccessTools.Field(typeof(BonusPanel), "_description");
        var effectField = AccessTools.Field(typeof(BonusPanel), "_effect");

        var bonus = bonusField?.GetValue(__instance) as Bonus;
        var description = descField?.GetValue(__instance) as TextMeshProUGUI;
        var effect = effectField?.GetValue(__instance) as TextMeshProUGUI;

        if (bonus == null || !description || !effect) { return; }

        if (bonus.Id.ToString() != "64f5b9e5fa34f11b380756d6") { return; }

        description!.text = "Unlocks cat";
        effect!.text = string.Empty;
    }
}