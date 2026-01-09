using HarmonyLib;
using MiraAPI.Modifiers;
using TouMiraRolesExtension.Modifiers;

namespace TouMiraRolesExtension.Patches;

/// <summary>
/// Prevents Serial Killer from reporting bodies when they have the NoReport modifier.
/// </summary>
[HarmonyPatch]
public static class SerialKillerReportBlockPatch
{
    [HarmonyPatch(typeof(ReportButton), nameof(ReportButton.DoClick))]
    [HarmonyPriority(Priority.First)]
    [HarmonyPrefix]
    public static bool ReportButtonDoClickPrefix(ActionButton __instance)
    {
        if (PlayerControl.LocalPlayer == null)
        {
            return true;
        }

        if (PlayerControl.LocalPlayer.HasModifier<SerialKillerNoReportModifier>())
        {
            return false;
        }

        return true;
    }
}