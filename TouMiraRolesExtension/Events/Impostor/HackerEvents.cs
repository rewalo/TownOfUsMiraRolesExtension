using InnerNet;
using AmongUs.GameOptions;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.GameOptions;
using MiraAPI.Roles;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Options.Roles.Impostor;
using TouMiraRolesExtension.Roles.Impostor;
using TownOfUs.Utilities;

namespace TouMiraRolesExtension.Events.Impostor;

public static class HackerEvents
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

    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (!@event.TriggeredByIntro)
        {
            return;
        }

        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || PlayerControl.LocalPlayer == null)
        {
            return;
        }

        HackerSystem.ResetAll();
    }

    [RegisterEvent]
    public static void GameEndEventHandler(GameEndEvent @event)
    {
        HackerSystem.ResetAll();
    }

    [RegisterEvent]
    public static void StartMeetingEventHandler(StartMeetingEvent @event)
    {
        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer != null)
        {
            HackerRole.RpcHackerResetRound(PlayerControl.LocalPlayer);
        }
    }

    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        var killer = @event.Source;
        if (!IsHackerRole(killer))
        {
            return;
        }

        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || PlayerControl.LocalPlayer == null)
        {
            return;
        }

        var opts = OptionGroupSingleton<HackerOptions>.Instance;
        if (!opts.JamEnabled)
        {
            return;
        }

        var perKill = (int)opts.JamChargesPerKill;
        var max = (int)opts.JamMaxCharges;
        HackerSystem.AddJamCharge(killer.PlayerId, perKill, max);

        var newCharges = HackerSystem.GetJamCharges(killer.PlayerId);
        HackerRole.RpcHackerSetJamCharges(PlayerControl.LocalPlayer, killer.PlayerId, newCharges);
    }
}