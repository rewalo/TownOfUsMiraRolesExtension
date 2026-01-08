using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.GameOptions;
using TouMiraRolesExtension.Roles.Neutral;
using TouMiraRolesExtension.Options.Roles.Neutral;
using TownOfUs.Utilities;

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
}