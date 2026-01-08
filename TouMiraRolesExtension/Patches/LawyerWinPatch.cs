using HarmonyLib;
using MiraAPI.GameEnd;
using TownOfUs.Options;
using TownOfUs.Patches;
using TouMiraRolesExtension.GameOver;
using TouMiraRolesExtension.Roles.Neutral;
using TownOfUs.Utilities;
using MiraAPI.GameOptions;
using TownOfUs;

namespace TouMiraRolesExtension.Patches;

[HarmonyPatch(typeof(LogicGameFlowPatches), nameof(LogicGameFlowPatches.CheckEndCriteriaPatch))]
public static class ExtensionLawyerWinPatch
{
    [HarmonyPrefix]
    public static bool CheckExtensionLawyerWin(LogicGameFlowNormal __instance, ref bool __result)
    {
        if (OptionGroupSingleton<HostSpecificOptions>.Instance.NoGameEnd.Value && TownOfUsPlugin.IsDevBuild)
        {
            return true;
        }

        if (ExileController.Instance)
        {
            return true;
        }

        var winningLawyers = PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p != null && !p.HasDied() && p.IsRole<LawyerRole>())
            .Select(p => p.GetRole<LawyerRole>())
            .Where(l => l != null && l.WinConditionMet() && l.Client != null && !l.Client.HasDied())
            .ToList();

        if (winningLawyers.Count > 0)
        {
            var lawyer = winningLawyers[0];
            if (lawyer.Player != null && lawyer.Player.Data != null && 
                lawyer.Client != null && lawyer.Client.Data != null)
            {
                var client = lawyer.Client;
                var clientRole = client.Data?.Role;
                
                CustomGameOver.Trigger<LawyerGameOver>([lawyer.Player.Data, client.Data]);
                
                __result = false;
                return false;
            }
        }

        return true;
    }
}