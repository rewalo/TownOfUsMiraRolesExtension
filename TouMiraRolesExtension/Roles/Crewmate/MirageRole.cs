using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using TouMiraRolesExtension.Assets;
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

public sealed class MirageRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable, IUnguessable
{
    public DoomableType DoomHintType => DoomableType.Insight;
    public string LocaleKey => "Mirage";
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
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}Decoy", "Decoy"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}DecoyWikiDescription"),
                    TouExtensionCrewAssets.DecoyButtonSprite)
            ];
        }
    }

    public Color RoleColor => TouExtensionColors.Mirage;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateSupport;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouExtensionAssets.MirageRoleIcon
    };
    public bool IsGuessable => OptionGroupSingleton<MirageOptions>.Instance.DecoyType != MirageDecoyType.Mirage;
    public RoleBehaviour AppearAs => this;

    public void LobbyStart()
    {
        MirageDecoySystem.ClearAll();
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        MirageDecoySystem.ClearForPlayer(targetPlayer.PlayerId);
    }

    [MethodRpc((uint)ExtensionRpc.MiragePlaceDecoy)]
    public static void RpcMiragePlaceDecoy(
        PlayerControl mirage,
        PlayerControl appearanceSource,
        Vector2 pos,
        float z,
        float durationSeconds,
        float zRot,
        bool flipX)
    {
        if (mirage?.Data?.Role is not MirageRole)
        {
            return;
        }

        if (mirage.AmOwner)
        {
            TouAudio.PlaySound(TouExtensionAudio.DecoyPlaceSound);
        }

        var worldPos = new Vector3(pos.x, pos.y, z);
        MirageDecoySystem.RevealOrSpawnDecoy(mirage.PlayerId, appearanceSource, worldPos, zRot, flipX, durationSeconds);
    }

    [MethodRpc((uint)ExtensionRpc.MiragePrimeDecoy)]
    public static void RpcMiragePrimeDecoy(
        PlayerControl mirage,
        PlayerControl appearanceSource,
        Vector2 pos,
        float z,
        float zRot,
        bool flipX)
    {
        if (mirage?.Data?.Role is not MirageRole)
        {
            return;
        }

        var worldPos = new Vector3(pos.x, pos.y, z);
        MirageDecoySystem.PrimeDecoy(mirage.PlayerId, appearanceSource, worldPos, zRot, flipX);
    }

    [MethodRpc((uint)ExtensionRpc.MirageDestroyDecoy)]
    public static void RpcMirageDestroyDecoy(PlayerControl mirage)
    {
        if (mirage?.Data?.Role is not MirageRole)
        {
            return;
        }

        if (mirage.AmOwner)
        {
            TouAudio.PlaySound(TouExtensionAudio.DecoyDestroySound);
        }

        if (MirageDecoySystem.TryRemoveDecoy(mirage.PlayerId, out _) && mirage.AmOwner)
        {
            Buttons.Crewmate.MirageDecoyButton.LocalInstance?.StartCooldownAndReset();
        }
    }

    [MethodRpc((uint)ExtensionRpc.MirageTriggerDecoy)]
    public static void RpcMirageTriggerDecoy(PlayerControl mirage, PlayerControl interactor, Vector2 pos)
    {
        if (mirage?.Data?.Role is not MirageRole)
        {
            return;
        }

        if (mirage.AmOwner)
        {
            TouAudio.PlaySound(TouExtensionAudio.DecoyDestroySound);
        }

        MirageDecoySystem.TryRemoveDecoy(mirage.PlayerId, out _);

        if (interactor != null && interactor.AmOwner)
        {
            Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Mirage));
            TouAudio.PlaySound(TouAudio.DiscoveredSound);
            var msg = TouLocale.GetParsed("ExtensionRoleMirageInteractorTriggered", "You interacted with a decoy!");
            var notif = Helpers.CreateAndShowNotification(
                msg,
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionAssets.MirageRoleIcon.LoadAsset());
            notif.AdjustNotification();
        }

        if (mirage.AmOwner)
        {
            Buttons.Crewmate.MirageDecoyButton.LocalInstance?.StartCooldownAndReset();

            Coroutines.Start(MiscUtils.CoFlash(TouExtensionColors.Mirage));
            TouAudio.PlaySound(TouAudio.DiscoveredSound);

            var msg = TouLocale.Get("ExtensionRoleMirageOwnerTriggered", "Your decoy was triggered!");

            var notif = Helpers.CreateAndShowNotification(
                msg,
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionAssets.MirageRoleIcon.LoadAsset());
            notif.AdjustNotification();

            var arrowDur = OptionGroupSingleton<MirageOptions>.Instance.ArrowTime;
            if (arrowDur > 0f && mirage.TryGetComponent<ModifierComponent>(out var modComp))
            {
                modComp.AddModifier(new VentArrowModifier(pos, TouExtensionColors.Mirage, arrowDur));
            }
        }
    }
}