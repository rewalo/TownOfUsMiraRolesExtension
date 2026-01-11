using HarmonyLib;
using System.Reflection;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Roles.Crewmate;
using MiraAPI.Hud;
using TownOfUs.Buttons;
using TownOfUs.Utilities;

namespace TouMiraRolesExtension.Patches;

/// <summary>
/// Intercept TownOfUs button activations (mouse or keybind) and trigger a Mirage decoy if the local player is in range.
/// This is done dynamically to catch role buttons that override ClickHandler.
/// </summary>
[HarmonyPatch]
public static class MirageDecoyTownOfUsButtonPatches
{
    [HarmonyPatch]
    private static class ClickHandlerPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            var seen = new HashSet<MethodBase>();
            foreach (var t in AccessTools.AllTypes().Where(t => t != null))
            {
                if (!t.IsClass || t.IsAbstract || t.ContainsGenericParameters)
                {
                    continue;
                }

                if (!typeof(TownOfUsButton).IsAssignableFrom(t))
                {
                    continue;
                }

                var m = AccessTools.Method(t, "ClickHandler");
                if (m != null && seen.Add(m))
                {
                    yield return m;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(object __instance)
        {
            if (__instance is Buttons.Crewmate.MirageDecoyButton)
            {
                return true;
            }

            if (!TryTriggerFromLocalPlayer(1.25f))
            {
                return true;
            }

            SpendCooldownAndUses(__instance);
            return false;
        }
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
}