using HarmonyLib;
using MiraAPI.GameEnd;
using TownOfUs.GameOver;
using TownOfUs.Utilities;
using TouMiraRolesExtension.GameOver;
using TouMiraRolesExtension.Roles.Neutral;

namespace TouMiraRolesExtension.Patches;

/// <summary>
/// Win-stealing neutral:
/// If any Lawyer and their Client are both alive at the moment the game would end (non-draw),
/// steal the win and end the game as a Lawyer win (Lawyer + Client are the winners).
/// </summary>
[HarmonyPatch(typeof(GameManager), nameof(GameManager.RpcEndGame))]
public static class LawyerStealWinPatch
{
    private static bool InProgress { get; set; }

    [HarmonyPrefix]
    public static bool Prefix(GameOverReason endReason)
    {
        // Only the host can decide game end.
        if (!AmongUsClient.Instance || !AmongUsClient.Instance.AmHost)
        {
            return true;
        }

        // Prevent recursion when LawyerGameOver triggers its own end-game RPC.
        if (InProgress || endReason == CustomGameOver.GameOverReason<LawyerGameOver>())
        {
            return true;
        }

        // Draws aren't a "win" to steal.
        if (endReason == CustomGameOver.GameOverReason<DrawGameOver>())
        {
            return true;
        }

        // If someone is currently being exiled, treat them as dead for steal purposes.
        // This prevents edge-cases where the exiled player hasn't had Data.IsDead set yet
        // when RpcEndGame fires (e.g. Lawyer being ejected).
        var exiled = ExileController.Instance?.initData?.networkedPlayer?.Object;

        var winningLawyers = PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p != null && p.IsRole<LawyerRole>())
            .Select(p => p.GetRole<LawyerRole>())
            .Where(l => l != null &&
                        l.Player != null && !l.Player.HasDied() &&
                        l.Client != null && !l.Client.HasDied() &&
                        l.Player.Data != null &&
                        l.Client.Data != null &&
                        exiled != l.Player &&
                        exiled != l.Client)
            .ToList();

        if (winningLawyers.Count == 0)
        {
            return true;
        }

        var winners = new HashSet<NetworkedPlayerInfo>();
        foreach (var lawyer in winningLawyers)
        {
            lawyer!.AboutToWin = true;
            winners.Add(lawyer.Player!.Data);
            winners.Add(lawyer.Client!.Data);
        }

        // Sanity: LawyerGameOver expects at least Lawyer + Client.
        if (winners.Count < 2)
        {
            return true;
        }

        try
        {
            InProgress = true;
            CustomGameOver.Trigger<LawyerGameOver>(winners.ToArray());
        }
        finally
        {
            InProgress = false;
        }

        // Cancel the original win.
        return false;
    }
}