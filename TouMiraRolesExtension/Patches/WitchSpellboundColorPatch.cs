using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMiraRolesExtension.Modifiers;
using TouMiraRolesExtension.Options.Roles.Impostor;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMiraRolesExtension.Patches;

/// <summary>
/// Patch to make spellbound players' names purple for everyone after first meeting if they have meetings left.
/// </summary>
[HarmonyPatch(typeof(PlayerRoleTextExtensions), nameof(PlayerRoleTextExtensions.UpdateTargetColor))]
public static class WitchSpellboundColorPatch
{
    [HarmonyPostfix]
    public static void UpdateTargetColorPostfix(ref Color __result, PlayerControl player, bool hidden = false)
    {
        if (player == null || !player.HasModifier<WitchSpellboundModifier>())
        {
            return;
        }

        if (MeetingHud.Instance != null)
        {
            __result = TouExtensionColors.Witch;
            return;
        }

        var modifier = player.GetModifier<WitchSpellboundModifier>();
        if (modifier != null)
        {
            var options = OptionGroupSingleton<WitchOptions>.Instance;
            var meetingsUntilDeath = options.MeetingsUntilDeath;
            var currentMeetingCount = Events.Impostor.WitchEvents.GetCurrentMeetingCount();
            var meetingsSinceSpell = currentMeetingCount - modifier.SpellCastMeeting;
            var meetingsRemaining = meetingsUntilDeath - meetingsSinceSpell;

            if (meetingsSinceSpell >= 1 && meetingsRemaining > 0)
            {
                __result = TouExtensionColors.Witch;
            }
        }
    }
}