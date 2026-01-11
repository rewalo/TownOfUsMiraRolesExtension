using MiraAPI.GameEnd;
using MiraAPI.Utilities;
using Reactor.Utilities.Extensions;
using TownOfUs.Modules;
using TouMiraRolesExtension.Roles.Neutral;
using TouMiraRolesExtension.Modifiers;
using TouMiraRolesExtension.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;
using TownOfUs.Modules.Localization;
using TownOfUs;
using TownOfUs.Utilities;
using MiraAPI.Modifiers;
using MiraAPI.Roles;

namespace TouMiraRolesExtension.GameOver;

public sealed class LawyerGameOver : CustomGameOver
{
    public override bool VerifyCondition(PlayerControl playerControl, NetworkedPlayerInfo[] winners)
    {
        // Must have at least 2 winners (at least one lawyer and one client)
        if (winners.Length < 2)
        {
            return false;
        }

        // The winners array is what gets replicated for this CustomGameOver.
        // Do NOT depend on local, non-networked role state (e.g. AboutToWin/WinConditionMet),
        // otherwise non-host clients can fall back to vanilla end-game results.
        if (!winners.Any(w => w.Object == playerControl))
        {
            return false;
        }

        // Check if the player is a winning lawyer
        if (playerControl.GetRole<LawyerRole>() != null)
        {
            return true;
        }

        // Check if the player is a client of any winning lawyer
        if (LawyerUtils.IsClientOfAnyLawyer(playerControl))
        {
            return true;
        }

        return false;
    }

    public override void AfterEndGameSetup(EndGameManager endGameManager)
    {
        // Win-stealing neutral: always show Lawyer win, and REPLACE the base WinText
        // (don't instantiate another TMP text, otherwise you see "Crewmates Win" underneath).
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