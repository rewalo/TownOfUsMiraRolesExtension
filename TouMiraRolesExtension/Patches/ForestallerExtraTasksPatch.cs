using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MiraAPI.GameOptions;
using TouMiraRolesExtension.Options.Roles.Crewmate;
using TouMiraRolesExtension.Roles.Crewmate;

namespace TouMiraRolesExtension.Patches;

[HarmonyPatch]
public static class ForestallerExtraTasksPatch
{
    [HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.RpcSetTasks))]
    [HarmonyPrefix]
    public static void RpcSetTasksPrefix(NetworkedPlayerInfo __instance, ref Il2CppStructArray<byte> taskTypeIds)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            return;
        }

        if (taskTypeIds == null)
        {
            return;
        }

        if (__instance == null || __instance.Disconnected)
        {
            return;
        }

        var player = __instance.Object;
        if (player == null || player.Data == null || player.Data.Disconnected)
        {
            return;
        }

        if (player.Data.Role is not ForestallerRole)
        {
            return;
        }

        var opt = OptionGroupSingleton<ForestallerOptions>.Instance;
        var extraShort = opt != null ? Math.Max(0, (int)opt.ExtraShortTasks) : 0;
        var extraLong = opt != null ? Math.Max(0, (int)opt.ExtraLongTasks) : 0;
        if (extraShort == 0 && extraLong == 0)
        {
            return;
        }

        var ship = ShipStatus.Instance;
        if (ship == null)
        {
            return;
        }

        var used = new HashSet<byte>();
        for (var i = 0; i < taskTypeIds.Length; i++)
        {
            used.Add(taskTypeIds[i]);
        }

        var shortPool = GetIndexPool(ship.ShortTasks);
        var longPool = GetIndexPool(ship.LongTasks);

        var additions = new List<byte>(extraShort + extraLong);
        AddRandomFromPool(additions, shortPool, extraShort, used);
        AddRandomFromPool(additions, longPool, extraLong, used);

        if (additions.Count == 0)
        {
            return;
        }

        var newArr = new Il2CppStructArray<byte>(taskTypeIds.Length + additions.Count);
        for (var i = 0; i < taskTypeIds.Length; i++)
        {
            newArr[i] = taskTypeIds[i];
        }

        for (var i = 0; i < additions.Count; i++)
        {
            newArr[taskTypeIds.Length + i] = additions[i];
        }

        taskTypeIds = newArr;

        if (TouMiraRolesExtensionPlugin.IsDevBuild)
        {
            Info($"[ForestallerExtraTasks] {player.Data.PlayerName} base={newArr.Length - additions.Count} added={additions.Count} short={extraShort} long={extraLong}");
        }
    }

    private static HashSet<byte> GetIndexPool(Il2CppReferenceArray<NormalPlayerTask> prefabs)
    {
        var pool = new HashSet<byte>();
        if (prefabs == null)
        {
            return pool;
        }

        for (var i = 0; i < prefabs.Length; i++)
        {
            var t = prefabs[i];
            if (t == null)
            {
                continue;
            }

            try
            {
                pool.Add((byte)t.Index);
            }
            catch
            {
                // ignored
            }
        }

        return pool;
    }

    private static void AddRandomFromPool(List<byte> additions, HashSet<byte> pool, int count, HashSet<byte> used)
    {
        if (count <= 0 || pool == null || pool.Count == 0)
        {
            return;
        }

        var available = new List<byte>(pool.Count);
        foreach (var b in pool)
        {
            if (!used.Contains(b))
            {
                available.Add(b);
            }
        }

        var rng = new System.Random(Guid.NewGuid().GetHashCode());
        while (count > 0 && available.Count > 0)
        {
            var idx = rng.Next(available.Count);
            var pick = available[idx];
            available.RemoveAt(idx);
            additions.Add(pick);
            used.Add(pick);
            count--;
        }

        if (count > 0)
        {
            var all = new List<byte>(pool);
            if (all.Count == 0)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                additions.Add(all[rng.Next(all.Count)]);
            }
        }
    }
}