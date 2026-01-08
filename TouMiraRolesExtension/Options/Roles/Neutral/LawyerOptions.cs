using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using TownOfUs.Options.Roles.Neutral;
using TouMiraRolesExtension.Roles.Neutral;
using TownOfUs.Modules.Localization;

namespace TouMiraRolesExtension.Options.Roles.Neutral;

public sealed class LawyerOptions : AbstractOptionGroup<LawyerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleLawyer", "Lawyer");

    public ModdedEnumOption OnClientDeath { get; } =
    new("ExtensionOptionLawyerBecomesClientDeath", (int)BecomeOptions.Jester, typeof(BecomeOptions),
        ["CrewmateKeyword", "TouRoleAmnesiac", "TouRoleSurvivor", "TouRoleMercenary", "TouRoleJester"])
    {
        Visible = () => !OptionGroupSingleton<LawyerOptions>.Instance.DieOnClientDeath,
    };

    [ModdedToggleOption("ExtensionOptionLawyerDieOnClientDeath")]
    public bool DieOnClientDeath { get; set; }

    [ModdedToggleOption("ExtensionOptionLawyerGetVotedOutWithClient")]
    public bool GetVotedOutWithClient { get; set; } = true;

    [ModdedToggleOption("ExtensionOptionLawyerCanSeeClientRole")]
    public bool CanSeeClientRole { get; set; } = true;
}