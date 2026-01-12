using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using TouMiraRolesExtension.Assets;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Networking;
using TouMiraRolesExtension.Options.Roles.Impostor;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMiraRolesExtension.Roles.Impostor;

public sealed class HackerRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Insight;
    public string LocaleKey => "Hacker";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Hacker;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorSupport;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = true,
        Icon = TouExtensionImpAssets.HackerRole,
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities
    {
        get
        {
            return
            [
                new(
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}Download", "Download"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}DownloadWikiDescription"),
                    TouExtensionImpAssets.HackerDownloadButtonSprite),
                new(
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}Device", "Device"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}DeviceWikiDescription"),
                    TouExtensionImpAssets.HackerDeviceGenericSprite),
                new(
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}Jam", "Jam"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}JamWikiDescription"),
                    TouExtensionImpAssets.HackerJamButtonSprite)
            ];
        }
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    [MethodRpc((uint)ExtensionRpc.HackerActivateJam)]
    public static void RpcHackerActivateJam(PlayerControl hacker)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        if (hacker?.Data?.Role is not HackerRole)
        {
            return;
        }

        var opts = OptionGroupSingleton<HackerOptions>.Instance;
        if (!opts.JamEnabled)
        {
            return;
        }

        if (!HackerSystem.TryConsumeJamCharge(hacker.PlayerId))
        {
            return;
        }

        RpcHackerSetJamCharges(PlayerControl.LocalPlayer, hacker.PlayerId, HackerSystem.GetJamCharges(hacker.PlayerId));
        RpcHackerStartJam(PlayerControl.LocalPlayer, opts.JamDurationSeconds);
    }

    [MethodRpc((uint)ExtensionRpc.HackerStartJam)]
    public static void RpcHackerStartJam(PlayerControl sender, float durationSeconds)
    {
        HackerSystem.ActivateJam(durationSeconds);
    }

    [MethodRpc((uint)ExtensionRpc.HackerSetJamCharges)]
    public static void RpcHackerSetJamCharges(PlayerControl sender, byte targetPlayerId, byte charges)
    {
        HackerSystem.SetJamCharges(targetPlayerId, charges);
    }

    [MethodRpc((uint)ExtensionRpc.HackerResetRound)]
    public static void RpcHackerResetRound(PlayerControl sender)
    {
        HackerSystem.ResetRoundState();
    }
}