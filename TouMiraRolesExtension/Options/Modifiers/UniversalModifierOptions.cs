using MiraAPI.GameOptions;
using MiraAPI.GameOptions.Attributes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using CluelessCensorTypeEnum = TouMiraRolesExtension.Options.Modifiers.CluelessCensorType;

namespace TouMiraRolesExtension.Options.Modifiers;

public enum CluelessCensorType
{
    WhiteBars,
    Asterisks,
    QuestionMarks,
    Remove
}

public sealed class UniversalModifierOptions : AbstractOptionGroup
{
    public override string GroupName => "Universal Modifiers (Extension)";
    public override bool ShowInModifiersMenu => true;
    public override uint GroupPriority => 1;

    [ModdedNumberOption("ExtensionModifierCluelessAmount", 0, 15)]
    public float CluelessAmount { get; set; } = 0;

    public ModdedNumberOption CluelessChance { get; } =
        new("ExtensionModifierCluelessChance", 50f, 0, 100f, 10f, MiraNumberSuffixes.Percent)
        {
            Visible = () => OptionGroupSingleton<UniversalModifierOptions>.Instance.CluelessAmount > 0
        };

    private static readonly string[] CluelessCensorTypeValues =
    [
        "ExtensionModifierCluelessCensorTypeEnumWhiteBars",
        "ExtensionModifierCluelessCensorTypeEnumAsterisks",
        "ExtensionModifierCluelessCensorTypeEnumQuestionMarks",
        "ExtensionModifierCluelessCensorTypeEnumRemove"
    ];

    public ModdedEnumOption<CluelessCensorType> CluelessCensorType { get; } =
        new("ExtensionModifierCluelessCensorType", CluelessCensorTypeEnum.QuestionMarks, CluelessCensorTypeValues)
        {
            Visible = () => OptionGroupSingleton<UniversalModifierOptions>.Instance.CluelessAmount > 0
        };
}