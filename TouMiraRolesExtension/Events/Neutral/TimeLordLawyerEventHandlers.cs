using MiraAPI.Events;
using MiraAPI.Roles;
using TouMiraRolesExtension.Roles.Neutral;
using TownOfUs.Events.Crewmate;
using TownOfUs.Events.TouEvents;
using TownOfUs.Modules.TimeLord;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMiraRolesExtension.Events.Neutral;

/// <summary>
/// Event handlers for Lawyer TimeLord events. Registers undo handlers and handles undo events
/// to restore lawyer role when client is revived during rewind.
/// </summary>
public static class TimeLordLawyerEventHandlers
{
    static TimeLordLawyerEventHandlers()
    {
        // Register undo event factory so CreateUndoEvent can create undo events for lawyer role changes
        var eventQueue = TimeLordEventHandlers.GetEventQueue();
        eventQueue.RegisterUndoEventFactory<TimeLordLawyerRoleChangeEvent>(evt =>
            new TimeLordLawyerRoleChangeUndoEvent(evt));
    }

    /// <summary>
    /// Handles lawyer role change undo events during rewind (restores lawyer role).
    /// </summary>
    [RegisterEvent]
    public static void HandleLawyerRoleChangeUndo(TimeLordLawyerRoleChangeUndoEvent @event)
    {
        if (@event.OriginalEvent is not TimeLordLawyerRoleChangeEvent originalEvent)
        {
            return;
        }

        var lawyer = originalEvent.Player;
        if (lawyer == null || lawyer.Data == null)
        {
            return;
        }

        // Only the host should change roles
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        // Change the lawyer back to lawyer role
        var lawyerRoleId = RoleId.Get<LawyerRole>();
        lawyer.ChangeRole(lawyerRoleId);
    }

    /// <summary>
    /// Records a lawyer role change event.
    /// </summary>
    public static void RecordLawyerRoleChange(PlayerControl lawyer, byte clientId, ushort newRoleType)
    {
        if (lawyer == null || !TownOfUs.Modules.TimeLordRewindSystem.MatchHasTimeLord())
        {
            return;
        }

        var evt = new TimeLordLawyerRoleChangeEvent(lawyer, clientId, newRoleType, Time.time);
        MiraEventManager.InvokeEvent(evt);
        var eventQueue = TimeLordEventHandlers.GetEventQueue();
        eventQueue.RecordEvent(evt);
    }
}

