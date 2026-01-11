using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Modifiers;
using TouMiraRolesExtension.Modifiers;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Roles.Neutral;
using TownOfUs.Events;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;

namespace TouMiraRolesExtension.Events.Neutral;

public static class SerialKillerEvents
{
    [RegisterEvent]
    public static void RoundStartHandler(RoundStartEvent @event)
    {
        ModifierUtils.GetActiveModifiers<SerialKillerManiacModifier>().Do(x => x.OnRoundStart());
    }

    [RegisterEvent]
    public static void PlayerDeathEventHandler(PlayerDeathEvent @event)
    {
        var victim = @event.Player;
        if (victim == null)
        {
            return;
        }

        VentOccupancySystem.ClearForPlayer(victim.PlayerId);
    }

    [RegisterEvent]
    public static void BeforeMurderEventHandler(BeforeMurderEvent @event)
    {
        var killer = @event.Source;
        var victim = @event.Target;

        if (killer == null || victim == null || !killer.IsRole<SerialKillerRole>())
        {
            return;
        }

        if (SerialKillerVentKillSystem.TryGetVentKillTarget(killer.PlayerId, out var ventTarget) && ventTarget != null && ventTarget.PlayerId == victim.PlayerId)
        {
            if (killer.AmOwner && killer.inVent)
            {
                if (killer.inVent && Vent.currentVent != null)
                {
                    killer.MyPhysics.RpcExitVent(Vent.currentVent.Id);
                    killer.MyPhysics?.ExitAllVents();
                }

                killer.inVent = false;
                Vent.currentVent = null;
            }

            if (victim.AmOwner && victim.inVent)
            {

                if (victim.HasDied())
                {
                    return;
                }

                if (victim.inVent && Vent.currentVent != null)
                {
                    victim.MyPhysics.RpcExitVent(Vent.currentVent.Id);
                    victim.MyPhysics?.ExitAllVents();
                }

                victim.inVent = false;
                Vent.currentVent = null;
            }

            VentOccupancySystem.ClearForPlayer(killer.PlayerId);
            VentOccupancySystem.ClearForPlayer(victim.PlayerId);

            if (!killer.HasModifier<SerialKillerNoVentModifier>())
            {
                killer.AddModifier<SerialKillerNoVentModifier>();
            }
        }

        if (killer.AmOwner)
        {
            DeathHandlerModifier.UpdateDeathHandlerImmediate(
                victim,
                TouLocale.Get("DiedToSerialKiller"),
                DeathEventHandlers.CurrentRound,
                (!MeetingHud.Instance && !ExileController.Instance)
                    ? DeathHandlerOverride.SetTrue
                    : DeathHandlerOverride.SetFalse,
                TouLocale.GetParsed("DiedByStringBasic")
                    .Replace("<player>", killer.Data.PlayerName),
                lockInfo: DeathHandlerOverride.SetTrue
            );
        }
    }

    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        var killer = @event.Source;
        var victim = @event.Target;

        if (killer == null || victim == null || !killer.IsRole<SerialKillerRole>())
        {
            return;
        }

        var serialKiller = killer.GetRole<SerialKillerRole>();
        if (serialKiller == null)
        {
            return;
        }

        if (MeetingHud.Instance || ExileController.Instance)
        {
            return;
        }

        if (SerialKillerVentKillSystem.TryGetVentKillTarget(killer.PlayerId, out var ventTarget) && ventTarget != null && ventTarget.PlayerId == victim.PlayerId)
        {
            SerialKillerVentKillSystem.ClearForPlayer(killer.PlayerId);
        }

        if (killer.TryGetModifier<SerialKillerManiacModifier>(out var maniacMod))
        {
            maniacMod.ResetOnKill();
        }
    }
}