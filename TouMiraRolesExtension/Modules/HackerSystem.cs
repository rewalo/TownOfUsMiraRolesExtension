using Il2CppInterop.Runtime;
using TownOfUs.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;
using Resources = UnityEngine.Resources;

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
    private static SystemConsole[]? _cachedSystemConsoles;
    private static MapConsole[]? _cachedMapConsoles;
    private static SystemConsole? _cachedCameraConsole;
    private static SystemConsole? _cachedDoorLogConsole;
    private static int _cachedConsoleFrame = -1;

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
        InvalidateConsoleCache();
    }

    /// <summary>
    /// Reset per-round Hacker state (battery + lock). Jam charges persist.
    /// </summary>
    public static void ResetRoundState()
    {
        LockedSourceByPlayer.Clear();
        BatterySecondsByPlayer.Clear();
        JamActiveUntil = 0f;
        InvalidateConsoleCache();
    }

    /// <summary>
    /// Invalidates cached console lookups. Call when map changes or consoles may have been destroyed.
    /// </summary>
    private static void InvalidateConsoleCache()
    {
        _cachedSystemConsoles = null;
        _cachedMapConsoles = null;
        _cachedCameraConsole = null;
        _cachedDoorLogConsole = null;
        _cachedConsoleFrame = -1;
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


        if (_cachedMapConsoles == null || Time.frameCount != _cachedConsoleFrame)
        {
            _cachedMapConsoles = Object.FindObjectsOfType<MapConsole>();
            _cachedConsoleFrame = Time.frameCount;
        }

        var consoles = _cachedMapConsoles;
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

        return dist < float.MaxValue;
    }

    private static bool TryGetVitalsDistance(Vector2 from, float range, out float dist)
    {
        dist = float.MaxValue;


        var consoles = GetCachedSystemConsoles();
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

        return dist < float.MaxValue;
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


        var mapId = (ExpandedMapNames)GameOptionsManager.Instance.currentNormalGameOptions.MapId;
        if (TutorialManager.InstanceExists)
        {
            mapId = (ExpandedMapNames)AmongUsClient.Instance.TutorialMapId;
        }

        if (mapId is ExpandedMapNames.MiraHq)
        {

            var miraDoorLogPos = new Vector2(15.9f, 4.8f);
            var d = Vector2.Distance(from, miraDoorLogPos);
            if (d <= range)
            {
                dist = d;
                return true;
            }
            return false;
        }


        var sc = FindDoorLogConsole();
        if (sc == null)
        {
            return false;
        }

        var p = (Vector2)sc.transform.position;
        var d2 = Vector2.Distance(from, p);
        if (d2 <= range)
        {
            dist = d2;
            return true;
        }

        return false;
    }

    public static SystemConsole? FindCameraConsole()
    {

        if (_cachedCameraConsole != null && _cachedCameraConsole.gameObject != null && _cachedCameraConsole.gameObject.activeInHierarchy)
        {
            return _cachedCameraConsole;
        }


        _cachedCameraConsole = null;

        var mapId = (ExpandedMapNames)GameOptionsManager.Instance.currentNormalGameOptions.MapId;
        if (TutorialManager.InstanceExists)
        {
            mapId = (ExpandedMapNames)AmongUsClient.Instance.TutorialMapId;
        }

        var consoles = GetCachedSystemConsoles();
        if (consoles == null || consoles.Length == 0)
        {
            return null;
        }

        SystemConsole? result = null;
        if (mapId is ExpandedMapNames.Airship)
        {
            result = consoles.FirstOrDefault(x => x != null && x.gameObject.name.Contains("task_cams"));
        }
        else if (mapId is ExpandedMapNames.Skeld or ExpandedMapNames.Dleks)
        {
            result = consoles.FirstOrDefault(x => x != null && x.gameObject.name.Contains("SurvConsole"));
        }
        else if (mapId is ExpandedMapNames.MiraHq)
        {
            result = consoles.FirstOrDefault(IsDoorLogConsole) ??
                     consoles.FirstOrDefault(x => x != null && x.gameObject.name.Contains("SurvLogConsole"));
        }
        else if (mapId is ExpandedMapNames.Submerged)
        {
            result = consoles.FirstOrDefault(x => x != null && x.gameObject.name.Contains("SecurityConsole"));
        }
        else
        {
            result = consoles.FirstOrDefault(x =>
                x != null && (x.gameObject.name.Contains("Surv_Panel") || x.name.Contains("Cam") ||
                              x.name.Contains("BinocularsSecurityConsole")));
        }

        _cachedCameraConsole = result;
        return result;
    }

    public static SystemConsole? FindDoorLogConsole()
    {
        if (_cachedDoorLogConsole != null && _cachedDoorLogConsole.gameObject != null && _cachedDoorLogConsole.gameObject.activeInHierarchy)
        {
            return _cachedDoorLogConsole;
        }

        _cachedDoorLogConsole = null;

        var consoles = GetCachedSystemConsoles();
        if (consoles == null || consoles.Length == 0)
        {
            return null;
        }

        var result = consoles.FirstOrDefault(IsDoorLogConsole) ??
                     consoles.FirstOrDefault(x =>
                         x != null &&
                         (x.gameObject.name.Contains("DoorLog", System.StringComparison.OrdinalIgnoreCase) ||
                          x.gameObject.name.Contains("SurvLogConsole", System.StringComparison.OrdinalIgnoreCase) ||
                          x.gameObject.name.Contains("SurvLog", System.StringComparison.OrdinalIgnoreCase) ||
                          x.name.Contains("DoorLog", System.StringComparison.OrdinalIgnoreCase)));

        _cachedDoorLogConsole = result;
        return result;
    }

    private static bool IsDoorLogConsole(SystemConsole? console)
    {
        if (console == null || console.MinigamePrefab == null)
        {
            return false;
        }

        if (console.MinigamePrefab.TryCast<SecurityLogGame>() != null)
        {
            return true;
        }

        return console.gameObject.name.Contains("SurvLogConsole", System.StringComparison.OrdinalIgnoreCase) ||
               console.gameObject.name.Contains("DoorLog", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets cached system consoles, refreshing the cache if needed. This avoids expensive FindObjectsOfType calls.
    /// </summary>
    private static SystemConsole[] GetCachedSystemConsoles()
    {

        if (_cachedSystemConsoles == null || Time.frameCount != _cachedConsoleFrame)
        {
            _cachedSystemConsoles = FindAllSystemConsoles();
            _cachedConsoleFrame = Time.frameCount;
        }

        return _cachedSystemConsoles ?? System.Array.Empty<SystemConsole>();
    }

    private static SystemConsole[] FindAllSystemConsoles()
    {
        var consoles = Object.FindObjectsOfType<SystemConsole>();
        if (consoles != null && consoles.Length > 0)
        {
            return consoles;
        }

        try
        {
            var allObjects = Resources.FindObjectsOfTypeAll(Il2CppType.From(typeof(SystemConsole)));
            if (allObjects == null)
            {
                return System.Array.Empty<SystemConsole>();
            }

            var result = new List<SystemConsole>();
            foreach (var obj in allObjects)
            {
                if (obj == null)
                {
                    continue;
                }

                var sc = obj.TryCast<SystemConsole>();
                if (sc == null || sc.gameObject == null)
                {
                    continue;
                }

                if (!sc.gameObject.scene.isLoaded)
                {
                    continue;
                }

                result.Add(sc);
            }

            return result.ToArray();
        }
        catch
        {
            return System.Array.Empty<SystemConsole>();
        }
    }
}