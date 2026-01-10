using HarmonyLib;
using MiraAPI.GameEnd;
using TownOfUs.Options;
using TownOfUs.Patches;
using TouMiraRolesExtension.GameOver;
using TouMiraRolesExtension.Roles.Neutral;
using TownOfUs.Utilities;
using MiraAPI.GameOptions;
using TownOfUs;
using TownOfUs.Modifiers;
using TownOfUs.Events;
using TownOfUs.Roles.Crewmate;
using InnerNet;
using MiraAPI.Utilities;

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

        // Don't check win conditions in tutorial
        if (TutorialManager.InstanceExists)
        {
            return true;
        }

        // Only check on host
        if (!AmongUsClient.Instance.AmHost)
        {
            return true;
        }

        // Ensure game data exists
        if (!GameData.Instance)
        {
            return true;
        }

        // Ensure game has started
        if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
        {
            return true;
        }

        if (ExileController.Instance)
        {
            return true;
        }

        // Don't end game if death handlers are running or death is recent
        if (DeathHandlerModifier.IsCoroutineRunning || DeathHandlerModifier.IsAltCoroutineRunning || DeathEventHandlers.IsDeathRecent)
        {
            return true;
        }

        // Don't end game if a revive is in progress
        if (AltruistRole.IsReviveInProgress)
        {
            return true;
        }

        // Don't end game if there are game halters alive with more than 1 player
        if (MiscUtils.GameHaltersAliveCount > 0 && Helpers.GetAlivePlayers().Count > 1)
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
            if (lawyer?.Player != null && lawyer.Player.Data != null && 
                lawyer.Client != null && lawyer.Client.Data != null)
            {
                var client = lawyer.Client;
                
                CustomGameOver.Trigger<LawyerGameOver>([lawyer.Player.Data, client.Data]);
                
                __result = false;
                return false;
            }
        }

        return true;
    }
}