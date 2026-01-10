using HarmonyLib;
using MiraAPI.Modifiers;
using TouMiraRolesExtension.Modifiers;

namespace TouMiraRolesExtension.Patches;

[HarmonyPatch(typeof(LogicOptions), nameof(LogicOptions.GetPlayerSpeedMod))]
public static class WraithSpeedPatch
{
    public static void Postfix(PlayerControl pc, ref float __result)
    {
        if (pc == null)
        {
            return;
        }

        if (pc.TryGetModifier<WraithDashModifier>(out var dash) && dash.TimerActive)
        {
            __result *= dash.SpeedFactor;
        }
    }
}
