using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using Reactor.Utilities;
using System.Collections;
using TouMiraRolesExtension.Assets;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Networking;
using TouMiraRolesExtension.Options.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMiraRolesExtension.Roles.Impostor;

public sealed class HackerRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    private static bool IsHackerRole(PlayerControl? player)
    {
        if (player == null || player.Data?.Role == null)
        {
            return false;
        }

        if (player.Data.Role is HackerRole)
        {
            return true;
        }

        try
        {
            return player.Data.Role.Role == (RoleTypes)RoleId.Get<HackerRole>();
        }
        catch
        {
            return false;
        }
    }

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

        if (!IsHackerRole(hacker))
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

        var host = PlayerControl.LocalPlayer;
        if (host == null)
        {
            return;
        }

        var newCharges = HackerSystem.GetJamCharges(hacker.PlayerId);
        HackerSystem.SetJamCharges(hacker.PlayerId, newCharges);
        HackerSystem.ActivateJam(opts.JamDurationSeconds);

        Coroutines.Start(CoBroadcastJamNextFrame(host, hacker.PlayerId, newCharges, opts.JamDurationSeconds));
    }

    private static IEnumerator CoBroadcastJamNextFrame(PlayerControl host, byte hackerId, byte newCharges, float durationSeconds)
    {
        yield return null;

        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
        {
            yield break;
        }

        if (host == null || PlayerControl.LocalPlayer == null)
        {
            yield break;
        }

        if (!HackerSystem.IsJammed)
        {
            yield break;
        }

        RpcHackerSetJamCharges(host, hackerId, newCharges);
        RpcHackerStartJam(host, hackerId, durationSeconds);
    }

    [MethodRpc((uint)ExtensionRpc.HackerStartJam, LocalHandling = RpcLocalHandling.Before)]
    public static void RpcHackerStartJam(PlayerControl sender, byte hackerId, float durationSeconds)
    {
        HackerSystem.ActivateJam(durationSeconds);


        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer != null && localPlayer.PlayerId == hackerId && IsHackerRole(localPlayer))
        {
            TouAudio.PlaySound(TouExtensionAudio.HackerJamSound);
        }
    }

    [MethodRpc((uint)ExtensionRpc.HackerSetJamCharges, LocalHandling = RpcLocalHandling.Before)]
    public static void RpcHackerSetJamCharges(PlayerControl sender, byte targetPlayerId, byte charges)
    {
        HackerSystem.SetJamCharges(targetPlayerId, charges);
    }

    [MethodRpc((uint)ExtensionRpc.HackerResetRound, LocalHandling = RpcLocalHandling.Before)]
    public static void RpcHackerResetRound(PlayerControl sender)
    {
        HackerSystem.ResetRoundState();
    }
}