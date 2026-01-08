using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;

namespace TouMiraRolesExtension.Options;

public sealed class GeneralOptions : AbstractOptionGroup
{
    public override string GroupName => "General";
    public override uint GroupPriority => 1;

    [ModdedToggleOption("Lawyer/Client Gets A Private Chat")]
    public bool LawyerChat { get; set; } = true;
}