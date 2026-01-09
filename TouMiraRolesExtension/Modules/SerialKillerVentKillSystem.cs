namespace TouMiraRolesExtension.Modules;

/// <summary>
/// Tracks when a Serial Killer can kill someone in a vent.
/// </summary>
public static class SerialKillerVentKillSystem
{
    private static readonly Dictionary<byte, PlayerControl?> VentKillTargets = new();

    public static void SetVentKillTarget(byte serialKillerId, PlayerControl? target)
    {
        if (target == null)
        {
            VentKillTargets.Remove(serialKillerId);
        }
        else
        {
            VentKillTargets[serialKillerId] = target;
        }
    }

    public static bool TryGetVentKillTarget(byte serialKillerId, out PlayerControl? target)
    {
        return VentKillTargets.TryGetValue(serialKillerId, out target);
    }

    public static void ClearAll()
    {
        VentKillTargets.Clear();
    }

    public static void ClearForPlayer(byte playerId)
    {
        VentKillTargets.Remove(playerId);
    }
}