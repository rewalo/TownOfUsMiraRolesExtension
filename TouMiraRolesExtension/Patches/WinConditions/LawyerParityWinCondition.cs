using MiraAPI.GameEnd;
using TownOfUs.Interfaces;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TouMiraRolesExtension.GameOver;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Roles.Neutral;
using TouMiraRolesExtension.Utilities;
using MiraAPI.Utilities;

namespace TouMiraRolesExtension.Patches.WinConditions;

/// <summary>
/// "Vanilla parity" protection:
/// If there are exactly 3 players alive and 2 of them are an alive Lawyer + their alive Client,
/// and there are no roles that should prevent an end (neutral killers / game halters / mixed killer conflicts),
/// end the game as a Lawyer win.
/// </summary>
public sealed class LawyerParityWinCondition : IWinCondition, IWinConditionWithBlocking
{
    // Run after neutral (5) and lovers (10), but before crew/impostor style checks.
    public int Priority => 12;

    public bool BlocksOthers => true;

    private static bool IsKillerClient(PlayerControl client)
    {
        // "Killer client" means a parity scenario where the Lawyer+Client duo should be able to close out the game.
        // We intentionally do NOT treat crewmate-killers (e.g. Sheriff) as "killer clients" here.
        return client != null && (client.IsImpostorAligned() || client.Is(RoleAlignment.NeutralKilling));
    }

    public bool IsMet(LogicGameFlowNormal gameFlow)
    {
        // Only the host can decide game end.
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
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

        // This is specifically to avoid "final 3" stalling against vanilla parity.
        // If there isn't at least one impostor alive, a normal win should handle this (and be stolen via RpcEndGame).
        if (MiscUtils.ImpAliveCount <= 0)
        {
            return false;
        }

        // Respect TownOfUs "continue game" blockers / continuing roles.
        if (MiscUtils.NKillersAliveCount > 0)
        {
            return false;
        }

        if (MiscUtils.GameHaltersAliveCount > 0)
        {
            return false;
        }

        // If any crewmate-killers are alive (e.g. Sheriff), don't force a parity end.
        if (MiscUtils.CrewKillersAliveCount > 0)
        {
            return false;
        }

        // At least one alive Lawyer with an alive Client, and both must be among the 3 alive.
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

            if (!IsKillerClient(client))
            {
                continue;
            }

            if (!alivePlayers.Any(ap => ap.PlayerId == lawyerPc.PlayerId) ||
                !alivePlayers.Any(ap => ap.PlayerId == client.PlayerId))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public void TriggerGameOver(LogicGameFlowNormal gameFlow)
    {
        // Only the host can decide game end.
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
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

            if (!IsKillerClient(client))
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