using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using TouMiraRolesExtension.Modifiers;
using TouMiraRolesExtension.Networking;
using TouMiraRolesExtension.Options.Roles.Crewmate;
using TouMiraRolesExtension.Roles.Crewmate;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMiraRolesExtension.Modules;

public static class ForestallerSystem
{
    private static readonly HashSet<byte> ActiveForestallerIds = new();
    private static readonly Dictionary<byte, PlayerControl> ActiveForestallerPlayers = new();
    private static readonly HashSet<byte> PendingMeetingRevealIds = new();
    private static readonly HashSet<byte> RevealedIds = new();
    private static readonly List<byte> TmpRemoveIds = new();

    public static void ClearAll()
    {
        ActiveForestallerIds.Clear();
        ActiveForestallerPlayers.Clear();
        PendingMeetingRevealIds.Clear();
        RevealedIds.Clear();
    }

    public static void ClearForPlayer(byte playerId)
    {
        ActiveForestallerIds.Remove(playerId);
        ActiveForestallerPlayers.Remove(playerId);
        PendingMeetingRevealIds.Remove(playerId);
        RevealedIds.Remove(playerId);
    }

    public static bool IsForestallerActive(byte playerId)
    {
        return ActiveForestallerIds.Contains(playerId);
    }

    public static bool IsForestallerRevealed(byte playerId)
    {
        return RevealedIds.Contains(playerId);
    }

    public static bool AnyActiveForestallerAlive()
    {
        if (ActiveForestallerPlayers.Count == 0)
        {
            return false;
        }

        TmpRemoveIds.Clear();
        foreach (var kvp in ActiveForestallerPlayers)
        {
            var id = kvp.Key;
            var pc = kvp.Value;
            if (pc == null || pc.Data == null || pc.Data.Disconnected || pc.HasDied() || pc.Data.Role is not ForestallerRole)
            {
                TmpRemoveIds.Add(id);
                continue;
            }

            return true;
        }

        for (var i = 0; i < TmpRemoveIds.Count; i++)
        {
            ActiveForestallerIds.Remove(TmpRemoveIds[i]);
            ActiveForestallerPlayers.Remove(TmpRemoveIds[i]);
        }

        return false;
    }

    public static void TryActivateIfCompletedAllTasks(PlayerControl player)
    {
        if (player == null || player.Data == null || player.Data.Disconnected)
        {
            return;
        }

        if (player.Data.Role is not ForestallerRole)
        {
            return;
        }

        if (player.HasDied())
        {
            ClearForPlayer(player.PlayerId);
            return;
        }

        GetTaskCounts(player, out var completed, out var total);
        if (total <= 0 || completed != total)
        {
            return;
        }

        if (!ActiveForestallerIds.Add(player.PlayerId))
        {
            return;
        }

        ActiveForestallerPlayers[player.PlayerId] = player;

        var revealTiming = OptionGroupSingleton<ForestallerOptions>.Instance?.RevealTiming ?? ForestallerRevealTiming.NextMeeting;
        if (revealTiming == ForestallerRevealTiming.NextMeeting)
        {
            PendingMeetingRevealIds.Add(player.PlayerId);
        }
        else
        {
            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            {
                RpcForestallerReveal(player);
            }
        }

        var modComp = player.GetModifierComponent();
        if (modComp != null && !player.HasModifier<ForestallerMeetingRevealModifier>())
        {
            modComp.AddModifier(new ForestallerMeetingRevealModifier(player.Data.Role.Cast<RoleBehaviour>()));
        }
    }

    public static void OnForestallerDeath(PlayerControl player)
    {
        if (player == null)
        {
            return;
        }

        ClearForPlayer(player.PlayerId);
    }

    public static void OnMeetingStarted()
    {
        if (PendingMeetingRevealIds.Count == 0)
        {
            return;
        }

        var shouldAnnounce = PendingMeetingRevealIds.Any(id => IsForestallerActive(id) && !RevealedIds.Contains(id));
        foreach (var id in PendingMeetingRevealIds.ToArray())
        {
            PendingMeetingRevealIds.Remove(id);
            if (IsForestallerActive(id))
            {
                RevealedIds.Add(id);
            }
        }

        if (!shouldAnnounce)
        {
            return;
        }

        ShowSabotagesDisabledAnnouncement();
    }

    [MethodRpc((uint)ExtensionRpc.ForestallerReveal)]
    public static void RpcForestallerReveal(PlayerControl forestaller)
    {
        if (forestaller == null || forestaller.Data == null || forestaller.Data.Disconnected)
        {
            return;
        }

        if (forestaller.Data.Role is not ForestallerRole)
        {
            return;
        }

        if (forestaller.HasDied())
        {
            return;
        }

        RevealedIds.Add(forestaller.PlayerId);

        var modComp = forestaller.GetModifierComponent();
        if (modComp != null && !forestaller.HasModifier<ForestallerMeetingRevealModifier>())
        {
            modComp.AddModifier(new ForestallerMeetingRevealModifier(forestaller.Data.Role.Cast<RoleBehaviour>()));
        }

        ShowSabotagesDisabledAnnouncement();
    }

    private static void ShowSabotagesDisabledAnnouncement()
    {
        var msg = TownOfUs.Modules.Localization.TouLocale.GetParsed(
            "ExtensionForestallerSabotagesDisabledAnnouncement",
            "Forestaller has completed all tasks. Sabotages are now disabled.");

        var notif = Helpers.CreateAndShowNotification(
            $"<b>{Color.white.ToTextColor()}{msg}</color></b>",
            Color.white,
            new Vector3(0f, 1f, -20f),
            spr: TownOfUs.Assets.TouRoleIcons.Engineer.LoadAsset());
        notif.AdjustNotification();
    }

    private static void GetTaskCounts(PlayerControl player, out int completed, out int total)
    {
        completed = 0;
        total = 0;

        if (player == null || player.Data == null)
        {
            return;
        }

        if (player.myTasks != null && player.myTasks.Count > 0)
        {
            var tasks = player.myTasks.ToArray().Where(x =>
                x != null && !PlayerTask.TaskIsEmergency(x) && !x.TryCast<ImportantTextTask>());
            foreach (var t in tasks)
            {
                total++;
                var taskInfo = player.Data.FindTaskById(t.Id);
                var isComplete = taskInfo != null ? taskInfo.Complete : t.IsComplete;
                if (isComplete)
                {
                    completed++;
                }
            }

            return;
        }

        foreach (var info in player.Data.Tasks)
        {
            total++;
            if (info.Complete)
            {
                completed++;
            }
        }
    }
}