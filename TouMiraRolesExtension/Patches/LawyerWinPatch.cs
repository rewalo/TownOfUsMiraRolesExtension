using InnerNet;
using MiraAPI.GameEnd;
using TownOfUs.Patches;
using TouMiraRolesExtension.GameOver;
using TouMiraRolesExtension.Roles.Neutral;
using TownOfUs.Utilities;
using TownOfUs;
using TownOfUs.Events;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Modifiers;

namespace TouMiraRolesExtension.Patches;

public static class ExtensionLawyerWinPatch
{
    public static void Register()
    {
        LogicGameFlowPatches.RegisterWinConditionChecker(CheckLawyerWin);
    }

    private static bool CheckLawyerWin(LogicGameFlowNormal instance)
    {
        // Only check win conditions on the host
        if (!AmongUsClient.Instance.AmHost)
        {
            return false;
        }

        // Prevent checking win conditions during death animations or right after deaths
        if (DeathHandlerModifier.IsCoroutineRunning || DeathHandlerModifier.IsAltCoroutineRunning || DeathEventHandlers.IsDeathRecent)
        {
            return false;
        }

        // Prevent checking during revives
        if (AltruistRole.IsReviveInProgress)
        {
            return false;
        }

        // Ensure game has actually started and progressed
        if (AmongUsClient.Instance == null || AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
        {
            return false;
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
                CustomGameOver.Trigger<LawyerGameOver>([lawyer.Player.Data, lawyer.Client.Data]);
                return true; // Game should end
            }
        }

        return false; // Continue checking other win conditions
    }
}