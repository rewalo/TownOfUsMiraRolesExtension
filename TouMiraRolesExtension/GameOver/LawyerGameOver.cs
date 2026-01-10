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
        // Determine the display based on any alive Lawyer's current Client.
        // Avoid WinConditionMet() here because it may rely on non-networked transient state.
        var client = PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p != null && !p.HasDied() && p.IsRole<LawyerRole>())
            .Select(p => p.GetRole<LawyerRole>())
            .Where(l => l != null && l.Client != null && !l.Client.HasDied())
            .Select(l => l!.Client)
            .FirstOrDefault();
        
        var (winColor, winText) = DetermineWinCondition(client);
        SetWinningFaction(winColor, winText);

        endGameManager.BackgroundBar.material.SetColor(ShaderID.Color, winColor);

        var text = Object.Instantiate(endGameManager.WinText);
        text.text = $"{winText}!";
        text.color = winColor;

        var pos = endGameManager.WinText.transform.localPosition;
        pos.y = 1.5f;
        pos += Vector3.down * 0.15f;
        text.transform.localScale = new Vector3(1f, 1f, 1f);

        text.transform.position = pos;
        text.text = $"<size=4>{text.text}</size>";
    }

    private static (Color winColor, string winText) DetermineWinCondition(PlayerControl? client)
    {
        if (client != null && client.Data != null && client.Data.Role != null)
        {
            var clientRole = client.Data.Role;
            
            if (client.IsImpostorAligned())
            {
                return (Palette.ImpostorRed, TouLocale.Get("ImpostorWin", "Impostors Win"));
            }
            
            if (clientRole is ICustomRole customRole && customRole.Team == ModdedRoleTeams.Custom)
            {
                var roleName = Helpers.GetRoleName(clientRole);
                return (clientRole.TeamColor, $"{roleName} {TouLocale.Get("Wins", "Wins")}");
            }
        }
        
        return (TownOfUsColors.Lawyer, $"{TouLocale.Get("ExtensionRoleLawyer", "Lawyer")} {TouLocale.Get("ExtensionLawyerWin", "Wins")}");
    }

    private static void SetWinningFaction(Color winColor, string winText)
    {
        GameHistory.WinningFaction = $"<color=#{winColor.ToHtmlStringRGBA()}>{winText}</color>";
    }
}