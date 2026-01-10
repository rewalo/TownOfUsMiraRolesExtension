using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMiraRolesExtension.Roles.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMiraRolesExtension.Options.Roles.Impostor;

public sealed class WraithOptions : AbstractOptionGroup<WraithRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleWraith", "Wraith");

    [ModdedNumberOption("ExtensionOptionWraithDashCooldown", 10f, 60f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float DashCooldown { get; set; } = 60f;

    [ModdedNumberOption("ExtensionOptionWraithDashDuration", 3f, 15f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float DashDuration { get; set; } = 3f;

    [ModdedToggleOption("ExtensionOptionWraithLantern")]
    public bool LanternEnabled { get; set; } = true;

    public ModdedNumberOption LanternCooldown { get; } = new("ExtensionOptionWraithLanternCooldown", 45f, 5f, 60f, 2.5f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<WraithOptions>.Instance.LanternEnabled
    };

    public ModdedNumberOption LanternDuration { get; } = new("ExtensionOptionWraithLanternDuration", 7f, 1f, 20f, 0.5f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<WraithOptions>.Instance.LanternEnabled
    };

    public ModdedNumberOption InvisibleDuration { get; } = new("ExtensionOptionWraithInvisibleDuration", 1.5f, 0f, 5f, 0.25f, MiraNumberSuffixes.Seconds)
    {
        Visible = () => OptionGroupSingleton<WraithOptions>.Instance.LanternEnabled
    };
}