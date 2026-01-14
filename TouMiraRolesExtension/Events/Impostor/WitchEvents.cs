using BepInEx.Logging;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using Reactor.Utilities;
using System.Collections;
using TouMiraRolesExtension.Modifiers;
using TouMiraRolesExtension.Options.Roles.Impostor;
using TouMiraRolesExtension.Roles.Impostor;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Networking;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMiraRolesExtension.Events.Impostor;

public static class WitchEvents
{
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("WitchEvents");
    private static readonly List<PlayerControl> PendingSpellDeaths = new();
    private static int _meetingCount;
    private static bool _processingDeaths;

    public static int GetCurrentMeetingCount() => _meetingCount;

    [RegisterEvent]
    public static void StartMeetingEventHandler(StartMeetingEvent @event)
    {
        _meetingCount++;
        Logger.LogWarning($"[Witch] StartMeetingEventHandler: Meeting count incremented to {_meetingCount}");


        WitchRole.SendBatchedNotifications();

        if (MeetingHud.Instance == null)
        {
            return;
        }

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || !player.HasModifier<WitchSpellboundModifier>())
            {
                continue;
            }

            var voteArea = MeetingHud.Instance.playerStates.FirstOrDefault(x => x.TargetPlayerId == player.PlayerId);
            if (voteArea != null)
            {
                voteArea.NameText.color = TouExtensionColors.Witch;
            }
        }

        Coroutines.Start(CoMonitorMeetingEnd());
    }

    private static IEnumerator CoMonitorMeetingEnd()
    {
        while (MeetingHud.Instance != null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        Logger.LogWarning($"[Witch] CoMonitorMeetingEnd: Meeting ended, processing spell deaths");

        if (!_processingDeaths)
        {
            _processingDeaths = true;
            Coroutines.Start(CoProcessSpellDeaths());
        }
        else
        {
            Logger.LogWarning($"[Witch] CoMonitorMeetingEnd: Deaths already being processed via EjectionEventHandler, skipping");
        }
    }

    [RegisterEvent]
    public static void EjectionEventHandler(EjectionEvent @event)
    {
        var exiled = @event.ExileController?.initData?.networkedPlayer?.Object;
        if (exiled == null)
        {
            return;
        }




        Logger.LogWarning($"[Witch] EjectionEventHandler: Starting spell death processing. Meeting count: {_meetingCount}");
        _processingDeaths = true;
        Coroutines.Start(CoProcessSpellDeaths());
    }

    private static IEnumerator CoProcessSpellDeaths()
    {
        try
        {
            Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Starting coroutine. Meeting count: {_meetingCount}");

            while (MeetingHud.Instance != null)
            {
                yield return new WaitForSeconds(0.05f);
            }

            yield return new WaitForSeconds(0.1f);


            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                yield break;
            }

            Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Meeting ended, checking witch status");

            var witchAlive = false;

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || !player.IsRole<WitchRole>())
                {
                    continue;
                }

                if (!player.HasDied())
                {
                    witchAlive = true;
                }
            }

            Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Witch alive: {witchAlive}");

            if (!witchAlive)
            {
                Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Witch is dead, clearing all spellbound modifiers");
                WitchRole.RpcWitchClearAllSpellbound(PlayerControl.LocalPlayer);
                PendingSpellDeaths.Clear();
                yield break;
            }

            var options = OptionGroupSingleton<WitchOptions>.Instance;
            var meetingsUntilDeath = options.MeetingsUntilDeath;
            Logger.LogWarning(
                $"[Witch] CoProcessSpellDeaths: Meetings until death: {meetingsUntilDeath}, Current meeting count: {_meetingCount}");

            var spellboundPlayers = new List<PlayerControl>();
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && pc.HasModifier<WitchSpellboundModifier>())
                {
                    spellboundPlayers.Add(pc);
                }
            }

            Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Found {spellboundPlayers.Count} spellbound players");

            foreach (var player in spellboundPlayers)
            {
                if (player == null || player.HasDied())
                {
                    Logger.LogWarning(
                        $"[Witch] CoProcessSpellDeaths: Skipping {player?.Data?.PlayerName ?? "null"} - already dead or null");
                    continue;
                }

                var modifier = player.GetModifier<WitchSpellboundModifier>();
                if (modifier == null)
                {
                    Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Skipping {player.Data.PlayerName} - no modifier found");
                    continue;
                }

                var meetingsSinceSpell = _meetingCount - modifier.SpellCastMeeting;
                var meetingsSinceSpellFloat = (float)meetingsSinceSpell;
                Logger.LogWarning(
                    $"[Witch] CoProcessSpellDeaths: Player {player.Data.PlayerName} - SpellCastMeeting: {modifier.SpellCastMeeting}, CurrentMeetingCount: {_meetingCount}, MeetingsSinceSpell: {meetingsSinceSpellFloat}, MeetingsUntilDeath: {meetingsUntilDeath}");

                if (meetingsSinceSpellFloat >= meetingsUntilDeath)
                {
                    Logger.LogWarning(
                        $"[Witch] CoProcessSpellDeaths: Player {player.Data.PlayerName} should die! ({meetingsSinceSpellFloat} >= {meetingsUntilDeath})");
                }
                else
                {
                    Logger.LogWarning(
                        $"[Witch] CoProcessSpellDeaths: Player {player.Data.PlayerName} not dying yet ({meetingsSinceSpellFloat} < {meetingsUntilDeath})");
                    continue;
                }

                var shouldDie = true;

                if (player.HasModifier<GuardianAngelProtectModifier>())
                {
                    Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Player {player.Data.PlayerName} is protected by shield");
                    shouldDie = false;
                }

                if (shouldDie)
                {
                    PlayerControl? witch = null;
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc != null && pc.IsRole<WitchRole>())
                        {
                            witch = pc;
                            break;
                        }
                    }

                    Logger.LogWarning(
                        $"[Witch] CoProcessSpellDeaths: Attempting to kill {player.Data.PlayerName}, witch found: {witch != null}");

                    if (witch != null)
                    {
                        Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Calling RpcSpecialMurder on {player.Data.PlayerName}");
                        witch.RpcSpecialMurder(
                            player,
                            isIndirect: true,
                            ignoreShield: false,
                            didSucceed: true,
                            resetKillTimer: true,
                            createDeadBody: false,
                            teleportMurderer: false,
                            showKillAnim: true,
                            playKillSound: false,
                            causeOfDeath: "Witch");
                    }
                    else
                    {
                        Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Witch not found, using fallback RpcMurderPlayer");
                        player.RpcMurderPlayer(player, true);
                    }

                    WitchRole.RpcWitchClearSpellboundPlayer(PlayerControl.LocalPlayer, player.PlayerId);
                    Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Cleared modifier from {player.Data.PlayerName}");
                }
                else
                {
                    Logger.LogWarning($"[Witch] CoProcessSpellDeaths: Player {player.Data.PlayerName} survived due to shield");
                    WitchRole.RpcWitchClearSpellboundPlayer(PlayerControl.LocalPlayer, player.PlayerId);
                }
            }

            PendingSpellDeaths.Clear();
        }
        finally
        {
            _processingDeaths = false;
        }
    }

    [RegisterEvent]
    public static void PlayerDeathEventHandler(PlayerDeathEvent @event)
    {
        var victim = @event.Player;
        if (victim == null)
        {
            return;
        }


        if (victim.IsRole<WitchRole>())
        {

            if (MeetingHud.Instance != null)
            {
                if (!_processingDeaths)
                {
                    _processingDeaths = true;
                    Coroutines.Start(CoProcessSpellDeaths());
                }

                return;
            }

            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            {
                WitchRole.RpcWitchClearAllSpellbound(PlayerControl.LocalPlayer);
                PendingSpellDeaths.Clear();
            }
        }
    }

    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {

        if (@event.TriggeredByIntro)
        {
            _meetingCount = 0;
        }
    }
}