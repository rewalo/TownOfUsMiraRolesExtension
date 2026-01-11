using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using TouMiraRolesExtension.Modules;

namespace TouMiraRolesExtension.Events.Neutral;

public static class LawyerRoundStartEvents
{
    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (@event.TriggeredByIntro)
        {
            LawyerDuoTracker.ClearAll();
        }
    }
}