using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMiraRolesExtension.Roles.Neutral;
using TownOfUs.Modules.Localization;

namespace TouMiraRolesExtension.Options.Roles.Neutral;

public sealed class SerialKillerOptions : AbstractOptionGroup<SerialKillerRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleSerialKiller", "Serial Killer");

    [ModdedToggleOption("ExtensionOptionSerialKillerCanReportBodies")]
    public bool CanReportBodies { get; set; } = false;

    [ModdedEnumOption("ExtensionOptionSerialKillerVentKillTargets", typeof(VentKillTargets),
        ["ExtensionOptionSerialKillerVentKillTargetsEnumImpostors", 
         "ExtensionOptionSerialKillerVentKillTargetsEnumImpNK", 
         "ExtensionOptionSerialKillerVentKillTargetsEnumImpNeutrals", 
         "ExtensionOptionSerialKillerVentKillTargetsEnumAny"])]
    public VentKillTargets VentKillTargets { get; set; } = VentKillTargets.ImpNK;

    [ModdedToggleOption("ExtensionOptionSerialKillerManiacMode")]
    public bool ManiacMode { get; set; } = false;

    public ModdedNumberOption ManiacTimer { get; } = new("ExtensionOptionSerialKillerManiacTimer", 60f, 5f, 60f, 5f, MiraNumberSuffixes.Seconds, "0.0")
    {
        Visible = () => OptionGroupSingleton<SerialKillerOptions>.Instance.ManiacMode
    };

    public ModdedNumberOption ManiacCooldown { get; } = new("ExtensionOptionSerialKillerManiacCooldown", 15f, 0f, 30f, 0.5f, MiraNumberSuffixes.Seconds, "0.0")
    {
        Visible = () => OptionGroupSingleton<SerialKillerOptions>.Instance.ManiacMode
    };
}

public enum VentKillTargets
{
    Impostors,
    ImpNK,
    ImpNeutrals,
    Any
}