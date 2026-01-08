using TownOfUs.Modifiers;
using MiraAPI.Modifiers;

namespace TouMiraRolesExtension.Modifiers;

/// <summary>
/// Reveal modifier for lawyer - makes lawyer's role visible to their client (defendant).
/// </summary>
public sealed class LawyerRevealModifier(RoleBehaviour role)
    : RevealModifier((int)ChangeRoleResult.Nothing, true, role)
{
    public override string ModifierName => "Lawyer Revealed";

    public override void OnActivate()
    {
        base.OnActivate();
        if (RevealRole && ShownRole == null)
        {
            ShownRole = role ?? (Player.Data?.Role as RoleBehaviour);
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

        var lawyerModifier = localPlayer.GetModifiers<LawyerTargetModifier>()
            .FirstOrDefault(x => x.OwnerId == Player.PlayerId);
        
        Visible = lawyerModifier != null;
    }
}