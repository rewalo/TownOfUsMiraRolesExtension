using HarmonyLib;
using MiraAPI.Hud;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Roles.Crewmate;
using TownOfUs.Buttons;
using TownOfUs.Utilities;

namespace TouMiraRolesExtension.Patches;

/// <summary>
/// Intercept TownOfUs button activations (mouse or keybind) and trigger a Mirage decoy if the local player is in range.
/// IMPORTANT: We patch the base TownOfUs button handler directly to avoid scanning/patching hundreds of derived button types,
/// which can trigger MonoMod/Harmony detour crashes on some IL2CPP + .NET 6 setups.
/// </summary>
[HarmonyPatch(typeof(TownOfUsButton), nameof(TownOfUsButton.ClickHandler))]
public static class MirageDecoyTownOfUsButtonPatches
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool Prefix(TownOfUsButton __instance)
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

    private static void SpendCooldownAndUses(CustomActionButton instance)
    {
        try
        {
            if (instance.LimitedUses)
            {
                instance.DecreaseUses(1);
            }

            instance.EffectActive = false;
            instance.Timer = instance.Cooldown;
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