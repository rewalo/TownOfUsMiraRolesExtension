using System.Linq;
using MiraAPI.GameOptions;
using TMPro;
using TownOfUs.Extensions;
using TownOfUs.Modules;
using TownOfUs.Utilities;
using TouMiraRolesExtension.Options.Roles.Crewmate;
using UnityEngine;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;

namespace TouMiraRolesExtension.Modules;

public static class MirageDecoySystem
{
    private sealed record ActiveDecoy(
        byte MirageId,
        FakePlayer Fake,
        Vector3 WorldPosition,
        float ExpiresAt,
        bool IsVisible);

    private static readonly Dictionary<byte, ActiveDecoy> ActiveByMirage = new();
    private static CosmeticsLayer? LocalOutlinedCosmetics;
    private static SpriteRenderer? LocalOutlinedBody;

    /// <summary>Decoy exists (primed-hidden or revealed-visible).</summary>
    public static bool HasAny(byte mirageId) => ActiveByMirage.ContainsKey(mirageId);

    /// <summary>Decoy is revealed and should be interactable/targetable.</summary>
    public static bool HasVisible(byte mirageId) =>
        ActiveByMirage.TryGetValue(mirageId, out var entry) && entry.IsVisible;

    public static void ClearLocalOutline()
    {
        if (LocalOutlinedCosmetics == null && LocalOutlinedBody == null)
        {
            return;
        }

        try
        {
            if (LocalOutlinedCosmetics != null)
            {
                LocalOutlinedCosmetics.SetOutline(false, new Il2CppSystem.Nullable<Color>(Color.clear));

                try
                {
                    LocalOutlinedCosmetics.currentBodySprite?.BodySprite?.SetOutline((Color?)null);
                }
                catch
                {
                    // ignore
                }
            }
            else if (LocalOutlinedBody != null)
            {
                LocalOutlinedBody.SetOutline((Color?)null);
            }
        }
        catch
        {
            // ignore
        }

        LocalOutlinedCosmetics = null;
        LocalOutlinedBody = null;
    }

    public static void UpdateLocalOutline(Vector2 from, float maxDistance, Color color)
    {
        if (ActiveByMirage.Count == 0)
        {
            ClearLocalOutline();
            return;
        }

        var bestDist = float.MaxValue;
        CosmeticsLayer? bestCosmetics = null;
        SpriteRenderer? bestBody = null;

        foreach (var kvp in ActiveByMirage)
        {
            if (!kvp.Value.IsVisible)
            {
                continue;
            }

            var fake = kvp.Value.Fake;
            if (fake.body == null)
            {
                continue;
            }

            var p = kvp.Value.WorldPosition;
            var v2 = new Vector2(p.x, p.y);
            var d = Vector2.Distance(from, v2);
            if (d > maxDistance || d >= bestDist)
            {
                continue;
            }

            var cosmetics = fake.body.GetComponentInChildren<CosmeticsLayer>(true);
            var body = cosmetics?.currentBodySprite?.BodySprite;
            if (body == null)
            {
                continue;
            }

            bestDist = d;
            bestCosmetics = cosmetics;
            bestBody = body;
        }

        if (bestCosmetics == null || bestBody == null)
        {
            ClearLocalOutline();
            return;
        }

        if ((LocalOutlinedCosmetics != null && LocalOutlinedCosmetics != bestCosmetics) ||
            (LocalOutlinedBody != null && LocalOutlinedBody != bestBody))
        {
            try
            {
                ClearLocalOutline();
            }
            catch
            {
                // ignore
            }
        }

        LocalOutlinedCosmetics = bestCosmetics;
        LocalOutlinedBody = bestBody;
        try
        {
            LocalOutlinedCosmetics.SetOutline(true, new Il2CppSystem.Nullable<Color>(color));
            LocalOutlinedBody.SetOutline((Color?)color);
        }
        catch
        {
            // ignore
        }
    }

    public static bool TryGetClosestDecoy(Vector2 from, float maxDistance, out byte mirageId, out Vector2 decoyPos)
    {
        mirageId = default;
        decoyPos = default;

        if (ActiveByMirage.Count == 0)
        {
            return false;
        }

        var bestDist = float.MaxValue;
        foreach (var kvp in ActiveByMirage)
        {
            if (!kvp.Value.IsVisible)
            {
                continue;
            }

            var p = kvp.Value.WorldPosition;
            var v2 = new Vector2(p.x, p.y);
            var d = Vector2.Distance(from, v2);
            if (d <= maxDistance && d < bestDist)
            {
                bestDist = d;
                mirageId = kvp.Key;
                decoyPos = v2;
            }
        }

        return bestDist != float.MaxValue;
    }

    public static bool TryGetActivePosition(byte mirageId, out Vector2 pos)
    {
        if (ActiveByMirage.TryGetValue(mirageId, out var entry))
        {
            pos = new Vector2(entry.WorldPosition.x, entry.WorldPosition.y);
            return true;
        }

        pos = default;
        return false;
    }

    public static void ClearAll()
    {
        ClearLocalOutline();
        foreach (var kvp in ActiveByMirage.Values)
        {
            kvp.Fake.Destroy();
        }
        ActiveByMirage.Clear();
    }

    public static void ClearForPlayer(byte mirageId)
    {
        if (ActiveByMirage.TryGetValue(mirageId, out var entry))
        {
            entry.Fake?.Destroy();
        }
        ActiveByMirage.Remove(mirageId);
        ClearLocalOutline();
    }

    /// <summary>
    /// Prime a decoy: create it once, at the Mirage's current pose, but keep it hidden.
    /// The Mirage player sees a faint preview; everyone else sees nothing yet.
    /// </summary>
    public static void PrimeDecoy(byte mirageId, PlayerControl appearanceSource, Vector3 worldPos, float zRot, bool flipX)
    {
        if (appearanceSource == null)
        {
            return;
        }

        ClearForPlayer(mirageId);

        var fake = new FakePlayer(appearanceSource);
        if (fake.body == null)
        {
            fake.Destroy();
            return;
        }

        fake.body.transform.position = worldPos;

        var alpha = (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == mirageId) ? 0.35f : 0f;
        SetAlpha(fake.body, alpha);

        ActiveByMirage[mirageId] = new ActiveDecoy(
            mirageId,
            fake,
            worldPos,
            float.PositiveInfinity,
            IsVisible: false);
    }

    /// <summary>
    /// Place a decoy: reveal the already-primed decoy to everyone and start its duration.
    /// If the decoy wasn't primed (late join / desync), we fall back to spawning it visible.
    /// </summary>
    public static void RevealOrSpawnDecoy(byte mirageId, PlayerControl appearanceSource, Vector3 worldPos, float zRot, bool flipX, float durationSeconds)
    {
        if (TryRevealExisting(mirageId, durationSeconds))
        {
            return;
        }

        if (appearanceSource == null)
        {
            return;
        }

        ClearForPlayer(mirageId);

        var fake = new FakePlayer(appearanceSource);
        if (fake.body == null)
        {
            fake.Destroy();
            return;
        }

        fake.body.transform.position = worldPos;
        SetAlpha(fake.body, 1f);

        var now = Time.time;
        var expiresAt = durationSeconds <= 0f ? float.PositiveInfinity : now + Mathf.Max(0.1f, durationSeconds);
        ActiveByMirage[mirageId] = new ActiveDecoy(mirageId, fake, worldPos, expiresAt, IsVisible: true);
    }

    private static bool TryRevealExisting(byte mirageId, float durationSeconds)
    {
        if (!ActiveByMirage.TryGetValue(mirageId, out var entry))
        {
            return false;
        }

        if (entry.Fake?.body == null)
        {
            ClearForPlayer(mirageId);
            return false;
        }

        var now = Time.time;
        var expiresAt = durationSeconds <= 0f ? float.PositiveInfinity : now + Mathf.Max(0.1f, durationSeconds);
        SetAlpha(entry.Fake.body, 1f);

        ActiveByMirage[mirageId] = entry with { ExpiresAt = expiresAt, IsVisible = true };
        return true;
    }

    public static bool TryRemoveDecoy(byte mirageId, out Vector2 lastPos)
    {
        if (!ActiveByMirage.TryGetValue(mirageId, out var entry))
        {
            lastPos = default;
            return false;
        }

        lastPos = new Vector2(entry.WorldPosition.x, entry.WorldPosition.y);
        ClearForPlayer(mirageId);
        return true;
    }

    public static void UpdateHost()
    {
        if (!AmongUsClient.Instance || !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        if (ActiveByMirage.Count == 0)
        {
            return;
        }

        var now = Time.time;
        var expired = ActiveByMirage
            .Where(kvp => kvp.Value.IsVisible && now >= kvp.Value.ExpiresAt)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var mirageId in expired)
        {
            var mirage = MiscUtils.PlayerById(mirageId);
            if (mirage == null)
            {
                ClearForPlayer(mirageId);
                continue;
            }

            Roles.Crewmate.MirageRole.RpcMirageDestroyDecoy(mirage);
        }
    }

    private static void SetAlpha(GameObject root, float alpha)
    {
        if (root == null)
        {
            return;
        }

        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null)
            {
                continue;
            }

            var c = sr.color;
            c.a = Mathf.Clamp01(alpha);
            sr.color = c;
        }

        foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmp == null)
            {
                continue;
            }

            var c = tmp.color;
            c.a = Mathf.Clamp01(alpha);
            tmp.color = c;
        }
    }

}