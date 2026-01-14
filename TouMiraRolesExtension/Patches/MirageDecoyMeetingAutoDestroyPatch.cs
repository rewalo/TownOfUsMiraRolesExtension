using HarmonyLib;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Roles.Crewmate;

namespace TouMiraRolesExtension.Patches;


[HarmonyPatch]
public static class MirageDecoyLifecyclePatches
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    public static void MeetingStartPostfix()
    {



        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc == null || pc.Data?.Role is not MirageRole)
            {
                continue;
            }

            if (!pc.AmOwner)
            {
                continue;
            }

            if (MirageDecoySystem.HasAny(pc.PlayerId))
            {
                MirageRole.RpcMirageDestroyDecoy(pc);
            }
        }
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    [HarmonyPostfix]
    public static void GameStartPostfix()
    {
        MirageDecoySystem.ClearAll();
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.RpcEndGame))]
    [HarmonyPostfix]
    public static void EndGamePostfix()
    {
        MirageDecoySystem.ClearAll();
    }
}