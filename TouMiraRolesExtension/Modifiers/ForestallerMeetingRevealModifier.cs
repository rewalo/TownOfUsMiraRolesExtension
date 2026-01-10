using Il2CppInterop.Runtime.Attributes;
using TownOfUs.Modifiers;
using TownOfUs.Utilities;
using TouMiraRolesExtension.Roles.Crewmate;

namespace TouMiraRolesExtension.Modifiers;

/// <summary>
/// Reveals the Forestaller's role permanently after the reveal meeting (Mayor-style behavior).
/// </summary>
public sealed class ForestallerMeetingRevealModifier : RevealModifier
{
    private readonly RoleBehaviour _role;

    [HideFromIl2Cpp] public bool IsHiddenFromList => true;

    public ForestallerMeetingRevealModifier(RoleBehaviour role)
        : base((int)ChangeRoleResult.Nothing, true, role)
    {
        _role = role;
    }

    public override string ModifierName => "Forestaller Revealed";

    public override void OnDeath(DeathReason reason)
    {
        base.OnDeath(reason);
        ModifierComponent?.RemoveModifier(this);
    }

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

        Visible = Player != null &&
                  Player.Data?.Role is ForestallerRole &&
                  !Player.HasDied() &&
                  Modules.ForestallerSystem.IsForestallerRevealed(Player.PlayerId);
    }
}