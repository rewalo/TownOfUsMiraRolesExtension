using System.Globalization;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMiraRolesExtension.Assets;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Options.Roles.Impostor;
using TouMiraRolesExtension.Roles.Impostor;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMiraRolesExtension.Buttons.Impostor;

public sealed class HackerJamButton : TownOfUsRoleButton<HackerRole>
{
    public override string Name => TouLocale.GetParsed("ExtensionRoleHackerJam", "Jam");
    public override BaseKeybind Keybind => OptionGroupSingleton<HackerOptions>.Instance.SimpleModeJamOnly
        ? Keybinds.SecondaryAction // F in simple mode
        : Keybinds.ModifierAction; // I when not in simple mode
    public override Color TextOutlineColor => TouExtensionColors.Hacker;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<HackerOptions>.Instance.JamCooldownSeconds + MapCooldown, 5f, 120f);
    public override float EffectDuration => OptionGroupSingleton<HackerOptions>.Instance.JamDurationSeconds;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.HackerJamButtonSprite;
    public override bool ZeroIsInfinite { get; set; } = true;

    public override bool Enabled(RoleBehaviour? role)
    {
        return base.Enabled(role) && OptionGroupSingleton<HackerOptions>.Instance.JamEnabled;
    }

    public override bool CanUse()
    {
        if (!base.CanUse())
        {
            return false;
        }

        if (HackerSystem.IsJammed)
        {
            return false;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return false;
        }

        return Timer <= 0f && HackerSystem.GetJamCharges(player.PlayerId) > 0;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        if (Button == null || PlayerControl.LocalPlayer == null)
        {
            return;
        }

        var charges = HackerSystem.GetJamCharges(PlayerControl.LocalPlayer.PlayerId);
        Button.usesRemainingText.gameObject.SetActive(true);
        Button.usesRemainingSprite.gameObject.SetActive(true);
        Button.usesRemainingText.text = charges.ToString(CultureInfo.InvariantCulture);

        if (EffectActive && !HackerSystem.IsJammed)
        {
            EffectActive = false;
            Timer = Cooldown;
        }
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return;
        }

        HackerRole.RpcHackerActivateJam(player);
    }
}