using HarmonyLib;
using TouMiraRolesExtension.Roles.Neutral;
using TouMiraRolesExtension.Utilities;
using TownOfUs.Roles.Neutral;

namespace TouMiraRolesExtension.Patches;

/// <summary>
/// Match TownOfUs behavior for Executioner/Fairy: if an Amnesiac remembers a dead Lawyer,
/// the new Lawyer should keep the same Client as the remembered Lawyer.
/// </summary>
[HarmonyPatch(typeof(AmnesiacRole), nameof(AmnesiacRole.RpcRemember))]
public static class AmnesiacLawyerRememberPatch
{
    [HarmonyPostfix]
    public static void Postfix(PlayerControl player, PlayerControl target)
    {
        if (!AmongUsClient.Instance || !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        if (player == null || target == null)
        {
            return;
        }

        if (player.Data?.Role is not LawyerRole)
        {
            return;
        }

        var client = LawyerUtils.FindClientForLawyer(target.PlayerId);
        if (client == null)
        {
            return;
        }

        LawyerRole.RpcSetLawyerClient(player, client);
    }
}