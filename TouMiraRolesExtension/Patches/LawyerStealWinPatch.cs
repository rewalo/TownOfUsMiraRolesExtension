using HarmonyLib;
using MiraAPI.GameEnd;
using TownOfUs.GameOver;
using TownOfUs.Utilities;
using TouMiraRolesExtension.GameOver;
using TouMiraRolesExtension.Modifiers;
using TouMiraRolesExtension.Roles.Neutral;
using TouMiraRolesExtension.Utilities;
using MiraAPI.Modifiers;

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

        // Never steal forced host-abort end-games (host keybind).
        if (endReason == CustomGameOver.GameOverReason<HostGameOver>())
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

        var winners = new HashSet<NetworkedPlayerInfo>();
        foreach (var lawyerPc in PlayerControl.AllPlayerControls.ToArray())
        {
            if (lawyerPc == null || !lawyerPc.IsRole<LawyerRole>())
            {
                continue;
            }

            if (lawyerPc.HasDied() || lawyerPc.Data == null || exiled == lawyerPc)
            {
                continue;
            }

            // Find the client via replicated modifier to avoid relying on l.Client (which can be null/desynced).
            var client = LawyerUtils.FindClientForLawyer(lawyerPc.PlayerId);
            if (client == null || client.HasDied() || client.Data == null || exiled == client)
            {
                continue;
            }

            // Sanity: ensure the client is actually marked as this lawyer's client.
            if (!client.HasModifier<LawyerTargetModifier>(m => m.OwnerId == lawyerPc.PlayerId))
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