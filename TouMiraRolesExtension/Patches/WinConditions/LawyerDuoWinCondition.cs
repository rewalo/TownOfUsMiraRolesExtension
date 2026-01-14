using MiraAPI.GameEnd;
using TownOfUs.Interfaces;
using TownOfUs.Utilities;
using TouMiraRolesExtension.GameOver;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Roles.Neutral;
using TouMiraRolesExtension.Utilities;
using MiraAPI.Utilities;
using MiraAPI.GameOptions;
using TouMiraRolesExtension.Options.Roles.Neutral;
using TownOfUs.Modules;
using TownOfUs.Roles;

namespace TouMiraRolesExtension.Patches.WinConditions;

/// <summary>
/// "Final 2" protection:
/// If the only two living players are an alive Lawyer and their alive Client, end the game.
/// This matches the intended "survivor-like" behavior where two specific players must be alive together,
/// and also bypasses any "continues game" stalling logic.
/// </summary>
public sealed class LawyerDuoWinCondition : IWinCondition, IWinConditionWithBlocking
{
    // Run after most neutral conditions, but before crew/impostor style checks.
    public int Priority => 12;

    public bool BlocksOthers => true;

    private static bool ClientHasWonAlone(PlayerControl client)
    {
        if (client == null || client.HasDied())
        {
            return false;
        }

        var clientRole = client.GetRoleWhenAlive();
        if (clientRole is ITownOfUsRole townOfUsRole)
        {
            return townOfUsRole.WinConditionMet();
        }

        return false;
    }

    public bool IsMet(LogicGameFlowNormal gameFlow)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
        {
            return false;
        }

        if (OptionGroupSingleton<LawyerOptions>.Instance.WinMode != LawyerWinMode.StealWin)
        {
            return false;
        }

        if (LawyerWinConditionState.Triggered)
        {
            return false;
        }

        var alivePlayers = Helpers.GetAlivePlayers();
        if (alivePlayers.Count != 2)
        {
            return false;
        }

        foreach (var lawyerPc in PlayerControl.AllPlayerControls.ToArray())
        {
            if (lawyerPc == null || lawyerPc.HasDied() || !lawyerPc.IsRole<LawyerRole>())
            {
                continue;
            }

            var client = LawyerUtils.FindClientForLawyer(lawyerPc.PlayerId);
            if (client == null || client.HasDied())
            {
                continue;
            }

            if (!alivePlayers.Any(ap => ap.PlayerId == lawyerPc.PlayerId) ||
                !alivePlayers.Any(ap => ap.PlayerId == client.PlayerId))
            {
                continue;
            }

            if (ClientHasWonAlone(client))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public void TriggerGameOver(LogicGameFlowNormal gameFlow)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        if (OptionGroupSingleton<LawyerOptions>.Instance.WinMode != LawyerWinMode.StealWin)
        {
            return;
        }

        if (LawyerWinConditionState.Triggered)
        {
            return;
        }

        var alivePlayers = Helpers.GetAlivePlayers();

        var winners = new HashSet<NetworkedPlayerInfo>();

        foreach (var lawyerPc in PlayerControl.AllPlayerControls.ToArray())
        {
            if (lawyerPc == null || lawyerPc.HasDied() || !lawyerPc.IsRole<LawyerRole>())
            {
                continue;
            }

            var client = LawyerUtils.FindClientForLawyer(lawyerPc.PlayerId);
            if (client == null || client.HasDied())
            {
                continue;
            }

            if (lawyerPc.Data == null || client.Data == null)
            {
                continue;
            }

            if (!alivePlayers.Any(ap => ap.PlayerId == lawyerPc.PlayerId) ||
                !alivePlayers.Any(ap => ap.PlayerId == client.PlayerId))
            {
                continue;
            }

            var lawyerRole = lawyerPc.GetRole<LawyerRole>();
            if (lawyerRole != null)
            {
                lawyerRole.AboutToWin = true;
            }

            winners.Add(lawyerPc.Data);
            winners.Add(client.Data);
        }

        if (winners.Count < 2)
        {
            return;
        }

        LawyerWinConditionState.MarkTriggered();
        CustomGameOver.Trigger<LawyerGameOver>(winners.ToArray());
    }
}