using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Modifiers;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Roles.Neutral;
using TouMiraRolesExtension.Modifiers;
using TownOfUs.Utilities;
using TownOfUs.Events;
using TownOfUs.Modifiers;
using HarmonyLib;
using TownOfUs.Modules.Localization;

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

        // Clear vent occupancy when player dies
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

        if (killer.AmOwner)
        {
            DeathHandlerModifier.UpdateDeathHandlerImmediate(victim,
                TouLocale.Get("DiedToSerialKiller"),
                DeathEventHandlers.CurrentRound,
                (!MeetingHud.Instance && !ExileController.Instance)
                    ? DeathHandlerOverride.SetTrue
                    : DeathHandlerOverride.SetFalse,
                TouLocale.GetParsed("DiedByStringBasic").Replace("<player>", killer.Data.PlayerName),
                lockInfo: DeathHandlerOverride.SetTrue);
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

        if (killer.inVent && SerialKillerVentKillSystem.TryGetVentKillTarget(killer.PlayerId, out var ventTarget))
        {
            if (ventTarget != null && ventTarget.PlayerId == victim.PlayerId)
            {
                if (!killer.HasModifier<SerialKillerNoVentModifier>())
                {
                    killer.AddModifier<SerialKillerNoVentModifier>();
                }

                SerialKillerVentKillSystem.ClearForPlayer(killer.PlayerId);
            }
        }

        if (killer.TryGetModifier<SerialKillerManiacModifier>(out var maniacMod))
        {
            maniacMod.ResetOnKill();
        }
    }
}