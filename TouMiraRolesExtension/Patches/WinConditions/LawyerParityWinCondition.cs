using MiraAPI.GameEnd;
using TownOfUs.Interfaces;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TouMiraRolesExtension.GameOver;
using TouMiraRolesExtension.Roles.Neutral;
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
        return PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p != null && !p.HasDied() && p.IsRole<LawyerRole>())
            .Select(p => p.GetRole<LawyerRole>())
            .Any(l => l != null &&
                      l.Player != null && !l.Player.HasDied() &&
                      l.Client != null && !l.Client.HasDied() &&
                      IsKillerClient(l.Client) &&
                      alivePlayers.Any(ap => ap.PlayerId == l.Player.PlayerId) &&
                      alivePlayers.Any(ap => ap.PlayerId == l.Client.PlayerId));
    }

    public void TriggerGameOver(LogicGameFlowNormal gameFlow)
    {
        var alivePlayers = Helpers.GetAlivePlayers();

        // Collect all alive lawyers whose client is also alive (and both are among the 3 alive).
        var winningLawyers = PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p != null && !p.HasDied() && p.IsRole<LawyerRole>())
            .Select(p => p.GetRole<LawyerRole>())
            .Where(l => l != null &&
                        l.Player != null && !l.Player.HasDied() &&
                        l.Client != null && !l.Client.HasDied() &&
                        IsKillerClient(l.Client) &&
                        l.Player.Data != null &&
                        l.Client.Data != null &&
                        alivePlayers.Any(ap => ap.PlayerId == l.Player.PlayerId) &&
                        alivePlayers.Any(ap => ap.PlayerId == l.Client.PlayerId))
            .ToList();

        if (winningLawyers.Count == 0)
        {
            return;
        }

        var winners = new HashSet<NetworkedPlayerInfo>();
        foreach (var lawyer in winningLawyers)
        {
            lawyer!.AboutToWin = true;
            winners.Add(lawyer.Player!.Data);
            winners.Add(lawyer.Client!.Data);
        }

        if (winners.Count < 2)
        {
            return;
        }

        CustomGameOver.Trigger<LawyerGameOver>(winners.ToArray());
    }
}
