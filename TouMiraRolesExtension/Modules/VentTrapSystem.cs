using MiraAPI.GameOptions;
using TouMiraRolesExtension.Options.Roles.Crewmate;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMiraRolesExtension.Modules;

public static class VentTrapSystem
{
    private sealed record TrapEntry(byte OwnerId, int RoundsRemaining);


    private static readonly Dictionary<int, TrapEntry> Traps = new();

    public static bool TryGetTraprId(int ventId, out byte traprId)
    {
        if (Traps.TryGetValue(ventId, out var entry))
        {
            traprId = entry.OwnerId;
            return true;
        }

        traprId = default;
        return false;
    }

    public static bool IsTrapped(int ventId) => Traps.ContainsKey(ventId);

    public static void Place(int ventId, byte traprId)
    {
        var rounds = (int)OptionGroupSingleton<TrapperOptions>.Instance.TrapRoundsLast;
        Traps[ventId] = new TrapEntry(traprId, rounds);
    }

    public static void Remove(int ventId)
    {
        Traps.Remove(ventId);
    }

    public static void DecrementRoundsAndRemoveExpired()
    {
        var roundsLast = (int)OptionGroupSingleton<TrapperOptions>.Instance.TrapRoundsLast;
        if (roundsLast <= 0 || Traps.Count == 0)
        {
            return;
        }

        var toRemove = new List<int>();
        var toUpdate = new List<KeyValuePair<int, TrapEntry>>();

        foreach (var kvp in Traps)
        {
            var newRemaining = kvp.Value.RoundsRemaining - 1;
            if (newRemaining <= 0)
            {
                toRemove.Add(kvp.Key);
            }
            else
            {
                toUpdate.Add(new(kvp.Key, kvp.Value with { RoundsRemaining = newRemaining }));
            }
        }

        foreach (var ventId in toRemove)
        {
            Traps.Remove(ventId);
        }

        foreach (var kvp in toUpdate)
        {
            Traps[kvp.Key] = kvp.Value;
        }
    }

    public static void ClearAll()
    {
        Traps.Clear();
    }

    public static void ClearOwnedBy(byte traprId)
    {
        if (Traps.Count == 0)
        {
            return;
        }

        var toRemove = new List<int>();
        foreach (var kvp in Traps)
        {
            if (kvp.Value.OwnerId == traprId)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var ventId in toRemove)
        {
            Traps.Remove(ventId);
        }
    }

    public static bool IsEligibleToBeTrapped(PlayerControl pc)
    {
        if (pc == null || pc.HasDied())
        {
            return false;
        }

        var targets = OptionGroupSingleton<TrapperOptions>.Instance.TrapTargets;
        return targets switch
        {
            VentTrapTargets.Impostors => pc.IsImpostor(),
            VentTrapTargets.ImpostorsAndNeutrals => pc.IsImpostor() || pc.IsNeutral(),
            VentTrapTargets.All => true,
            _ => pc.IsImpostor() || pc.IsNeutral()
        };
    }

    public static Vector2 GetVentTopPosition(Vent vent)
    {
        return (Vector2)vent.transform.position + new Vector2(0f, 0.3636f);
    }
}
