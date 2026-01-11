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

[HarmonyPatch]
public static class MirageDecoyTownOfUsTargetButtonPatches
{
    [HarmonyPatch]
    private static class ClickHandlerPatch
    {
        public static IEnumerable<MethodBase> TargetMethods() =>
            GetTouTargetButtonConcreteMethods("ClickHandler", argTypes: null);

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

    [HarmonyPatch]
    private static class FixedUpdateHandlerPatch
    {
        public static IEnumerable<MethodBase> TargetMethods() =>
            GetTouTargetButtonConcreteMethods("FixedUpdateHandler", new[] { typeof(PlayerControl) });

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

    private static IEnumerable<MethodBase> GetTouTargetButtonConcreteMethods(string methodName, Type[]? argTypes)
    {
        var methods = new HashSet<MethodBase>();
        foreach (var t in AccessTools.AllTypes().Where(t => t != null))
        {
            if (!t.IsClass || t.IsAbstract || t.ContainsGenericParameters)
            {
                continue;
            }

            var b = FindClosedGenericBase(t, typeof(TownOfUsTargetButton<>));
            if (b == null || b.ContainsGenericParameters)
            {
                continue;
            }

            MethodInfo? m = argTypes == null
                ? AccessTools.Method(t, methodName)
                : AccessTools.Method(t, methodName, argTypes);

            if (m != null)
            {
                methods.Add(m);
            }
        }

        foreach (var m in methods)
        {
            yield return m;
        }
    }

    private static Type? FindClosedGenericBase(Type type, Type genericBaseDef)
    {
        var cur = type;
        while (cur != null && cur != typeof(object))
        {
            var bt = cur.BaseType;
            if (bt == null)
            {
                return null;
            }

            if (bt.IsGenericType && bt.GetGenericTypeDefinition() == genericBaseDef)
            {
                return bt;
            }

            cur = bt;
        }

        return null;
    }
}