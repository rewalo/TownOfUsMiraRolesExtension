using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Modifiers;

namespace TouMiraRolesExtension.Modifiers;

/// <summary>
/// Prevents the Serial Killer from reporting dead bodies.
/// </summary>
public sealed class SerialKillerNoReportModifier : BaseModifier
{
    [HideFromIl2Cpp] public bool IsHiddenFromList => true;
    public override string ModifierName => "No Report";
    public override bool HideOnUi => true;
}