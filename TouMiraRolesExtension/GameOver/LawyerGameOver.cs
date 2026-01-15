using MiraAPI.GameEnd;
using MiraAPI.Utilities;
using Reactor.Utilities.Extensions;
using TouMiraRolesExtension.Roles.Neutral;
using TouMiraRolesExtension.Utilities;
using TownOfUs;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMiraRolesExtension.GameOver;

public sealed class LawyerGameOver : CustomGameOver
{
    public override bool VerifyCondition(PlayerControl playerControl, NetworkedPlayerInfo[] winners)
    {
        if (winners == null || winners.Length < 2)
        {
            return false;
        }

        var winningLawyers = new List<PlayerControl>();
        foreach (var w in winners)
        {
            var pc = w?.Object;
            if (pc != null && pc.IsRole<LawyerRole>())
            {
                winningLawyers.Add(pc);
            }
        }

        if (winningLawyers.Count == 0)
        {
            return false;
        }

        foreach (var w in winners)
        {
            var pc = w?.Object;
            if (pc == null)
            {
                return false;
            }

            if (pc.IsRole<LawyerRole>())
            {
                continue;
            }

            var ok = winningLawyers.Any(lawyerPc => LawyerUtils.IsClientOfLawyer(pc, lawyerPc.PlayerId));
            if (!ok)
            {
                return false;
            }
        }

        foreach (var lawyerPc in winningLawyers)
        {
            var hasClientWinner = winners.Any(w =>
            {
                var obj = w?.Object;
                return obj != null &&
                       !obj.IsRole<LawyerRole>() &&
                       LawyerUtils.IsClientOfLawyer(obj, lawyerPc.PlayerId);
            });
            if (!hasClientWinner)
            {
                return false;
            }
        }

        return true;
    }

    public override void AfterEndGameSetup(EndGameManager endGameManager)
    {
        var (winColor, winText) = DetermineWinCondition();
        SetWinningFaction(winColor, winText);

        endGameManager.BackgroundBar.material.SetColor(ShaderID.Color, winColor);

        var baseText = endGameManager.WinText;
        baseText.color = winColor;
        baseText.text = $"<size=4>{winText}!</size>";
    }

    private static (Color winColor, string winText) DetermineWinCondition()
    {
        return (TownOfUsColors.Lawyer,
            $"{TouLocale.Get("ExtensionRoleLawyer", "Lawyer")} {TouLocale.Get("ExtensionLawyerWin", "Wins")}");
    }

    private static void SetWinningFaction(Color winColor, string winText)
    {
        GameHistory.WinningFaction = $"<color=#{winColor.ToHtmlStringRGBA()}>{winText}</color>";
    }
}