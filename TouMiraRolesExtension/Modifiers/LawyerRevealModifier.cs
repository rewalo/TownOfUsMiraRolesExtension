using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Modifiers;
using TouMiraRolesExtension.Utilities;
using TownOfUs.Modifiers;

namespace TouMiraRolesExtension.Modifiers;

/// <summary>
/// Reveal modifier for lawyer - makes lawyer's role visible to their client (defendant).
/// </summary>
public sealed class LawyerRevealModifier : RevealModifier
{
    private readonly RoleBehaviour _role;

    [HideFromIl2Cpp] public bool IsHiddenFromList => true;

    public LawyerRevealModifier(RoleBehaviour role)
        : base((int)ChangeRoleResult.Nothing, true, role)
    {
        _role = role;
    }

    public override string ModifierName => "Lawyer Revealed";

    public override void OnActivate()
    {
        base.OnActivate();
        if (RevealRole && ShownRole == null)
        {
            ShownRole = _role ?? (Player.Data?.Role as RoleBehaviour);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null)
        {
            Visible = false;
            return;
        }

        // Use LawyerUtils to check if local player is a client of THIS SPECIFIC lawyer
        Visible = LawyerUtils.IsClientOfLawyer(localPlayer, Player.PlayerId);
    }
}