using System.Reflection;
using EFT;
using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;
using TMPro;

namespace HideoutCat.Patches.BonusPanelPatches;

public class UpdateViewPatch : ModulePatch
{
    private static readonly FieldInfo? BonusField = AccessTools.Field(typeof(BonusPanel), "_bonus");
    private static readonly FieldInfo? DescField = AccessTools.Field(typeof(BonusPanel), "_description");
    private static readonly FieldInfo? EffectField = AccessTools.Field(typeof(BonusPanel), "_effect");

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BonusPanel), nameof(BonusPanel.UpdateView));
    }

    [PatchPostfix]
    private static void Postfix(BonusPanel __instance)
    {
        var bonus = BonusField?.GetValue(__instance) as Bonus;
        var description = DescField?.GetValue(__instance) as TextMeshProUGUI;
        var effect = EffectField?.GetValue(__instance) as TextMeshProUGUI;

        if (bonus == null || !description || !effect) { return; }

        if (bonus.Id.ToString() != "64f5b9e5fa34f11b380756d6") { return; }

        description!.text = "Unlocks cat";
        effect!.text = string.Empty;
    }
}