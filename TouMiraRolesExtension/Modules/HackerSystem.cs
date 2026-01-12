using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using InnerNet;
using TownOfUs.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMiraRolesExtension.Modules;

public enum HackerInfoSource : byte
{
    None = 0,
    Admin = 1,
    Cameras = 2,
    Vitals = 3,
    DoorLog = 4
}

public static class HackerSystem
{
    private static readonly Dictionary<byte, HackerInfoSource> LockedSourceByPlayer = new();
    private static readonly Dictionary<byte, float> BatterySecondsByPlayer = new();
    private static readonly Dictionary<byte, byte> JamChargesByPlayer = new();

    public static float JamActiveUntil { get; private set; }

    public static bool IsJammed => Time.time < JamActiveUntil;

    /// <summary>
    /// Reset ALL Hacker state (use for new game / intro).
    /// </summary>
    public static void ResetAll()
    {
        LockedSourceByPlayer.Clear();
        BatterySecondsByPlayer.Clear();
        JamChargesByPlayer.Clear();
        JamActiveUntil = 0f;
    }

    /// <summary>
    /// Reset per-round Hacker state (battery + lock). Jam charges persist.
    /// </summary>
    public static void ResetRoundState()
    {
        LockedSourceByPlayer.Clear();
        BatterySecondsByPlayer.Clear();
        JamActiveUntil = 0f;
    }

    public static HackerInfoSource GetLockedSource(byte playerId)
    {
        return LockedSourceByPlayer.TryGetValue(playerId, out var src) ? src : HackerInfoSource.None;
    }

    public static void SetLockedSource(byte playerId, HackerInfoSource source)
    {
        if (source == HackerInfoSource.None)
        {
            LockedSourceByPlayer.Remove(playerId);
            return;
        }

        LockedSourceByPlayer[playerId] = source;
    }

    public static float GetBatterySeconds(byte playerId)
    {
        return BatterySecondsByPlayer.TryGetValue(playerId, out var v) ? v : 0f;
    }

    public static void SetBatterySeconds(byte playerId, float seconds)
    {
        if (seconds <= 0f)
        {
            BatterySecondsByPlayer.Remove(playerId);
            return;
        }

        BatterySecondsByPlayer[playerId] = seconds;
    }

    public static byte GetJamCharges(byte playerId)
    {
        return JamChargesByPlayer.TryGetValue(playerId, out var c) ? c : (byte)0;
    }

    public static void SetJamCharges(byte playerId, byte charges)
    {
        if (charges == 0)
        {
            JamChargesByPlayer.Remove(playerId);
            return;
        }

        JamChargesByPlayer[playerId] = charges;
    }

    public static void AddJamCharge(byte playerId, int delta, int maxCharges)
    {
        if (delta <= 0 || maxCharges <= 0)
        {
            return;
        }

        var current = GetJamCharges(playerId);
        var next = Mathf.Clamp(current + delta, 0, maxCharges);
        SetJamCharges(playerId, (byte)next);
    }

    public static bool TryConsumeJamCharge(byte playerId)
    {
        var current = GetJamCharges(playerId);
        if (current <= 0)
        {
            return false;
        }

        SetJamCharges(playerId, (byte)(current - 1));
        return true;
    }

    public static void ActivateJam(float durationSeconds)
    {
        var end = Time.time + Mathf.Max(0.1f, durationSeconds);
        JamActiveUntil = Mathf.Max(JamActiveUntil, end);
    }

    public static bool TryFindNearbyDownloadSource(PlayerControl player, float range, out HackerInfoSource source)
    {
        source = HackerInfoSource.None;
        if (player == null)
        {
            return false;
        }

        var pos = player.GetTruePosition();
        var bestDist = float.MaxValue;
        HackerInfoSource best = HackerInfoSource.None;

        if (TryGetAdminDistance(pos, range, out var dAdmin) && dAdmin < bestDist)
        {
            bestDist = dAdmin;
            best = HackerInfoSource.Admin;
        }

        if (TryGetCameraDistance(pos, range, out var dCam) && dCam < bestDist)
        {
            bestDist = dCam;
            best = HackerInfoSource.Cameras;
        }

        if (TryGetVitalsDistance(pos, range, out var dVit) && dVit < bestDist)
        {
            bestDist = dVit;
            best = HackerInfoSource.Vitals;
        }

        if (TryGetDoorLogDistance(pos, range, out var dDoor) && dDoor < bestDist)
        {
            bestDist = dDoor;
            best = HackerInfoSource.DoorLog;
        }

        source = best;
        return best != HackerInfoSource.None;
    }

    public static bool IsPlayerNearSource(PlayerControl player, HackerInfoSource source, float range)
    {
        if (player == null)
        {
            return false;
        }

        var pos = player.GetTruePosition();
        return source switch
        {
            HackerInfoSource.Admin => TryGetAdminDistance(pos, range, out _),
            HackerInfoSource.Cameras => TryGetCameraDistance(pos, range, out _),
            HackerInfoSource.Vitals => TryGetVitalsDistance(pos, range, out _),
            HackerInfoSource.DoorLog => TryGetDoorLogDistance(pos, range, out _),
            _ => false
        };
    }

    private static bool TryGetAdminDistance(Vector2 from, float range, out float dist)
    {
        dist = float.MaxValue;
        var consoles = Object.FindObjectsOfType<MapConsole>();
        if (consoles == null || consoles.Length == 0)
        {
            return false;
        }

        foreach (var mc in consoles)
        {
            if (mc == null)
            {
                continue;
            }

            var p = (Vector2)mc.transform.position;
            var d = Vector2.Distance(from, p);
            if (d <= range && d < dist)
            {
                dist = d;
            }
        }

        return dist != float.MaxValue;
    }

    private static bool TryGetVitalsDistance(Vector2 from, float range, out float dist)
    {
        dist = float.MaxValue;
        var consoles = Object.FindObjectsOfType<SystemConsole>();
        if (consoles == null || consoles.Length == 0)
        {
            return false;
        }

        foreach (var sc in consoles)
        {
            if (sc == null || sc.MinigamePrefab == null)
            {
                continue;
            }

            if (sc.MinigamePrefab.TryCast<VitalsMinigame>() == null && !sc.name.Contains("vitals", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var p = (Vector2)sc.transform.position;
            var d = Vector2.Distance(from, p);
            if (d <= range && d < dist)
            {
                dist = d;
            }
        }

        return dist != float.MaxValue;
    }

    private static bool TryGetCameraDistance(Vector2 from, float range, out float dist)
    {
        dist = float.MaxValue;
        var sc = FindCameraConsole();
        if (sc == null)
        {
            return false;
        }

        var p = (Vector2)sc.transform.position;
        var d = Vector2.Distance(from, p);
        if (d <= range)
        {
            dist = d;
            return true;
        }

        return false;
    }

    private static bool TryGetDoorLogDistance(Vector2 from, float range, out float dist)
    {
        dist = float.MaxValue;
        var sc = FindDoorLogConsole();
        if (sc == null)
        {
            return false;
        }

        var p = (Vector2)sc.transform.position;
        var d = Vector2.Distance(from, p);
        if (d <= range)
        {
            dist = d;
            return true;
        }

        return false;
    }

    public static SystemConsole? FindCameraConsole()
    {
        var mapId = (ExpandedMapNames)GameOptionsManager.Instance.currentNormalGameOptions.MapId;
        if (TutorialManager.InstanceExists)
        {
            mapId = (ExpandedMapNames)AmongUsClient.Instance.TutorialMapId;
        }

        var consoles = Object.FindObjectsOfType<SystemConsole>();
        if (consoles == null || consoles.Length == 0)
        {
            return null;
        }

        if (mapId is ExpandedMapNames.Airship)
        {
            return consoles.FirstOrDefault(x => x != null && x.gameObject.name.Contains("task_cams"));
        }

        if (mapId is ExpandedMapNames.Skeld or ExpandedMapNames.Dleks)
        {
            return consoles.FirstOrDefault(x => x != null && x.gameObject.name.Contains("SurvConsole"));
        }

        if (mapId is ExpandedMapNames.MiraHq)
        {
            return consoles.FirstOrDefault(x => x != null && x.gameObject.name.Contains("SurvLogConsole"));
        }

        if (mapId is ExpandedMapNames.Submerged)
        {
            return consoles.FirstOrDefault(x => x != null && x.gameObject.name.Contains("SecurityConsole"));
        }

        return consoles.FirstOrDefault(x =>
            x != null && (x.gameObject.name.Contains("Surv_Panel") || x.name.Contains("Cam") ||
                          x.name.Contains("BinocularsSecurityConsole")));
    }

    public static SystemConsole? FindDoorLogConsole()
    {
        var consoles = Object.FindObjectsOfType<SystemConsole>();
        if (consoles == null || consoles.Length == 0)
        {
            return null;
        }

        return consoles.FirstOrDefault(x =>
                   x != null &&
                   (x.gameObject.name.Contains("DoorLog", System.StringComparison.OrdinalIgnoreCase) ||
                    x.gameObject.name.Contains("SurvLogConsole", System.StringComparison.OrdinalIgnoreCase))) ??
               consoles.FirstOrDefault(x => x != null && x.name.Contains("DoorLog", System.StringComparison.OrdinalIgnoreCase));
    }
}