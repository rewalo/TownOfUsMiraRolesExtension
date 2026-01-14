using HarmonyLib;
using MiraAPI.GameEnd;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMiraRolesExtension.GameOver;
using TouMiraRolesExtension.Modifiers;
using TouMiraRolesExtension.Options.Roles.Neutral;
using TouMiraRolesExtension.Roles.Neutral;
using TouMiraRolesExtension.Utilities;
using TownOfUs.GameOver;
using TownOfUs.Utilities;

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

        if (!AmongUsClient.Instance || !AmongUsClient.Instance.AmHost)
        {
            return true;
        }


        if (OptionGroupSingleton<LawyerOptions>.Instance.WinMode != LawyerWinMode.StealWin)
        {
            return true;
        }


        if (InProgress || endReason == CustomGameOver.GameOverReason<LawyerGameOver>())
        {
            return true;
        }


        if (endReason == CustomGameOver.GameOverReason<HostGameOver>())
        {
            return true;
        }


        if (endReason == CustomGameOver.GameOverReason<DrawGameOver>())
        {
            return true;
        }

        var exiled = ExileController.Instance?.initData?.networkedPlayer?.Object;

        var winners = new HashSet<NetworkedPlayerInfo>();
        foreach (var lawyerPc in PlayerControl.AllPlayerControls)
        {
            if (lawyerPc == null || !lawyerPc.IsRole<LawyerRole>())
            {
                continue;
            }

            if (lawyerPc.HasDied() || lawyerPc.Data == null || exiled == lawyerPc)
            {
                continue;
            }

            var client = LawyerUtils.FindClientForLawyer(lawyerPc.PlayerId);
            if (client == null || client.HasDied() || client.Data == null || exiled == client)
            {
                continue;
            }
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
        return false;
    }
}