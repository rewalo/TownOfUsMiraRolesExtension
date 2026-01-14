using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Roles.Crewmate;
using MiraAPI.Hud;
using TownOfUs.Buttons;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMiraRolesExtension.Patches;

public static class MirageDecoyTownOfUsTargetButtonPatches
{
    // Same motivation as MirageDecoyTownOfUsButtonPatches: patch the base handlers directly.
    [HarmonyPatch(typeof(TownOfUsTargetButton<PlayerControl>), nameof(TownOfUsTargetButton<PlayerControl>.ClickHandler))]
    private static class PlayerTargetClickHandlerPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(object __instance)
        {
            if (__instance is Buttons.Crewmate.MirageDecoyButton)
            {
                return true;
            }

            if (!TryTriggerFromLocalPlayer(GetDistance(__instance)))
            {
                return true;
            }

            SpendCooldownAndUses(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(TownOfUsTargetButton<PlayerControl>), nameof(TownOfUsTargetButton<PlayerControl>.FixedUpdateHandler))]
    private static class PlayerTargetFixedUpdateHandlerPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(object __instance)
        {
            if (MeetingHud.Instance)
            {
                return;
            }

            var local = PlayerControl.LocalPlayer;
            if (local == null || (local.Data?.IsDead ?? false))
            {
                return;
            }

            var actionButton = GetActionButton(__instance);
            if (actionButton == null || !actionButton.isActiveAndEnabled || actionButton.isCoolingDown)
            {
                MirageDecoySystem.ClearLocalOutline();
                return;
            }

            var distance = GetDistance(__instance);
            if (!MirageDecoySystem.TryGetClosestDecoy(local.GetTruePosition(), distance, out _, out _))
            {
                MirageDecoySystem.ClearLocalOutline();
                return;
            }

            actionButton.SetEnabled();
            ForceActionButtonVisualEnabled(actionButton);
            MirageDecoySystem.UpdateLocalOutline(local.GetTruePosition(), distance, GetOutlineColor(__instance));
        }

        private static ActionButton? GetActionButton(object instance)
        {
            try
            {
                var prop = instance.GetType().GetProperty("Button", BindingFlags.Instance | BindingFlags.Public);
                return prop?.GetValue(instance) as ActionButton;
            }
            catch
            {
                return null;
            }
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

        private static Color GetOutlineColor(object buttonInstance)
        {
            try
            {
                var roleProp = buttonInstance.GetType().GetProperty("Role", BindingFlags.Instance | BindingFlags.Public);
                var roleObj = roleProp?.GetValue(buttonInstance);
                if (roleObj != null)
                {
                    var teamColorProp = roleObj.GetType().GetProperty("TeamColor", BindingFlags.Instance | BindingFlags.Public);
                    var teamColorObj = teamColorProp?.GetValue(roleObj);
                    if (teamColorObj is Color c)
                    {
                        return c;
                    }
                }
            }
            catch
            {
                // ignore
            }

            return Palette.EnabledColor;
        }
    }

    [HarmonyPatch(typeof(TownOfUsTargetButton<DeadBody>), nameof(TownOfUsTargetButton<DeadBody>.ClickHandler))]
    private static class BodyTargetClickHandlerPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(object __instance)
        {
            if (__instance is Buttons.Crewmate.MirageDecoyButton)
            {
                return true;
            }

            if (!TryTriggerFromLocalPlayer(GetDistance(__instance)))
            {
                return true;
            }

            SpendCooldownAndUses(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(TownOfUsTargetButton<DeadBody>), nameof(TownOfUsTargetButton<DeadBody>.FixedUpdateHandler))]
    private static class BodyTargetFixedUpdateHandlerPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(object __instance) => PlayerTargetFixedUpdateHandlerPatch.Postfix(__instance);
    }

    [HarmonyPatch(typeof(TownOfUsTargetButton<Vent>), nameof(TownOfUsTargetButton<Vent>.ClickHandler))]
    private static class VentTargetClickHandlerPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(object __instance)
        {
            if (__instance is Buttons.Crewmate.MirageDecoyButton)
            {
                return true;
            }

            if (!TryTriggerFromLocalPlayer(GetDistance(__instance)))
            {
                return true;
            }

            SpendCooldownAndUses(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(TownOfUsTargetButton<Vent>), nameof(TownOfUsTargetButton<Vent>.FixedUpdateHandler))]
    private static class VentTargetFixedUpdateHandlerPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(object __instance) => PlayerTargetFixedUpdateHandlerPatch.Postfix(__instance);
    }

    private static bool TryTriggerFromLocalPlayer(float maxDistance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.HasDied() || MeetingHud.Instance)
        {
            return false;
        }

        var from = local.GetTruePosition();
        if (!MirageDecoySystem.TryGetClosestDecoy(from, maxDistance, out var mirageId, out var decoyPos))
        {
            return false;
        }

        var mirage = MiscUtils.PlayerById(mirageId);
        if (mirage == null || mirage.HasDied() || !mirage.IsRole<MirageRole>())
        {
            return false;
        }

        MirageRole.RpcMirageTriggerDecoy(mirage, local, decoyPos);
        return true;
    }

    private static void SpendCooldownAndUses(object instance)
    {
        try
        {
            if (instance is CustomActionButton btn)
            {
                if (btn.LimitedUses)
                {
                    btn.DecreaseUses(1);
                }

                btn.EffectActive = false;
                btn.Timer = btn.Cooldown;
            }
        }
        catch
        {
            // ignore
        }
    }

    private static float GetDistance(object instance)
    {
        try
        {
            var prop = instance.GetType().GetProperty("Distance", BindingFlags.Instance | BindingFlags.Public);
            if (prop != null && prop.PropertyType == typeof(float))
            {
                var boxed = prop.GetValue(instance);
                if (boxed is float f)
                {
                    return f;
                }
            }
        }
        catch
        {
            // ignore
        }

        return 1.25f;
    }
}