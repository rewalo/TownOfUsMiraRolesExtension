using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using TouMiraRolesExtension.Assets;
using TouMiraRolesExtension.Modifiers;
using TouMiraRolesExtension.Options.Roles.Impostor;
using TouMiraRolesExtension.Roles.Impostor;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Roles.Crewmate;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMiraRolesExtension.Buttons.Impostor;

public sealed class WitchSpellButton : TownOfUsKillRoleButton<WitchRole, PlayerControl>, IDiseaseableButton, IKillButton
{
    private float _spellProgress;
    private PlayerControl? _spellTarget;
    private float _spellStartTime;

    public override string Name => TouLocale.Get("ExtensionRoleWitchSpell", "Spell");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Witch;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<WitchOptions>.Instance.SpellCooldown + MapCooldown, 5f, 120f);
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.SpellButtonSprite;

    public override bool ZeroIsInfinite { get; set; } = true;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
    }
    public void SetDiseasedTimer(float multiplier)
    {
        SetTimer(Cooldown * multiplier);
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        var options = OptionGroupSingleton<WitchOptions>.Instance;
        var player = PlayerControl.LocalPlayer;

        if (_spellTarget != null && !_spellTarget.HasDied() && player != null)
        {
            var distance = Vector2.Distance(player.GetTruePosition(), _spellTarget.GetTruePosition());
            if (distance <= Distance && Timer <= 0)
            {
                var elapsed = Time.time - _spellStartTime;
                _spellProgress = Mathf.Clamp01(elapsed / options.SpellCastingDuration);

                if (_spellProgress >= 1f)
                {
                    WitchRole.RpcWitchSpell(player, _spellTarget);
                    _spellTarget = null;
                    _spellProgress = 0f;
                    var newCooldown = Cooldown + options.AdditionalCooldown;
                    SetTimer(newCooldown);

                    var killButton = CustomButtonSingleton<WitchKillButton>.Instance;
                    if (killButton != null)
                    {
                        killButton.SetTimer(killButton.Cooldown);
                    }
                    player.SetKillTimer(killButton?.Cooldown ?? player.GetKillCooldown());
                }
            }
            else
            {
                _spellTarget = null;
                _spellProgress = 0f;
            }
        }
        else if (_spellTarget != null && (_spellTarget.HasDied() || _spellTarget == null))
        {
            _spellTarget = null;
            _spellProgress = 0f;
        }


        if (_spellTarget != null && _spellProgress > 0f)
        {
            Button?.SetCooldownFill(1f - _spellProgress);
        }

        base.FixedUpdate(playerControl);
    }

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (!base.IsTargetValid(target) || target == null)
        {
            return false;
        }

        return CanSpellTarget(target);
    }

    private static bool CanSpellTarget(PlayerControl? target)
    {
        if (target == null)
        {
            return false;
        }

        if (target.HasModifier<WitchSpellboundModifier>())
        {
            return false;
        }

        if (target.IsRole<SpyRole>() || target.IsImpostor())
        {
            return false;
        }

        return true;
    }

    public override PlayerControl? GetTarget()
    {
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance);
    }

    protected override void OnClick()
    {
        if (Target == null)
        {
            Error("Witch Spell: Target is null");
            return;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            Error("Witch Spell: LocalPlayer is null");
            return;
        }

        if (_spellTarget == null || _spellTarget.PlayerId != Target.PlayerId)
        {
            _spellTarget = Target;
            _spellStartTime = Time.time;
            _spellProgress = 0f;
        }
    }

    public override void ClickHandler()
    {
        if (!CanClick())
        {
            return;
        }

        OnClick();
        Button?.SetDisabled();
    }
}