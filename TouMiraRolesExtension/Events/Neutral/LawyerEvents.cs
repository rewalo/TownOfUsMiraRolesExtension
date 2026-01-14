using System.Collections;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Meeting.Voting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.GameOptions;
using Reactor.Utilities;
using TouMiraRolesExtension.Roles.Neutral;
using TouMiraRolesExtension.Options.Roles.Neutral;
using TownOfUs.Events;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Game;
using TownOfUs.Utilities;
using TownOfUs.Modules.Localization;
using MiraAPI.Utilities;

namespace TouMiraRolesExtension.Events.Neutral;

public static class LawyerEvents
{
    [RegisterEvent]
    public static void EjectionEventHandler(EjectionEvent @event)
    {
        var exiled = @event.ExileController?.initData?.networkedPlayer?.Object;
        if (exiled == null)
        {
            return;
        }

        foreach (var player in PlayerControl.AllPlayerControls.ToArray())
        {
            if (player == null || !player.IsRole<LawyerRole>())
            {
                continue;
            }

            var lawyer = player.GetRole<LawyerRole>();
            if (lawyer == null || lawyer.Client == null)
            {
                continue;
            }

            if (lawyer.Client.PlayerId == exiled.PlayerId)
            {
                lawyer.ClientVoted = true;
                
                if (OptionGroupSingleton<LawyerOptions>.Instance.GetVotedOutWithClient)
                {
                    DeathHandlerModifier.UpdateDeathHandlerImmediate(lawyer.Player, 
                        TouLocale.Get("ExtensionLawyerDiedWithClient"),
                        DeathEventHandlers.CurrentRound, 
                        DeathHandlerOverride.SetFalse,
                        lockInfo: DeathHandlerOverride.SetTrue);
                    lawyer.Player.Exiled();
                }
                
                lawyer.CheckClientDeath(exiled);
            }
        }
    }

    [RegisterEvent]
    public static void PlayerDeathEventHandler(PlayerDeathEvent @event)
    {
        var victim = @event.Player;
        if (victim == null)
        {
            return;
        }

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || !player.IsRole<LawyerRole>())
            {
                continue;
            }

            var lawyer = player.GetRole<LawyerRole>();
            if (lawyer == null || lawyer.Client == null)
            {
                continue;
            }

            if (lawyer.Client.PlayerId == victim.PlayerId)
            {
                lawyer.CheckClientDeath(victim);
            }
        }
    }

    [RegisterEvent(1000)]
    public static void BeforeLocalVoteEvent(BeforeVoteEvent @event)
    {
        var voteArea = @event.VoteArea;
        var votedPlayer = MiscUtils.PlayerById(voteArea.TargetPlayerId);
        if (PlayerControl.LocalPlayer.HasDied() || (votedPlayer != null && votedPlayer.HasDied()))
        {
            return;
        }

        if (PlayerControl.LocalPlayer.Data.Role is not LawyerRole)
        {
            return;
        }

        if (voteArea.Parent.state is MeetingHud.VoteStates.Proceeding or MeetingHud.VoteStates.Results)
        {
            return;
        }

    }
}