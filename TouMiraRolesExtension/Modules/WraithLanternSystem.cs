using TouMiraRolesExtension.Assets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMiraRolesExtension.Modules;

public static class WraithLanternSystem
{
    private const float LanternScale = 0.65f;

    private sealed record ActiveLantern(Vector2 Position, float PlacedAt, float ExpiresAt);

    private static readonly Dictionary<byte, ActiveLantern> Active = new();
    private static readonly List<Vector2> BrokenEvidence = new();
    private static readonly Dictionary<byte, GameObject> ActiveVisuals = new();
    private static readonly List<GameObject> BrokenVisuals = new();

    public static void ClearAll()
    {
        Active.Clear();
        BrokenEvidence.Clear();
        ClearAllVisuals();
    }

    public static void ClearForPlayer(byte wraithId)
    {
        Active.Remove(wraithId);
        if (ActiveVisuals.TryGetValue(wraithId, out var go) && go != null)
        {
            Object.Destroy(go);
        }
        ActiveVisuals.Remove(wraithId);
    }

    public static bool HasActive(byte wraithId) => Active.ContainsKey(wraithId);

    public static bool TryGetActivePosition(byte wraithId, out Vector2 pos)
    {
        if (Active.TryGetValue(wraithId, out var entry))
        {
            pos = entry.Position;
            return true;
        }

        pos = default;
        return false;
    }

    public static void PlaceLantern(byte wraithId, Vector2 pos, float durationSeconds)
    {
        var now = Time.time;
        Active[wraithId] = new ActiveLantern(pos, now, now + Mathf.Max(0f, durationSeconds));

        if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == wraithId)
        {
            SpawnOrMoveActiveVisual(wraithId, pos);
        }
    }

    public static bool TryReturnLantern(byte wraithId, out Vector2 pos)
    {
        if (!Active.TryGetValue(wraithId, out var entry))
        {
            pos = default;
            return false;
        }

        Active.Remove(wraithId);
        pos = entry.Position;

        if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == wraithId)
        {
            RemoveActiveVisual(wraithId);
        }

        return true;
    }

    public static void BreakLantern(byte wraithId, Vector2 pos)
    {
        Active.Remove(wraithId);
        RemoveActiveVisual(wraithId);

        BrokenEvidence.Add(pos);
        SpawnBrokenVisual(pos);
    }


    private static void TryCopyVentRenderSettings(SpriteRenderer targetRenderer, out float zAxis)
    {
        zAxis = 0f;
        if (targetRenderer == null)
        {
            return;
        }

        var vent = Object.FindObjectOfType<Vent>();
        if (vent == null)
        {
            return;
        }

        zAxis = vent.transform.position.z;
        var ventRenderer = vent.GetComponent<SpriteRenderer>();
        if (ventRenderer == null)
        {
            return;
        }

        targetRenderer.sortingLayerID = ventRenderer.sortingLayerID;
        targetRenderer.sortingOrder = ventRenderer.sortingOrder;
        targetRenderer.sharedMaterial = ventRenderer.sharedMaterial;
    }

    private static void ClearAllVisuals()
    {
        foreach (var visual in ActiveVisuals.Values.Where(v => v != null))
        {
            Object.Destroy(visual);
        }
        ActiveVisuals.Clear();

        foreach (var go in BrokenVisuals.Where(g => g != null))
        {
            Object.Destroy(go);
        }
        BrokenVisuals.Clear();
    }

    private static void SpawnOrMoveActiveVisual(byte wraithId, Vector2 pos)
    {
        if (!ActiveVisuals.TryGetValue(wraithId, out var go) || go == null)
        {
            go = new GameObject("WraithLantern");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = TouExtensionAssets.LanternSprite.LoadAsset();
            sr.color = new Color(1f, 1f, 1f, 0.45f);
            TryCopyVentRenderSettings(sr, out _);
            go.transform.localScale = new Vector3(LanternScale, LanternScale, 1f);
            ActiveVisuals[wraithId] = go;
        }

        var sr2 = go.GetComponent<SpriteRenderer>();
        TryCopyVentRenderSettings(sr2, out var zAxis);
        go.transform.position = new Vector3(pos.x, pos.y, zAxis);
    }

    private static void RemoveActiveVisual(byte wraithId)
    {
        if (ActiveVisuals.TryGetValue(wraithId, out var go) && go != null)
        {
            Object.Destroy(go);
        }
        ActiveVisuals.Remove(wraithId);
    }

    private static void SpawnBrokenVisual(Vector2 pos)
    {
        var go = new GameObject("WraithBrokenLantern");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = TouExtensionAssets.BrokenLanternSprite.LoadAsset();
        sr.color = new Color(0.7f, 0.7f, 0.7f, 0.95f);
        TryCopyVentRenderSettings(sr, out var zAxis);
        go.transform.localScale = new Vector3(LanternScale, LanternScale, 1f);
        go.transform.position = new Vector3(pos.x, pos.y, zAxis);
        BrokenVisuals.Add(go);
    }
}