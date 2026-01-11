using MiraAPI.GameEnd;
using TownOfUs.Interfaces;
using TownOfUs.Utilities;
using TouMiraRolesExtension.GameOver;
using TouMiraRolesExtension.Roles.Neutral;
using MiraAPI.Utilities;

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

    public bool IsMet(LogicGameFlowNormal gameFlow)
    {
        var alivePlayers = Helpers.GetAlivePlayers();
        if (alivePlayers.Count != 2)
        {
            return false;
        }

        return PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p != null && !p.HasDied() && p.IsRole<LawyerRole>())
            .Select(p => p.GetRole<LawyerRole>())
            .Any(l => l != null &&
                      l.Player != null && !l.Player.HasDied() &&
                      l.Client != null && !l.Client.HasDied() &&
                      alivePlayers.Any(ap => ap.PlayerId == l.Player.PlayerId) &&
                      alivePlayers.Any(ap => ap.PlayerId == l.Client.PlayerId));
    }

    public void TriggerGameOver(LogicGameFlowNormal gameFlow)
    {
        var alivePlayers = Helpers.GetAlivePlayers();

        var winners = new HashSet<NetworkedPlayerInfo>();

        var winningLawyers = PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p != null && !p.HasDied() && p.IsRole<LawyerRole>())
            .Select(p => p.GetRole<LawyerRole>())
            .Where(l => l != null &&
                        l.Player != null && !l.Player.HasDied() &&
                        l.Client != null && !l.Client.HasDied() &&
                        l.Player.Data != null &&
                        l.Client.Data != null &&
                        alivePlayers.Any(ap => ap.PlayerId == l.Player.PlayerId) &&
                        alivePlayers.Any(ap => ap.PlayerId == l.Client.PlayerId))
            .ToList();

        foreach (var lawyer in winningLawyers)
        {
            winners.Add(lawyer!.Player!.Data);
            winners.Add(lawyer.Client!.Data);
        }

        if (winners.Count < 2)
        {
            return;
        }

        CustomGameOver.Trigger<LawyerGameOver>(winners.ToArray());
    }
}