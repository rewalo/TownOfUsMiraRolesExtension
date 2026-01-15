using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using System.Collections;
using TouMiraRolesExtension.Modifiers;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Networking;
using TouMiraRolesExtension.Options.Roles.Crewmate;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMiraRolesExtension.Roles.Crewmate;

/// <summary>
/// Trapper role: Places traps on vents that immobilize players who use them.
/// </summary>
public sealed class TrapperRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public override bool IsAffectedByComms => false;

    public DoomableType DoomHintType => DoomableType.Trickster;
    public string LocaleKey => "Trapper";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities
    {
        get
        {
            return
            [
                new(
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}Trap", "Trap"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}TrapWikiDescription"),
                    TouCrewAssets.TrapSprite)
            ];
        }
    }

    public Color RoleColor => TouExtensionColors.Trapper;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateInvestigative;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouRoleIcons.Trapper,
        IntroSound = TouAudio.EngineerIntroSound
    };

    public void LobbyStart()
    {
        VentTrapSystem.ClearAll();
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);

        VentTrapSystem.ClearOwnedBy(targetPlayer.PlayerId);
    }

    [MethodRpc((uint)ExtensionRpc.TrapperPlaceTrap)]
    public static void RpcTrapperPlaceTrap(PlayerControl trapper, int ventId)
    {
        if (trapper == null || trapper.Data?.Role is not TrapperRole)
        {
            return;
        }

        VentTrapSystem.Place(ventId, trapper.PlayerId);

        if (trapper.AmOwner)
        {
            var vent = Helpers.GetVentById(ventId);
            var room = vent != null ? MiscUtils.GetRoomName(vent.transform.position) : TouLocale.Get("Unknown", "Unknown");
            var msg = TouLocale.GetParsed("ExtensionRoleTrapperPlaced", "Trapped a vent in <room>!", new()
            {
                ["<room>"] = room
            });

            var notif = Helpers.CreateAndShowNotification(
                msg,
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouRoleIcons.Trapper.LoadAsset());
            notif.AdjustNotification();
        }
    }

    [MethodRpc((uint)ExtensionRpc.TrapperTriggerTrap)]
    public static IEnumerator RpcTrapperTriggerTrap(PlayerControl trapper, int ventId, byte victimId)
    {
        if (trapper == null)
        {
            yield break;
        }

        VentTrapSystem.Remove(ventId);

        var victim = MiscUtils.PlayerById(victimId);
        if (victim == null)
        {
            yield break;
        }

        if (!VentTrapSystem.IsEligibleToBeTrapped(victim))
        {
            yield break;
        }

        var vent = Helpers.GetVentById(ventId);
        var ventTopPos = vent != null ? VentTrapSystem.GetVentTopPosition(vent) : (Vector2)victim.transform.position;

        yield return new WaitForSeconds(0.3f);

        if (victim.AmOwner)
        {
            CoApplyTrapToVictimAfterVentAnim(victim, ventId, ventTopPos, vent);
        }
        else if (trapper.AmOwner)
        {
            Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Trapper));

            var arrowDur = OptionGroupSingleton<TrapperOptions>.Instance.ArrowDuration;
            if (trapper.TryGetComponent<ModifierComponent>(out var modifierComp))
            {
                modifierComp.AddModifier(new VentArrowModifier(ventTopPos, TouExtensionColors.Trapper, arrowDur));
            }

            var room = vent != null ? MiscUtils.GetRoomName(vent.transform.position) : TouLocale.Get("Unknown", "Unknown");
            var msg = TouLocale.GetParsed("ExtensionRoleTrapperTriggered", "Your trap was triggered in <room>!", new()
            {
                ["<room>"] = room
            });

            var notif = Helpers.CreateAndShowNotification(
                msg,
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouRoleIcons.Trapper.LoadAsset());
            notif.AdjustNotification();
        }
    }

    private static void CoApplyTrapToVictimAfterVentAnim(PlayerControl victim, int ventId, Vector2 ventTopPos, Vent? vent)
    {
        if (victim == null || victim.HasDied() || !victim.AmOwner)
        {
            return;
        }

        var dur = OptionGroupSingleton<TrapperOptions>.Instance.Trappeduration;
        if (victim.TryGetComponent<ModifierComponent>(out var modifierComp))
        {
            modifierComp.AddModifier(new TrappedOnVentModifier(ventTopPos, dur, ventId));
        }

        Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Trapper));
        TouAudio.PlaySound(TouAudio.DiscoveredSound);

        var room = vent != null ? MiscUtils.GetRoomName(vent.transform.position) : TouLocale.Get("Unknown", "Unknown");
        var msg = TouLocale.GetParsed("ExtensionRoleTrapperCaught", "You were caught in a trap in <room>!", new()
        {
            ["<room>"] = room
        });

        var notif = Helpers.CreateAndShowNotification(
            msg,
            Color.white,
            new Vector3(0f, 1f, -20f),
            spr: TouRoleIcons.Trapper.LoadAsset());
        notif.AdjustNotification();
    }
}