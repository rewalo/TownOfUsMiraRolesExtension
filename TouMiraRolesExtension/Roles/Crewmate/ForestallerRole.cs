using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using TownOfUs;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using UnityEngine;

namespace TouMiraRolesExtension.Roles.Crewmate;

/// <summary>
/// Forestaller role: when they complete all tasks, sabotages are disabled (while they are alive).
/// They are revealed in meetings after completing all tasks.
/// </summary>
public sealed class ForestallerRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Insight;
    public string LocaleKey => "Forestaller";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Forestaller");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + TownOfUs.Utilities.MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Forestaller;
    public MiraAPI.Roles.ModdedRoleTeams Team => MiraAPI.Roles.ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateSupport;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TownOfUs.Assets.TouRoleIcons.Engineer,
        IntroSound = TownOfUs.Assets.TouAudio.EngineerIntroSound
    };

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        Modules.ForestallerSystem.ClearForPlayer(targetPlayer.PlayerId);
    }

    [HideFromIl2Cpp]
    public void CheckTaskRequirements()
    {
        Modules.ForestallerSystem.TryActivateIfCompletedAllTasks(Player);
    }
}