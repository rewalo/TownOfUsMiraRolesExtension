using HarmonyLib;
using Hazel;
using TouMiraRolesExtension.Modules;

namespace TouMiraRolesExtension.Patches;

[HarmonyPatch]
public static class ForestallerSabotageBlockPatches
{
    [HarmonyPatch(typeof(SabotageSystemType), nameof(SabotageSystemType.UpdateSystem))]
    [HarmonyPrefix]
    public static bool SabotageSystemTypeUpdateSystemPrefix(SabotageSystemType __instance, [HarmonyArgument(0)] PlayerControl player,
        [HarmonyArgument(1)] MessageReader reader)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            return true;
        }

        if (PlayerControl.AllPlayerControls.Count <= 1)
        {
            return true;
        }

        if (player == null || player.Data == null || player.Data.Disconnected)
        {
            return true;
        }

        if (!ForestallerSystem.AnyActiveForestallerAlive())
        {
            return true;
        }


        if (reader == null || reader.BytesRemaining != 1)
        {
            return true;
        }

        if (reader == null || reader.Buffer == null || reader.Buffer.Length == 0)
        {
            return true;
        }

        var idx = reader.readHead;
        if (idx < 0 || idx >= reader.Buffer.Length)
        {
            idx = reader.Offset;
        }

        if (idx < 0 || idx >= reader.Buffer.Length)
        {
            idx = reader.Position;
        }

        if (idx < 0 || idx >= reader.Buffer.Length)
        {
            return true;
        }

        var amount = reader.Buffer[idx];

        if (IsBlockedGlobalSabotage((SystemTypes)amount))
        {
            if (__instance != null && __instance.Timer < 30f)
            {
                __instance.Timer = 30f;
            }

            return false;
        }

        return true;
    }

    private static bool IsBlockedGlobalSabotage(SystemTypes system)
    {
        return system is SystemTypes.LifeSupp
            or SystemTypes.Reactor
            or SystemTypes.Electrical
            or SystemTypes.Comms
            or SystemTypes.Laboratory
            or SystemTypes.HeliSabotage
            or SystemTypes.MushroomMixupSabotage
            or SystemTypes.Sabotage;
    }
}