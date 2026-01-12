using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using TouMiraRolesExtension.Assets;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Options.Roles.Crewmate;
using TouMiraRolesExtension.Roles.Crewmate;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMiraRolesExtension.Buttons.Crewmate;

public sealed class MirageDecoyButton : TownOfUsRoleButton<MirageRole>
{
    private enum Stage
    {
        Prime,
        Place,
        Destroy
    }

    private const float PostPlaceLockSeconds = 3f;

    public static MirageDecoyButton? LocalInstance { get; private set; }

    private Stage _stage = Stage.Prime;
    private byte? _primedAppearanceId;
    private Vector3 _primedWorldPos;
    private float _destroyUnlockAt;
    private bool _isProcessingClick;

    public override string Name => TouLocale.GetParsed("ExtensionRoleMirageDecoyPrime", "Prime");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Mirage;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<MirageOptions>.Instance.DecoyCooldown + MapCooldown, 5f, 120f);
    public override float EffectDuration => OptionGroupSingleton<MirageOptions>.Instance.DecoyDuration;
    public override int MaxUses => (int)OptionGroupSingleton<MirageOptions>.Instance.InitialUses;
    public override LoadableAsset<Sprite> Sprite => TouExtensionCrewAssets.DecoyButtonSprite;
    public override bool ZeroIsInfinite { get; set; } = true;

    public override void ClickHandler()
    {
        if (_isProcessingClick)
        {
            return;
        }

        _isProcessingClick = true;

        try
        {
            if (!CanUse())
            {
                return;
            }

            OnClick();
        }
        finally
        {
            Coroutines.Start(ResetProcessingFlag());
        }
    }

    private System.Collections.IEnumerator ResetProcessingFlag()
    {
        yield return new WaitForSeconds(0.2f);
        _isProcessingClick = false;
    }

    public override bool CanUse()
    {
        if (TimeLordRewindSystem.IsRewinding || MeetingHud.Instance || HudManager.Instance.Chat.IsOpenOrOpening)
        {
            return false;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied())
        {
            return false;
        }

        if (_stage == Stage.Destroy)
        {
            return Time.time >= _destroyUnlockAt;
        }

        return base.CanUse() && Timer <= 0f && (!LimitedUses || UsesLeft > 0);
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        var player = PlayerControl.LocalPlayer;
        if (player == null || !player.IsRole<MirageRole>())
        {
            return;
        }

        LocalInstance = this;

        var hasVisible = MirageDecoySystem.HasVisible(player.PlayerId);
        var hasAny = MirageDecoySystem.HasAny(player.PlayerId);

        if (hasVisible)
        {
            _stage = Stage.Destroy;

            if (!EffectActive && EffectDuration > 0f)
            {
                EffectActive = true;
                Timer = EffectDuration;
            }
        }
        else if (hasAny)
        {
            _stage = Stage.Place;
            EffectActive = false;
        }
        else if (_stage == Stage.Destroy || _stage == Stage.Place)
        {
            _stage = Stage.Prime;
            _primedAppearanceId = null;
            EffectActive = false;
        }

        switch (_stage)
        {
            case Stage.Prime:
                OverrideName(TouLocale.GetParsed("ExtensionRoleMirageDecoyPrime", "Prime"));
                break;
            case Stage.Place:
                OverrideName(TouLocale.GetParsed("ExtensionRoleMirageDecoyPlace", "Place"));
                break;
            case Stage.Destroy:
                OverrideName(TouLocale.GetParsed("ExtensionRoleMirageDecoyDestroy", "Destroy"));
                break;
        }

        if (_stage == Stage.Destroy &&
            hasVisible &&
            EffectDuration <= 0f &&
            Button != null)
        {
            var lockRemaining = _destroyUnlockAt - Time.time;
            if (lockRemaining > 0f)
            {
                try
                {
                    Button.SetFillUp(lockRemaining, PostPlaceLockSeconds);
                    Button.cooldownTimerText.text = Mathf.Ceil(lockRemaining)
                        .ToString(CooldownTimerFormatString, System.Globalization.NumberFormatInfo.InvariantInfo);
                    Button.cooldownTimerText.gameObject.SetActive(true);
                }
                catch
                {
                    // ignore
                }
            }
        }

        if (MeetingHud.Instance)
        {
            // no-op
        }
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return;
        }

        if (_stage == Stage.Destroy)
        {
            EffectActive = false;
            MirageRole.RpcMirageDestroyDecoy(player);
            return;
        }

        if (_stage == Stage.Prime)
        {
            _stage = Stage.Place;
            PrimeAtCurrentPosition(player);
            return;
        }

        var appearance = GetPrimedAppearanceSource(player) ?? player;

        if (LimitedUses)
        {
            if (UsesLeft <= 0)
            {
                return;
            }

            UsesLeft--;
            SetUses(UsesLeft);
        }

        MirageRole.RpcMiragePlaceDecoy(
            player,
            appearance,
            new Vector2(_primedWorldPos.x, _primedWorldPos.y),
            _primedWorldPos.z,
            OptionGroupSingleton<MirageOptions>.Instance.DecoyDuration,
            0f,
            false);
        _stage = Stage.Destroy;
        _destroyUnlockAt = Time.time + PostPlaceLockSeconds;
    }

    public void StartCooldownAndReset()
    {
        _stage = Stage.Prime;
        _primedAppearanceId = null;
        EffectActive = false;
        Timer = Cooldown;
        Button?.SetDisabled();
    }

    private static PlayerControl? GetAppearanceSource(PlayerControl mirage)
    {
        var type = OptionGroupSingleton<MirageOptions>.Instance.DecoyType;
        if (type == MirageDecoyType.Mirage)
        {
            return mirage;
        }

        var candidates = PlayerControl.AllPlayerControls.ToArray()
            .Where(p => p != null && !p.HasDied() && p.PlayerId != mirage.PlayerId)
            .ToList();

        return candidates.Count == 0 ? mirage : candidates.Random();
    }

    private void PrimeAtCurrentPosition(PlayerControl mirage)
    {
        var appearance = GetAppearanceSource(mirage) ?? mirage;
        _primedAppearanceId = appearance.PlayerId;
        _primedWorldPos = mirage.transform.position;

        MirageRole.RpcMiragePrimeDecoy(
            mirage,
            appearance,
            new Vector2(_primedWorldPos.x, _primedWorldPos.y),
            _primedWorldPos.z,
            0f,
            false);
    }

    private PlayerControl? GetPrimedAppearanceSource(PlayerControl mirage)
    {
        if (_primedAppearanceId.HasValue)
        {
            return MiscUtils.PlayerById(_primedAppearanceId.Value);
        }

        return GetAppearanceSource(mirage);
    }

    public override void OnEffectEnd()
    {
        base.OnEffectEnd();

        var player = PlayerControl.LocalPlayer;
        if (player != null && player.IsRole<MirageRole>() && MirageDecoySystem.HasVisible(player.PlayerId))
        {
            MirageRole.RpcMirageDestroyDecoy(player);
        }
    }
}