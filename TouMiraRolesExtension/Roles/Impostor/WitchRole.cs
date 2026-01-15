using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using TouMiraRolesExtension.Assets;
using TouMiraRolesExtension.Modifiers;
using TouMiraRolesExtension.Networking;
using TouMiraRolesExtension.Options.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMiraRolesExtension.Roles.Impostor;

public sealed class WitchRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    private static readonly List<PlayerControl> _pendingNotifications = new();

    public DoomableType DoomHintType => DoomableType.Trickster;
    public string LocaleKey => "Witch";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Witch;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorKilling;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = false,
        Icon = TouRoleIcons.Witch
    };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities
    {
        get
        {
            return new List<CustomButtonWikiDescription>
            {
                new(TouLocale.GetParsed($"ExtensionRole{LocaleKey}Spell", "Spell"),
                    TouLocale.GetParsed($"ExtensionRole{LocaleKey}SpellWikiDescription"),
                    TouExtensionImpAssets.SpellButtonSprite)
            };
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

    [MethodRpc((uint)ExtensionRpc.WitchSpell)]
    public static void RpcWitchSpell(PlayerControl witch, PlayerControl target)
    {
        if (witch.Data.Role is not WitchRole)
        {
            Error("RpcWitchSpell - Invalid witch");
            return;
        }

        if (target == null || target.HasDied())
        {
            return;
        }

        var shouldSpell = true;

        if (target.HasModifier<GuardianAngelProtectModifier>())
        {
            shouldSpell = false;
        }

        if (shouldSpell && !target.HasModifier<WitchSpellboundModifier>())
        {
            target.AddModifier<WitchSpellboundModifier>(witch.PlayerId);
        }

        if (shouldSpell)
        {
            if (PlayerControl.LocalPlayer == witch)
            {
                TouAudio.PlaySound(TouExtensionAudio.WitchLaugh);
            }
            QueueSpellNotification(target);
        }
    }

    private static void QueueSpellNotification(PlayerControl target)
    {
        if (target == null || target.Data == null)
        {
            return;
        }

        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
        {
            return;
        }


        if (!_pendingNotifications.Contains(target))
        {
            _pendingNotifications.Add(target);
        }
    }

    public static void SendBatchedNotifications()
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
        {
            return;
        }

        if (_pendingNotifications.Count == 0)
        {
            return;
        }


        PlayerControl? witch = null;
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc != null && pc.IsRole<WitchRole>())
            {
                witch = pc;
                break;
            }
        }

        if (witch == null || witch.Data == null)
        {
            _pendingNotifications.Clear();
            return;
        }

        var options = OptionGroupSingleton<WitchOptions>.Instance;
        var meetingsUntilDeath = options.MeetingsUntilDeath;
        var currentMeetingCount = Events.Impostor.WitchEvents.GetCurrentMeetingCount();


        var newlySpellboundPlayers = _pendingNotifications
            .Where(p => p != null && p.Data != null && p.HasModifier<WitchSpellboundModifier>())
            .Select(p =>
            {
                var mod = p?.GetModifier<WitchSpellboundModifier>();
                if (mod == null) return null;
                var meetingsSinceSpell = currentMeetingCount - mod.SpellCastMeeting;
                var meetingsRemaining = meetingsUntilDeath - meetingsSinceSpell;
                return new { Player = p, MeetingsRemaining = meetingsRemaining };
            })
            .Where(x => x != null)
            .ToList();

        if (newlySpellboundPlayers.Count == 0)
        {
            _pendingNotifications.Clear();
            return;
        }

        var witchColor = ColorUtility.ToHtmlStringRGBA(TouExtensionColors.Witch);
        var title = $"<color=#{witchColor}>{TouLocale.Get("ExtensionRoleWitch", "Witch")}</color>";

        string message;
        if (newlySpellboundPlayers.Count > 1)
        {

            var playerList = string.Join("\n", newlySpellboundPlayers.Select(sp =>
            {
                if (sp?.Player?.Data == null) return string.Empty;
                var remainingInt = Mathf.RoundToInt(sp.MeetingsRemaining);
                var remainingText = remainingInt <= 0
                    ? "They will die after this meeting"
                    : $"They have {remainingInt} meetings left";
                return $"  <color=#{witchColor}>{sp.Player.Data.PlayerName}</color>: {remainingText}";
            }).Where(s => !string.IsNullOrEmpty(s)));


            var baseMessage = TouLocale.GetParsed("ExtensionWitchSpellNotificationMultiple",
                $"Multiple players have been cursed:\n&lt;players&gt;\nVote out or kill the Witch to save them!");


            message = baseMessage
                .Replace("\\n", "\n")
                .Replace("&lt;players&gt;", playerList)
                .Replace("<players>", playerList);
        }
        else
        {

            var sp = newlySpellboundPlayers[0];
            if (sp?.Player?.Data == null) return;
            var remainingInt = Mathf.RoundToInt(sp.MeetingsRemaining);
            var baseMessage = TouLocale.GetParsed("ExtensionWitchSpellNotification",
                $"&lt;player&gt; has been cursed! They have &lt;meetings&gt; meeting(s) left. Vote out or kill the Witch to save them!");


            baseMessage = baseMessage.Replace("\\n", "\n");

            string finalMessage;
            if (remainingInt <= 0)
            {

                finalMessage = baseMessage
                    .Replace("&lt;player&gt;", $"<color=#{witchColor}>{sp.Player.Data.PlayerName}</color>")
                    .Replace("<player>", $"<color=#{witchColor}>{sp.Player.Data.PlayerName}</color>")
                    .Replace("They have <meetings> meeting(s) left", "They will die after this meeting")
                    .Replace("&lt;meetings&gt; meeting(s) left", "They will die after this meeting");
            }
            else
            {

                finalMessage = baseMessage
                    .Replace("&lt;player&gt;", $"<color=#{witchColor}>{sp.Player.Data.PlayerName}</color>")
                    .Replace("<player>", $"<color=#{witchColor}>{sp.Player.Data.PlayerName}</color>")
                    .Replace("&lt;meetings&gt; meeting(s) left", $"{remainingInt} meeting{(remainingInt == 1 ? "" : "s")} left")
                    .Replace("<meetings> meeting(s) left", $"{remainingInt} meeting{(remainingInt == 1 ? "" : "s")} left");
            }
            message = finalMessage;
        }


        RpcSendSpellNotification(witch, title, message);


        _pendingNotifications.Clear();
    }

    [MethodRpc((uint)ExtensionRpc.WitchSpellNotification)]
    public static void RpcSendSpellNotification(PlayerControl sender, string title, string message)
    {
        MiscUtils.AddFakeChat(PlayerControl.LocalPlayer.Data, title, message, false, true);
    }

    [MethodRpc((uint)ExtensionRpc.WitchClearAllSpellbound)]
    public static void RpcWitchClearAllSpellbound(PlayerControl sender)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || !player.HasModifier<WitchSpellboundModifier>())
            {
                continue;
            }

            player.RemoveModifier<WitchSpellboundModifier>();
        }
    }

    [MethodRpc((uint)ExtensionRpc.WitchClearSpellboundPlayer)]
    public static void RpcWitchClearSpellboundPlayer(PlayerControl sender, byte targetId)
    {
        var player = MiscUtils.PlayerById(targetId);
        if (player == null)
        {
            return;
        }

        if (player.HasModifier<WitchSpellboundModifier>())
        {
            player.RemoveModifier<WitchSpellboundModifier>();
        }
    }
}