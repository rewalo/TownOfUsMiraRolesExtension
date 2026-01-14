using TownOfUs.Events.TouEvents;

namespace TouMiraRolesExtension.Events.Neutral;

/// <summary>
/// Event fired when a lawyer changes role due to their client dying.
/// </summary>
public class TimeLordLawyerRoleChangeEvent : TimeLordEvent
{
    /// <summary>
    /// The client's player ID whose death caused the role change.
    /// </summary>
    public byte ClientId { get; }

    /// <summary>
    /// The role type the lawyer changed to.
    /// </summary>
    public ushort NewRoleType { get; }

    public TimeLordLawyerRoleChangeEvent(PlayerControl lawyer, byte clientId, ushort newRoleType, float time) 
        : base(lawyer, time)
    {
        ClientId = clientId;
        NewRoleType = newRoleType;
    }
}

/// <summary>
/// Event fired to undo a lawyer role change during rewind (restore lawyer role).
/// </summary>
public class TimeLordLawyerRoleChangeUndoEvent : TimeLordUndoEvent
{
    public TimeLordLawyerRoleChangeUndoEvent(TimeLordLawyerRoleChangeEvent originalEvent) 
        : base(originalEvent)
    {
    }
}

