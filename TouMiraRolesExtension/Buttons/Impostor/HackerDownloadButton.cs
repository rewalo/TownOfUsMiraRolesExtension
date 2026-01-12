using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMiraRolesExtension.Assets;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Options.Roles.Impostor;
using TouMiraRolesExtension.Roles.Impostor;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMiraRolesExtension.Buttons.Impostor;

public sealed class HackerDownloadButton : TownOfUsRoleButton<HackerRole>
{
    private bool _isDownloading;
    private float _accumulatedSeconds;
    private float _lastUpdateTime;
    public override string Name => TouLocale.GetParsed("ExtensionRoleHackerDownload", "Download");

    public override BaseKeybind Keybind => Keybinds.SecondaryAction; // F (portable-equipment key)
    public override Color TextOutlineColor => TouExtensionColors.Hacker;
    public override float Cooldown => 0.5f;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.HackerDownloadButtonSprite;
    public override bool ZeroIsInfinite { get; set; } = true;

    public override bool Enabled(RoleBehaviour? role)
    {
        return base.Enabled(role) && !OptionGroupSingleton<HackerOptions>.Instance.SimpleModeJamOnly;
    }

    public override bool CanUse()
    {
        if (!base.CanUse())
        {
            return false;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied())
        {
            return false;
        }

        if (player.AreCommsAffected())
        {
            return false;
        }

        var opts = OptionGroupSingleton<HackerOptions>.Instance;

        var locked = HackerSystem.GetLockedSource(player.PlayerId);
        if (locked != HackerInfoSource.None)
        {
            return HackerSystem.IsPlayerNearSource(player, locked, opts.DownloadRange);
        }

        return HackerSystem.TryFindNearbyDownloadSource(player, opts.DownloadRange, out _);
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        var player = PlayerControl.LocalPlayer;
        if (Button == null || player == null)
        {
            return;
        }

        OverrideName(BuildDisplayName(player));

        if (MeetingHud.Instance)
        {
            StopDownload(resetTimer: false);
            return;
        }

        if (!_isDownloading)
        {
            return;
        }

        if (player.HasDied() || player.AreCommsAffected())
        {
            StopDownload(resetTimer: true);
            return;
        }

        var opts = OptionGroupSingleton<HackerOptions>.Instance;
        var locked = HackerSystem.GetLockedSource(player.PlayerId);
        if (locked == HackerInfoSource.None || !HackerSystem.IsPlayerNearSource(player, locked, opts.DownloadRange))
        {
            StopDownload(resetTimer: true);
            return;
        }

        var battery = HackerSystem.GetBatterySeconds(player.PlayerId);
        if (battery >= opts.MaxBatterySeconds - 0.01f)
        {
            StopDownload(resetTimer: true);
            return;
        }

        var now = Time.time;
        if (_lastUpdateTime <= 0f)
        {
            _lastUpdateTime = now;
        }

        _accumulatedSeconds += now - _lastUpdateTime;
        _lastUpdateTime = now;

        while (_accumulatedSeconds >= 1f)
        {
            _accumulatedSeconds -= 1f;
            battery = Mathf.Min(opts.MaxBatterySeconds, battery + opts.BatteryPerDownloadSecond);
            HackerSystem.SetBatterySeconds(player.PlayerId, battery);
        }

        Button.SetCooldownFill(1f - Mathf.Clamp01(_accumulatedSeconds));

        if (battery >= opts.MaxBatterySeconds - 0.01f)
        {
            StopDownload(resetTimer: true);
        }
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return;
        }

        if (_isDownloading)
        {
            StopDownload(resetTimer: true);
            return;
        }

        var opts = OptionGroupSingleton<HackerOptions>.Instance;
        var locked = HackerSystem.GetLockedSource(player.PlayerId);

        if (locked == HackerInfoSource.None)
        {
            if (!HackerSystem.TryFindNearbyDownloadSource(player, opts.DownloadRange, out var chosen) ||
                chosen == HackerInfoSource.None)
            {
                return;
            }

            HackerSystem.SetLockedSource(player.PlayerId, chosen);
            locked = chosen;
        }

        if (!HackerSystem.IsPlayerNearSource(player, locked, opts.DownloadRange))
        {
            return;
        }

        _isDownloading = true;
        _accumulatedSeconds = 0f;
        _lastUpdateTime = Time.time;
    }

    private void StopDownload(bool resetTimer)
    {
        _isDownloading = false;
        _accumulatedSeconds = 0f;
        _lastUpdateTime = 0f;
        if (resetTimer)
        {
            Timer = Cooldown;
        }
    }

    private static string BuildDisplayName(PlayerControl player)
    {
        var baseName = TouLocale.GetParsed("ExtensionRoleHackerDownload", "Download");
        var locked = HackerSystem.GetLockedSource(player.PlayerId);
        var suffix = locked != HackerInfoSource.None ? locked.ToString() : string.Empty;

        if (locked == HackerInfoSource.None)
        {
            var opts = OptionGroupSingleton<HackerOptions>.Instance;
            if (HackerSystem.TryFindNearbyDownloadSource(player, opts.DownloadRange, out var nearby) &&
                nearby != HackerInfoSource.None)
            {
                suffix = nearby.ToString();
            }
        }

        return string.IsNullOrEmpty(suffix) ? baseName : $"{baseName} ({suffix})";
    }
}