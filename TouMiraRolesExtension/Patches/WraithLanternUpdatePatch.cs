using HarmonyLib;
using TouMiraRolesExtension.Modules;

namespace TouMiraRolesExtension.Patches;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class WraithLanternUpdatePatch
{
    public static void Postfix()
    {
    }
}