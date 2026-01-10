using MiraAPI.GameOptions;
using MiraAPI.Modifiers.Types;
using TouMiraRolesExtension.Options.Roles.Impostor;

namespace TouMiraRolesExtension.Modifiers;

public sealed class WraithDashModifier : TimedModifier
{
    public override string ModifierName => "Dash";
    public override bool HideOnUi => true;
    public override float Duration => OptionGroupSingleton<WraithOptions>.Instance.DashDuration;

    public float SpeedFactor { get; private set; } = 1.75f;

    public override void OnActivate()
    {
        base.OnActivate();
        SpeedFactor = 1.75f;
    }

    public override void OnTimerComplete()
    {
        base.OnTimerComplete();
        SpeedFactor = 1f;
    }
}