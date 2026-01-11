using AmongUs.GameOptions;
using HarmonyLib;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Roles.Crewmate;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMiraRolesExtension.Patches;


[HarmonyPatch]
public static class MirageDecoyInteractionPatches
{
    private static bool TryTriggerFromLocalPlayer(float maxDistance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.HasDied() || MeetingHud.Instance)
        {
            return false;
        }

        var from = local.GetTruePosition();
        if (!MirageDecoySystem.TryGetClosestDecoy(from, maxDistance, out var mirageId, out var decoyPos))
        {
            return false;
        }

        var mirage = MiscUtils.PlayerById(mirageId);
        if (mirage == null || mirage.HasDied() || !mirage.IsRole<MirageRole>())
        {
            return false;
        }

        MirageRole.RpcMirageTriggerDecoy(mirage, local, decoyPos);
        return true;
    }
    private static float GetKillDistance()
    {
        var opts = GameOptionsManager.Instance?.currentNormalGameOptions;
        if (opts == null)
        {
            return 1.0f;
        }

        var killDistances = opts.GetFloatArray(FloatArrayOptionNames.KillDistances);
        var idx = Math.Clamp(opts.KillDistance, 0, killDistances.Length - 1);
        return killDistances[idx];
    }

    [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    public static bool KillButtonDoClickPrefix()
    {
        if (!TryTriggerFromLocalPlayer(GetKillDistance()))
        {
            return true;
        }

        try
        {
            var local = PlayerControl.LocalPlayer;
            if (local != null)
            {
                local.SetKillTimer(local.GetKillCooldown());
            }
        }
        catch
        {
            // ignore
        }

        return false;
    }
}