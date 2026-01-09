using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TouMiraRolesExtension.Roles.Impostor;
using TownOfUs.Modules.Localization;

namespace TouMiraRolesExtension.Options.Roles.Impostor;

public sealed class WitchOptions : AbstractOptionGroup<WitchRole>
{
    public override string GroupName => TouLocale.Get("ExtensionRoleWitch", "Witch");

    [ModdedNumberOption("ExtensionOptionWitchSpellCooldown", 5f, 120f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float SpellCooldown { get; set; } = 37.5f;

    [ModdedNumberOption("ExtensionOptionWitchAdditionalCooldown", 0f, 30f, 2.5f, MiraNumberSuffixes.Seconds)]
    public float AdditionalCooldown { get; set; } = 5f;

    [ModdedToggleOption("ExtensionOptionWitchCanSpellEveryone")]
    public bool CanSpellEveryone { get; set; } = true;

    [ModdedNumberOption("ExtensionOptionWitchSpellCastingDuration", 0.5f, 5f, 0.5f, MiraNumberSuffixes.Seconds)]
    public float SpellCastingDuration { get; set; } = 2.5f;

    [ModdedNumberOption("ExtensionOptionWitchMeetingsUntilDeath", 1f, 5f, 1f, MiraNumberSuffixes.None)]
    public float MeetingsUntilDeath { get; set; } = 1f;
}