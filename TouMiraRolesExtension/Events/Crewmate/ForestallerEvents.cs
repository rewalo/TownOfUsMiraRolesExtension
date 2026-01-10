using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using TownOfUs.Roles;
using TouMiraRolesExtension.Roles.Crewmate;

namespace TouMiraRolesExtension.Events.Crewmate;

public static class ForestallerEvents
{
    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (@event.TriggeredByIntro)
        {
            Modules.ForestallerSystem.ClearAll();
        }
    }

    [RegisterEvent]
    public static void CompleteTaskEvent(CompleteTaskEvent @event)
    {
        if (@event.Player?.Data?.Role is ForestallerRole forestaller)
        {
            forestaller.CheckTaskRequirements();
        }
    }

    [RegisterEvent]
    public static void PlayerDeathEvent(PlayerDeathEvent @event)
    {
        if (@event.Player?.Data?.Role is ForestallerRole)
        {
            Modules.ForestallerSystem.OnForestallerDeath(@event.Player);
        }
    }

    [RegisterEvent]
    public static void StartMeetingEventHandler(StartMeetingEvent @event)
    {
        Modules.ForestallerSystem.OnMeetingStarted();
    }
}