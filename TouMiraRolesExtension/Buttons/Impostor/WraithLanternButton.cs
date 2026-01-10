using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using System.Collections;
using TouMiraRolesExtension.Assets;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Options.Roles.Impostor;
using TouMiraRolesExtension.Roles.Impostor;
using TownOfUs.Buttons;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Modules;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMiraRolesExtension.Buttons.Impostor;

public sealed class WraithLanternButton : TownOfUsRoleButton<WraithRole>
{
    private string _placeName = string.Empty;
    private string _returnName = string.Empty;
    private bool _isProcessingClick;

    public override string Name => TouLocale.GetParsed("ExtensionRoleWraithLanternPlace", "Lantern");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Wraith;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<WraithOptions>.Instance.LanternCooldown.Value + MapCooldown, 5f, 60f);
    public override float EffectDuration => OptionGroupSingleton<WraithOptions>.Instance.LanternDuration.Value;

    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.LanternButtonSprite;
    public override bool ZeroIsInfinite { get; set; } = true;

    public override bool Enabled(RoleBehaviour? role)
    {
        return base.Enabled(role) && OptionGroupSingleton<WraithOptions>.Instance.LanternEnabled;
    }

    public override bool CanUse()
    {
        if (EffectActive && PlayerControl.LocalPlayer != null &&
            WraithLanternSystem.HasActive(PlayerControl.LocalPlayer.PlayerId))
        {
            var player = PlayerControl.LocalPlayer;
            if (TimeLordRewindSystem.IsRewinding)
            {
                return false;
            }

            if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance)
            {
                return false;
            }

            if (player.HasDied())
            {
                return false;
            }

            if (!player.CanMove || player.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities))
            {
                return false;
            }

            return true;
        }

        return base.CanUse();
    }

    public override bool CanClick()
    {
        if (EffectActive && PlayerControl.LocalPlayer != null &&
            WraithLanternSystem.HasActive(PlayerControl.LocalPlayer.PlayerId))
        {
            var player = PlayerControl.LocalPlayer;
            if (TimeLordRewindSystem.IsRewinding)
            {
                return false;
            }

            if (HudManager.Instance.Chat.IsOpenOrOpening || MeetingHud.Instance)
            {
                return false;
            }

            if (player.HasDied())
            {
                return false;
            }

            if (!player.CanMove || player.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities))
            {
                return false;
            }

            return true;
        }

        return base.CanClick();
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (Button == null || PlayerControl.LocalPlayer == null)
        {
            return;
        }

        var hasActive = WraithLanternSystem.HasActive(PlayerControl.LocalPlayer.PlayerId);
        EnsureNames();

        if (hasActive)
        {
            OverrideName(_returnName);
        }
        else
        {
            OverrideName(_placeName);
        }
    }

    public override void ClickHandler()
    {
        if (_isProcessingClick)
        {
            return;
        }

        _isProcessingClick = true;

        try
        {
            if (!CanClick())
            {
                return;
            }

            var player = PlayerControl.LocalPlayer;
            if (player == null || player.HasDied())
            {
                return;
            }

            if (player.HasModifier<GlitchHackedModifier>() ||
                player.GetModifiers<DisabledModifier>().Any(x => !x.CanUseAbilities))
            {
                return;
            }

            if (EffectActive && WraithLanternSystem.TryGetActivePosition(player.PlayerId, out var pos))
            {
                WraithRole.RpcWraithReturnLantern(player, pos);

                Timer = Cooldown;
                EffectActive = false;
                Button?.SetDisabled();
                return;
            }

            base.ClickHandler();
        }
        finally
        {
            Coroutines.Start(ResetProcessingFlag());
        }
    }

    private IEnumerator ResetProcessingFlag()
    {
        yield return new WaitForSeconds(0.2f);
        _isProcessingClick = false;
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return;
        }

        WraithRole.RpcWraithPlaceLantern(player, player.GetTruePosition());
    }

    public override void OnEffectEnd()
    {
        base.OnEffectEnd();

        var player = PlayerControl.LocalPlayer;
        if (player != null && WraithLanternSystem.TryGetActivePosition(player.PlayerId, out var pos))
        {
            WraithRole.RpcWraithBreakLantern(player, pos);
        }
    }

    private void EnsureNames()
    {
        if (string.IsNullOrEmpty(_placeName))
        {
            _placeName = TouLocale.GetParsed("ExtensionRoleWraithLanternPlace", "Lantern");
        }

        if (string.IsNullOrEmpty(_returnName))
        {
            _returnName = TouLocale.GetParsed("ExtensionRoleWraithLanternReturn", "Return");
        }
    }
}