namespace TouMiraRolesExtension.Modules;

/// <summary>
/// Shared state for Lawyer win-condition triggering. Prevents per-frame re-trigger spam during end-game.
/// </summary>
public static class LawyerWinConditionState
{
    public static bool Triggered { get; private set; }

    public static void MarkTriggered()
    {
        Triggered = true;
    }

    public static void Reset()
    {
        Triggered = false;
    }
}