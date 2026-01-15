using MiraAPI.GameEnd;
using MiraAPI.GameOptions;
using MiraAPI.Utilities;
using TouMiraRolesExtension.GameOver;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Options.Roles.Neutral;
using TouMiraRolesExtension.Roles.Neutral;
using TouMiraRolesExtension.Utilities;
using TownOfUs.Interfaces;
using TownOfUs.Modules;
using TownOfUs.Roles;
using TownOfUs.Utilities;

namespace TouMiraRolesExtension.Patches.WinConditions;

/// <summary>
/// "Vanilla parity" protection:
/// If there are exactly 3 players alive and 2 of them are an alive Lawyer + their alive Client,
/// and there are no roles that should prevent an end (neutral killers / game halters / mixed killer conflicts),
/// end the game as a Lawyer win.
/// </summary>
public sealed class LawyerParityWinCondition : IWinCondition, IWinConditionWithBlocking
{

    public int Priority => 12;

    public bool BlocksOthers => true;

    private static bool IsKillerClient(PlayerControl client)
    {
        return client != null && (client.IsImpostorAligned() || client.Is(RoleAlignment.NeutralKilling));
    }

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
        if (alivePlayers.Count != 3)
        {
            return false;
        }



        if (MiscUtils.ImpAliveCount <= 0)
        {
            return false;
        }


        if (MiscUtils.NKillersAliveCount > 0)
        {
            return false;
        }

        if (MiscUtils.GameHaltersAliveCount > 0)
        {
            return false;
        }


        if (MiscUtils.CrewKillersAliveCount > 0)
        {
            return false;
        }


        foreach (var lawyerPc in PlayerControl.AllPlayerControls)
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

            if (!IsKillerClient(client))
            {
                continue;
            }

            var alivePlayerIds = alivePlayers.Select(ap => ap.PlayerId).ToHashSet();
            if (!alivePlayerIds.Contains(lawyerPc.PlayerId) || !alivePlayerIds.Contains(client.PlayerId))
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
        foreach (var lawyerPc in PlayerControl.AllPlayerControls)
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

            if (!IsKillerClient(client))
            {
                continue;
            }

            if (lawyerPc.Data == null || client.Data == null)
            {
                continue;
            }

            var alivePlayerIds = alivePlayers.Select(ap => ap.PlayerId).ToHashSet();
            if (!alivePlayerIds.Contains(lawyerPc.PlayerId) || !alivePlayerIds.Contains(client.PlayerId))
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