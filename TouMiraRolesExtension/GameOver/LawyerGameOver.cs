using MiraAPI.GameEnd;
using MiraAPI.Utilities;
using Reactor.Utilities.Extensions;
using TownOfUs.Modules;
using TouMiraRolesExtension.Roles.Neutral;
using TouMiraRolesExtension.Modifiers;
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
        if (winners.Length != 2)
        {
            return false;
        }

        if (playerControl.GetRole<LawyerRole>() != null)
        {
            return winners.Any(w => w.Object == playerControl);
        }

        if (playerControl.HasModifier<LawyerTargetModifier>())
        {
            return winners.Any(w => w.Object == playerControl);
        }

        return false;
    }

    public override void AfterEndGameSetup(EndGameManager endGameManager)
    {
        var client = PlayerControl.AllPlayerControls.ToArray()
            .FirstOrDefault(p => p != null && p.HasModifier<LawyerTargetModifier>());
        
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