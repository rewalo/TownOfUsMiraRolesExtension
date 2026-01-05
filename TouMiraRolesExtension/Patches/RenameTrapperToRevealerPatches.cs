using HarmonyLib;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Modules.Localization;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace TouMiraRolesExtension.Patches;

/// <summary>
/// Patches to rename the existing Trapper role (ground traps) to Revealer.
/// </summary>
[HarmonyPatch]
public static class RenameTrapperToRevealerPatches
{
    [HarmonyPatch(typeof(TrapperRole), "get_LocaleKey")]
    [HarmonyPostfix]
    public static void TrapperRoleLocaleKeyPostfix(ref string __result)
    {
        __result = "Revealer";
    }

    [HarmonyPatch(typeof(TrapperRole), "get_RoleName")]
    [HarmonyPostfix]
    public static void TrapperRoleRoleNamePostfix(ref string __result)
    {
        __result = TouLocale.Get("TouRoleRevealer", "Revealer");
    }

    [HarmonyPatch(typeof(TrapperRole), "get_RoleDescription")]
    [HarmonyPostfix]
    public static void TrapperRoleRoleDescriptionPostfix(ref string __result)
    {
        __result = TouLocale.GetParsed("TouRoleRevealerIntroBlurb");
    }

    [HarmonyPatch(typeof(TrapperRole), "get_RoleLongDescription")]
    [HarmonyPostfix]
    public static void TrapperRoleRoleLongDescriptionPostfix(ref string __result)
    {
        __result = TouLocale.GetParsed("TouRoleRevealerTabDescription");
    }
}