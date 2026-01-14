using HarmonyLib;
using MiraAPI.Modifiers;
using TouMiraRolesExtension.Modifiers;
using TownOfUs.Utilities;

namespace TouMiraRolesExtension.Patches;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
public static class WitchMeetingHighlightPatch
{
    [HarmonyPostfix]
    public static void UpdatePostfix(MeetingHud __instance)
    {
        if (__instance == null || __instance.playerStates == null)
        {
            return;
        }

        foreach (var voteArea in __instance.playerStates)
        {
            if (voteArea == null)
            {
                continue;
            }

            var player = MiscUtils.PlayerById(voteArea.TargetPlayerId);
            if (player == null || !player.HasModifier<WitchSpellboundModifier>())
            {
                continue;
            }

            voteArea.NameText.color = TouExtensionColors.Witch;
        }
    }
}