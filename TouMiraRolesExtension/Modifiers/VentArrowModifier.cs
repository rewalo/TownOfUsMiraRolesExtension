using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Modifiers.Types;
using Reactor.Utilities.Extensions;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMiraRolesExtension.Modifiers;

/// <summary>
/// Short-lived arrow pointing at a world position (e.g., a vent).
/// Only shows on the owning client.
/// </summary>
public sealed class VentArrowModifier(Vector3 target, Color color, float durationSeconds) : TimedModifier
{
    public override string ModifierName => "Vent Arrow";
    public override bool HideOnUi => true;
    public override bool AutoStart => true;
    public override bool RemoveOnComplete => true;
    public override float Duration => Mathf.Max(0.05f, durationSeconds);

    [HideFromIl2Cpp] public bool IsHiddenFromList => true;

    public Vector3 Target { get; } = target;
    public Color Color { get; } = color;

    private ArrowBehaviour? _arrow;

    public override void OnActivate()
    {
        base.OnActivate();

        if (!Player.AmOwner)
        {
            return;
        }

        _arrow = MiscUtils.CreateArrow(Player.transform, Color);
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!Player.AmOwner)
        {
            return;
        }

        if (_arrow != null)
        {
            _arrow.target = Target;
            _arrow.Update();
        }
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();

        if (_arrow != null && !_arrow.IsDestroyedOrNull())
        {
            _arrow.gameObject.Destroy();
            _arrow.Destroy();
        }

        _arrow = null;
    }
}