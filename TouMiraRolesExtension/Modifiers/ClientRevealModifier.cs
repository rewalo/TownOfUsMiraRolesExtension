using TownOfUs.Modifiers;
using TownOfUs.Utilities;
using TouMiraRolesExtension.Roles.Neutral;
using TouMiraRolesExtension.Options.Roles.Neutral;
using MiraAPI.GameOptions;

namespace TouMiraRolesExtension.Modifiers;

/// <summary>
/// Reveal modifier for client - makes client's role visible to their lawyer (if setting is enabled).
/// </summary>
public sealed class ClientRevealModifier : RevealModifier
{
    private readonly RoleBehaviour _role;

    public ClientRevealModifier(RoleBehaviour role)
        : base((int)ChangeRoleResult.Nothing, true, role)
    {
        _role = role;
    }

    public override string ModifierName => "Client Revealed";

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

        if (!localPlayer.IsRole<LawyerRole>())
        {
            Visible = false;
            return;
        }

        var lawyerRole = localPlayer.GetRole<LawyerRole>();
        if (lawyerRole?.Client == null || lawyerRole.Client.PlayerId != Player.PlayerId)
        {
            Visible = false;
            return;
        }
        
        var options = OptionGroupSingleton<LawyerOptions>.Instance;
        Visible = options?.CanSeeClientRole == true;
    }
}