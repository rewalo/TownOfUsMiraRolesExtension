using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMiraRolesExtension.Roles.Neutral;
using TownOfUs.Modules.Localization;
using TownOfUs.Options.Roles.Neutral;

namespace TouMiraRolesExtension.Options.Roles.Neutral;

public sealed class LawyerOptions : AbstractOptionGroup<LawyerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleLawyer", "Lawyer");

    [ModdedEnumOption("ExtensionOptionLawyerWinMode", typeof(LawyerWinMode),
        ["ExtensionOptionLawyerWinModeEnumWithClient", "ExtensionOptionLawyerWinModeEnumStealWin"])]
    public LawyerWinMode WinMode { get; set; } = LawyerWinMode.StealWin;

    [ModdedNumberOption("ExtensionOptionLawyerKillerClientChance", 0f, 100f, 10f, MiraNumberSuffixes.Percent)]
    public float KillerClientChance { get; set; } = 80f;

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

    [ModdedNumberOption("ExtensionOptionLawyerMaxObjections", 0f, 10f, 1f, MiraNumberSuffixes.None, "0")]
    public float MaxObjections { get; set; } = 1f;

    [ModdedNumberOption("ExtensionOptionLawyerMaxObjectionsPerMeeting", 0f, 10f, 1f, MiraNumberSuffixes.None, "0")]
    public float MaxObjectionsPerMeeting { get; set; } = 1f;
}

public enum LawyerWinMode
{
    WinWithClient,
    StealWin
}