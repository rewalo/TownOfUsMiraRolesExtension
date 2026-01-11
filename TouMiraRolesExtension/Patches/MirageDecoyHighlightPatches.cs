using AmongUs.GameOptions;
using HarmonyLib;
using TouMiraRolesExtension.Modules;
using TownOfUs.Buttons;
using UnityEngine;

namespace TouMiraRolesExtension.Patches;

[HarmonyPatch]
public static class MirageDecoyHighlightPatches
{
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPostfix]
    public static void HudManagerUpdatePostfix(HudManager __instance)
    {
        if (__instance == null || MeetingHud.Instance || PlayerControl.LocalPlayer == null)
        {
            return;
        }

        if (__instance.KillButton != null &&
            __instance.KillButton.isActiveAndEnabled &&
            !__instance.KillButton.isCoolingDown &&
            IsLocalNearAnyDecoy(GetKillDistance()))
        {
            __instance.KillButton.SetEnabled();
            ForceActionButtonVisualEnabled(__instance.KillButton);
        }
    }

    private static bool IsLocalNearAnyDecoy(float maxDistance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || (local.Data?.IsDead ?? false))
        {
            return false;
        }

        return MirageDecoySystem.TryGetClosestDecoy(local.GetTruePosition(), maxDistance, out _, out _);
    }

    private static float GetKillDistance()
    {
        var opts = GameOptionsManager.Instance?.currentNormalGameOptions;
        if (opts == null)
        {
            return 1.0f;
        }

        var killDistances = opts.GetFloatArray(FloatArrayOptionNames.KillDistances);
        var idx = System.Math.Clamp(opts.KillDistance, 0, killDistances.Length - 1);
        return killDistances[idx];
    }

    private static void ForceActionButtonVisualEnabled(ActionButton button)
    {
        try
        {
            var renderers = button.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers)
            {
                if (sr == null) continue;
                sr.color = Palette.EnabledColor;
                if (sr.material != null)
                {
                    sr.material.SetFloat("_Desat", 0f);
                }
            }

            var tmps = button.GetComponentsInChildren<TMPro.TMP_Text>(true);
            foreach (var tmp in tmps)
            {
                if (tmp == null) continue;
                tmp.color = Palette.EnabledColor;
            }
        }
        catch
        {
            // ignore
        }
    }
}