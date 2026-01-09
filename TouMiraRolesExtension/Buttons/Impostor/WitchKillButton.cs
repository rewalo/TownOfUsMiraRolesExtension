using MiraAPI.GameOptions;
using MiraAPI.Networking;
using MiraAPI.Utilities.Assets;
using TouMiraRolesExtension.Roles.Impostor;
using TownOfUs.Utilities;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using TownOfUs.Modifiers;
using UnityEngine;
using MiraAPI.Hud;

namespace TouMiraRolesExtension.Buttons.Impostor;

public sealed class WitchKillButton : TownOfUsKillRoleButton<WitchRole, PlayerControl>, IDiseaseableButton, IKillButton
{
    public override string Name => TranslationController.Instance.GetStringWithDefault(StringNames.KillLabel, "Kill");
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Witch;
    public override float Cooldown => PlayerControl.LocalPlayer.GetKillCooldown();
    public override LoadableAsset<Sprite> Sprite => TouAssets.KillSprite;

    public override bool ZeroIsInfinite { get; set; } = true;

    public void SetDiseasedTimer(float multiplier)
    {
        SetTimer(Cooldown * multiplier);
    }

    protected override void OnClick()
    {
        if (Target == null)
        {
            Error("Witch Kill: Target is null");
            return;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            Error("Witch Kill: LocalPlayer is null");
            return;
        }

        player.RpcCustomMurder(Target);
        
        var spellButton = CustomButtonSingleton<WitchSpellButton>.Instance;
        if (spellButton != null)
        {
            spellButton.SetTimer(spellButton.Cooldown);
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
        Timer = Cooldown;
    }

    public override PlayerControl? GetTarget()
    {
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance);
    }
}