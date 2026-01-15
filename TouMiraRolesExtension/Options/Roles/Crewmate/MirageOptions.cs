using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMiraRolesExtension.Roles.Crewmate;
using TownOfUs.Modules.Localization;

namespace TouMiraRolesExtension.Options.Roles.Crewmate;

public sealed class MirageOptions : AbstractOptionGroup<MirageRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleMirage", "Mirage");

    [ModdedNumberOption("ExtensionOptionMirageInitialUses", 1f, 15f, 1f, MiraNumberSuffixes.None, "0")]
    public float InitialUses { get; set; } = 3f;

    public ModdedNumberOption UsesPerTasks { get; } =
        new("ExtensionOptionMirageUsesPerTasks", 0f, 0f, 15f, 1f, "Off", "#", MiraNumberSuffixes.None, "0");

    [ModdedNumberOption("ExtensionOptionMirageDecoyCooldown", 1f, 60f, 1f, MiraNumberSuffixes.Seconds)]
    public float DecoyCooldown { get; set; } = 25f;

    public ModdedNumberOption DecoyDuration { get; } =
        new("ExtensionOptionMirageDecoyDuration", 15f, 0f, 60f, 1f, "Off", "#", MiraNumberSuffixes.Seconds, "0");

    [ModdedEnumOption("ExtensionOptionMirageDecoyType", typeof(MirageDecoyType),
        ["ExtensionOptionMirageDecoyTypeEnumMirage", "ExtensionOptionMirageDecoyTypeEnumRandomPlayer"])]
    public MirageDecoyType DecoyType { get; set; } = MirageDecoyType.RandomPlayer;

    [ModdedNumberOption("ExtensionOptionMirageArrowTime", 0f, 15f, 0.5f, MiraNumberSuffixes.Seconds, "0", true)]
    public float ArrowTime { get; set; } = 5f;
}

public enum MirageDecoyType
{
    Mirage,
    RandomPlayer
}