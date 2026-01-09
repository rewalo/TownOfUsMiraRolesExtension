using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Modifiers;

namespace TouMiraRolesExtension.Modifiers;

/// <summary>
/// Prevents the Serial Killer from venting after they kill someone in a vent.
/// </summary>
public sealed class SerialKillerNoVentModifier : BaseModifier
{
    [HideFromIl2Cpp] public bool IsHiddenFromList => true;
    public override string ModifierName => "No Vent";
    public override bool HideOnUi => true;
}

