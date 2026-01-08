using Reactor.Networking.Attributes;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Game;
using TownOfUs.Options;
using TownOfUs.Utilities;
using TouMiraRolesExtension.Modifiers;
using TouMiraRolesExtension.Networking;
using TouMiraRolesExtension.Options;
using TouMiraRolesExtension.Roles.Neutral;
using TouMiraRolesExtension.Utilities;
using TownOfUs;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using Reactor.Utilities.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Patches.Options;

namespace TouMiraRolesExtension.Patches;

public static class LawyerChatPatches
{
    [MethodRpc((uint)ExtensionRpc.SendLawyerChat)]
    public static void RpcSendLawyerChat(PlayerControl player, string text)
    {
        var genOpt = OptionGroupSingleton<Options.GeneralOptions>.Instance;
        if (!genOpt.LawyerChat)
        {
            return;
        }

        var localPlayer = PlayerControl.LocalPlayer;
        // Check if local player is the SPECIFIC client of this SPECIFIC lawyer
        var isClientOfThisLawyer = LawyerUtils.IsClientOfLawyer(localPlayer, player.PlayerId);
        var isDeadAndKnows = DeathHandlerModifier.IsFullyDead(localPlayer) &&
                             OptionGroupSingleton<TownOfUs.Options.GeneralOptions>.Instance.TheDeadKnow;

        var shouldMarkUnread = false;
        // Lawyer sees their own message
        if (player.AmOwner)
        {
            MiscUtils.AddTeamChat(player.Data,
                $"<color=#{TownOfUsColors.Lawyer.ToHtmlStringRGBA()}>{TouLocale.GetParsed("ExtensionLawyerChatTitle").Replace("<player>", player.Data.PlayerName)}</color>",
                text, bubbleType: BubbleType.Other, onLeft: false);
            shouldMarkUnread = true;
        }
        // Client of this specific lawyer sees the message
        else if (isClientOfThisLawyer)
        {
            MiscUtils.AddTeamChat(player.Data,
                $"<color=#{TownOfUsColors.Lawyer.ToHtmlStringRGBA()}>{TouLocale.GetParsed("ExtensionLawyerChatTitle").Replace("<player>", player.Data.PlayerName)}</color>",
                text, bubbleType: BubbleType.Other, onLeft: true);
            shouldMarkUnread = true;
        }
        // Dead players who can see private chats - check if they had a relationship with THIS specific lawyer
        else if (isDeadAndKnows)
        {
            // Check if dead player was a client of THIS specific lawyer
            var wasClientOfThisLawyer = LawyerUtils.IsClientOfLawyer(localPlayer, player.PlayerId);
            // Check if dead player was the lawyer for someone, and if that lawyer's client is the sender
            // (This shouldn't happen since sender is the lawyer, but checking for completeness)
            var deadPlayerLawyerRole = localPlayer.GetRole<LawyerRole>();
            var wasLawyerOfSender = deadPlayerLawyerRole != null && 
                                   deadPlayerLawyerRole.Client != null && 
                                   deadPlayerLawyerRole.Client.PlayerId == player.PlayerId;
            
            var canSee = wasClientOfThisLawyer || wasLawyerOfSender;

            if (canSee)
            {
                MiscUtils.AddTeamChat(player.Data,
                    $"<color=#{TownOfUsColors.Lawyer.ToHtmlStringRGBA()}>{TouLocale.GetParsed("ExtensionLawyerChatTitle").Replace("<player>", player.Data.PlayerName)}</color>",
                    text, bubbleType: BubbleType.Other, onLeft: !player.AmOwner);
                shouldMarkUnread = true;
            }
        }

        if (shouldMarkUnread && MeetingHud.Instance != null)
        {
            var chats = TeamChatPatches.TeamChatManager.GetAllAvailableChats();
            var hasForcedChat = chats.Any(c => c.IsForced);
            var currentChat = TeamChatPatches.CurrentChatIndex >= 0 && TeamChatPatches.CurrentChatIndex < chats.Count
? chats[TeamChatPatches.CurrentChatIndex]
: null;
            if ((!TeamChatPatches.TeamChatActive || currentChat == null || currentChat.Priority != 50) && !hasForcedChat)
            {
                TeamChatPatches.TeamChatManager.MarkChatAsUnread(50);
            }
        }
    }

    [MethodRpc((uint)ExtensionRpc.SendClientChat)]
    public static void RpcSendClientChat(PlayerControl player, string text)
    {
        var genOpt = OptionGroupSingleton<Options.GeneralOptions>.Instance;
        if (!genOpt.LawyerChat)
        {
            return;
        }

        var localPlayer = PlayerControl.LocalPlayer;
        // Check if local player is the SPECIFIC lawyer of this SPECIFIC client
        var isLawyerOfThisClient = LawyerUtils.HasLawyerClientRelationship(localPlayer, player);
        var isDeadAndKnows = DeathHandlerModifier.IsFullyDead(localPlayer) &&
                             OptionGroupSingleton<TownOfUs.Options.GeneralOptions>.Instance.TheDeadKnow;

        var shouldMarkUnread = false;
        // Client sees their own message
        if (player.AmOwner)
        {
            MiscUtils.AddTeamChat(player.Data,
                $"<color=#{TownOfUsColors.Lawyer.ToHtmlStringRGBA()}>{TouLocale.GetParsed("ExtensionLawyerClientChatTitle").Replace("<player>", player.Data.PlayerName)}</color>",
                text, bubbleType: BubbleType.Other, onLeft: false);
            shouldMarkUnread = true;
        }
        // Lawyer of this specific client sees the message
        else if (isLawyerOfThisClient)
        {
            MiscUtils.AddTeamChat(player.Data,
                $"<color=#{TownOfUsColors.Lawyer.ToHtmlStringRGBA()}>{TouLocale.GetParsed("ExtensionLawyerClientChatTitle").Replace("<player>", player.Data.PlayerName)}</color>",
                text, bubbleType: BubbleType.Other, onLeft: true);
            shouldMarkUnread = true;
        }
        // Dead players who can see private chats - check if they had a relationship with THIS specific client
        else if (isDeadAndKnows)
        {
            // Check if dead player was the lawyer for THIS specific client
            var wasLawyerOfThisClient = LawyerUtils.HasLawyerClientRelationship(localPlayer, player);
            // Check if dead player was a client, and if their lawyer is the sender
            // (This shouldn't happen since sender is the client, but checking for completeness)
            var wasClientOfSender = LawyerUtils.IsClientOfLawyer(localPlayer, player.PlayerId);
            
            var canSee = wasLawyerOfThisClient || wasClientOfSender;

            if (canSee)
            {
                MiscUtils.AddTeamChat(player.Data,
                    $"<color=#{TownOfUsColors.Lawyer.ToHtmlStringRGBA()}>{TouLocale.GetParsed("ExtensionLawyerClientChatTitle").Replace("<player>", player.Data.PlayerName)}</color>",
                    text, bubbleType: BubbleType.Other, onLeft: !player.AmOwner);
                shouldMarkUnread = true;
            }
        }

        if (shouldMarkUnread && MeetingHud.Instance != null)
        {
            var chats = TeamChatPatches.TeamChatManager.GetAllAvailableChats();
            var hasForcedChat = chats.Any(c => c.IsForced);
            var currentChat = TeamChatPatches.CurrentChatIndex >= 0 && TeamChatPatches.CurrentChatIndex < chats.Count
? chats[TeamChatPatches.CurrentChatIndex]
: null;
            if ((!TeamChatPatches.TeamChatActive || currentChat == null || currentChat.Priority != 50) && !hasForcedChat)
            {
                TeamChatPatches.TeamChatManager.MarkChatAsUnread(50);
            }
        }
    }
}