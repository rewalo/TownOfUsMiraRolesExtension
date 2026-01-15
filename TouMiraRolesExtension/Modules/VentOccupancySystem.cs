using TownOfUs.Utilities;

namespace TouMiraRolesExtension.Modules;

/// <summary>
/// Tracks which players are currently in which vents.
/// </summary>
public static class VentOccupancySystem
{
    private static readonly Dictionary<int, byte> VentOccupants = new();

    public static void SetOccupant(int ventId, byte playerId)
    {
        if (playerId == 0)
        {
            VentOccupants.Remove(ventId);
        }
        else
        {
            VentOccupants[ventId] = playerId;
        }
    }

    public static bool TryGetOccupant(int ventId, out byte playerId)
    {
        return VentOccupants.TryGetValue(ventId, out playerId);
    }

    public static bool IsOccupied(int ventId)
    {
        return VentOccupants.ContainsKey(ventId);
    }

    public static void ClearAll()
    {
        VentOccupants.Clear();
    }

    public static void ClearForPlayer(byte playerId)
    {
        var toRemove = new List<int>();
        foreach (var kvp in VentOccupants)
        {
            if (kvp.Value == playerId)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var ventId in toRemove)
        {
            VentOccupants.Remove(ventId);
        }
    }

    public static PlayerControl? GetOccupantPlayer(int ventId)
    {
        if (TryGetOccupant(ventId, out var playerId))
        {
            return MiscUtils.PlayerById(playerId);
        }
        return null;
    }
}
