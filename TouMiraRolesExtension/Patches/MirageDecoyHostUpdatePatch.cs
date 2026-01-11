using HarmonyLib;
using TouMiraRolesExtension.Modules;

namespace TouMiraRolesExtension.Patches;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
public static class MirageDecoyHostUpdatePatch
{
    [HarmonyPostfix]
    public static void FixedUpdatePostfix()
    {
        MirageDecoySystem.UpdateHost();
    }
}