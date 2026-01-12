using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMiraRolesExtension.Roles.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMiraRolesExtension.Options.Roles.Impostor;

public sealed class HackerOptions : AbstractOptionGroup<HackerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleHacker", "Hacker");

    [ModdedNumberOption("ExtensionOptionHackerMaxBatterySeconds", 3f, 15f, 1f, MiraNumberSuffixes.Seconds)]
    public float MaxBatterySeconds { get; set; } = 10f;

    [ModdedNumberOption("ExtensionOptionHackerBatteryPerDownloadSecond", 1f, 4f, 1f, MiraNumberSuffixes.Seconds)]
    public float BatteryPerDownloadSecond { get; set; } = 2f;

    [ModdedNumberOption("ExtensionOptionHackerDownloadRange", 0.5f, 2.5f, 0.25f, MiraNumberSuffixes.None)]
    public float DownloadRange { get; set; } = 1f;

    [ModdedToggleOption("ExtensionOptionHackerMoveWithDevice")]
    public bool MoveWithDevice { get; set; } = true;

    [ModdedNumberOption("ExtensionOptionHackerJamChargesPerKill", 0f, 5f, 1f, MiraNumberSuffixes.None)]
    public float JamChargesPerKill { get; set; } = 1f;

    [ModdedNumberOption("ExtensionOptionHackerJamMaxCharges", 1f, 10f, 1f, MiraNumberSuffixes.None)]
    public float JamMaxCharges { get; set; } = 3f;

    [ModdedNumberOption("ExtensionOptionHackerJamCooldown", 10f, 35f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float JamCooldownSeconds { get; set; } = 20f;

    [ModdedNumberOption("ExtensionOptionHackerJamDuration", 5f, 20f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float JamDurationSeconds { get; set; } = 10f;

    public bool JamEnabled => JamChargesPerKill > 0f && JamMaxCharges > 0f;
}