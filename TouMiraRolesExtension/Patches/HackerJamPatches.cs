using HarmonyLib;
using TouMiraRolesExtension.Modules;

namespace TouMiraRolesExtension.Patches;

[HarmonyPatch]
public static class HackerJamPatches
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.AreCommsAffected))]
    [HarmonyPostfix]
    public static void PlayerControlAreCommsAffectedPostfix(ref bool __result)
    {
        if (__result)
        {
            return;
        }

        if (HackerSystem.IsJammed)
        {
            __result = true;
        }
    }
}