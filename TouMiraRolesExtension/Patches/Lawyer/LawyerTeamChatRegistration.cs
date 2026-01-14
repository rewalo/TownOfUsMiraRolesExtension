using MiraAPI.GameOptions;
using TouMiraRolesExtension.Utilities;
using TownOfUs;
using TownOfUs.Patches.Options;
using UnityEngine;
using static TownOfUs.Patches.Options.TeamChatPatches;

namespace TouMiraRolesExtension.Patches;

public static class LawyerTeamChatRegistration
{
    private static ExtensionTeamChatHandler? _handler;

    public static void Register()
    {
        if (_handler != null)
        {
            return;
        }

        _handler = new ExtensionTeamChatHandler
        {
            Priority = 50,
            IsChatAvailable = () =>
            {
                var genOpt = OptionGroupSingleton<Options.GeneralOptions>.Instance;
                if (!genOpt.LawyerChat || !MeetingHud.Instance)
                {
                    return false;
                }

                var localPlayer = PlayerControl.LocalPlayer;
                if (localPlayer == null || localPlayer.Data == null)
                {
                    return false;
                }

                var client = LawyerUtils.GetClientForLawyer(localPlayer);
                var isClient = LawyerUtils.IsClientOfAnyLawyer(localPlayer);

                return client != null || isClient;
            },
            SendMessage = (sender, message) =>
            {
                var localPlayer = PlayerControl.LocalPlayer;
                var client = LawyerUtils.GetClientForLawyer(localPlayer);
                var isClient = LawyerUtils.IsClientOfAnyLawyer(localPlayer);

                if (client != null)
                {
                    LawyerChatPatches.RpcSendLawyerChat(sender, message);
                }
                else if (isClient)
                {
                    LawyerChatPatches.RpcSendClientChat(sender, message);
                }
            },
            GetDisplayText = () =>
            {
                return "Lawyer Chat";
            },
            DisplayTextColor = TownOfUsColors.Lawyer,
            BackgroundColor = new Color(0.15f, 0.2f, 0.25f, 0.8f),
            CanDeadPlayerSee = (deadPlayer) =>
            {
                var genOpt = OptionGroupSingleton<TownOfUs.Options.GeneralOptions>.Instance;
                if (!genOpt.TheDeadKnow)
                {
                    return false;
                }

                var client = LawyerUtils.GetClientForLawyer(deadPlayer);
                var isClient = LawyerUtils.IsClientOfAnyLawyer(deadPlayer);

                return client != null || isClient;
            }
        };

        TeamChatPatches.ExtensionTeamChatRegistry.RegisterHandler(_handler);
    }

    public static void Unregister()
    {
        if (_handler != null)
        {
            TeamChatPatches.ExtensionTeamChatRegistry.UnregisterHandler(_handler);
            _handler = null;
        }
    }
}