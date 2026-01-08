using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Options.Roles.Crewmate;
using TouMiraRolesExtension.Roles.Crewmate;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMiraRolesExtension.Buttons.Crewmate;

public sealed class TrapperTrapButton : TownOfUsRoleButton<TrapperRole, Vent>
{
    private static readonly ContactFilter2D Filter = Helpers.CreateFilter(Constants.Usables);

    public override string Name => TouLocale.GetParsed("ExtensionRoleTrapperTrap", "Trap");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Trapper;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<TrapperOptions>.Instance.TrapCooldown + MapCooldown, 5f, 120f);
    public override int MaxUses => (int)OptionGroupSingleton<TrapperOptions>.Instance.MaxTraps;
    public override LoadableAsset<Sprite> Sprite => TouCrewAssets.TrapSprite;

    public override bool IsTargetValid(Vent? target)
    {
        return base.IsTargetValid(target) && target != null && !VentTrapSystem.IsTrapped(target.Id);
    }

    public override Vent? GetTarget()
    {
        var vent = PlayerControl.LocalPlayer.GetNearestObjectOfType<Vent>(Distance / 4, Filter) ??
                   PlayerControl.LocalPlayer.GetNearestObjectOfType<Vent>(Distance / 3, Filter) ??
                   PlayerControl.LocalPlayer.GetNearestObjectOfType<Vent>(Distance / 2, Filter) ??
                   PlayerControl.LocalPlayer.GetNearestObjectOfType<Vent>(Distance, Filter);

        if (vent != null && PlayerControl.LocalPlayer.CanUseVent(vent))
        {
            return vent;
        }

        return null;
    }

    public override bool CanUse()
    {
        var newTarget = GetTarget();
        if (newTarget != Target)
        {
            Target?.SetOutline(false, false);
        }

        Target = IsTargetValid(newTarget) ? newTarget : null;
        SetOutline(true);

        return base.CanUse() && Timer <= 0 && Target != null && UsesLeft > 0;
    }

    protected override void OnClick()
    {
        if (Target == null)
        {
            return;
        }

        TrapperRole.RpcTrapperPlaceTrap(PlayerControl.LocalPlayer, Target.Id);
    }
}