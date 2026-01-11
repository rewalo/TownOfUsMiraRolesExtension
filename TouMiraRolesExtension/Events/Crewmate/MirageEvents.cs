using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using TouMiraRolesExtension.Buttons.Crewmate;
using TouMiraRolesExtension.Options.Roles.Crewmate;
using TouMiraRolesExtension.Roles.Crewmate;

namespace TouMiraRolesExtension.Events.Crewmate;

public static class MirageEvents
{
    private static int ActiveTaskCount;
    private static uint LastTaskId = uint.MaxValue;

    [RegisterEvent]
    public static void RoundStartEventHandler(RoundStartEvent @event)
    {
        if (!@event.TriggeredByIntro)
        {
            return;
        }

        ActiveTaskCount = 0;
        LastTaskId = uint.MaxValue;

        if (PlayerControl.LocalPlayer?.Data?.Role is not MirageRole)
        {
            return;
        }

        var btn = CustomButtonSingleton<MirageDecoyButton>.Instance;
        btn.SetUses((int)OptionGroupSingleton<MirageOptions>.Instance.InitialUses);
    }

    [RegisterEvent]
    public static void CompleteTaskEvent(CompleteTaskEvent @event)
    {
        if (@event.Player == null || !@event.Player.AmOwner)
        {
            return;
        }

        if (@event.Player.Data?.Role is not MirageRole)
        {
            return;
        }

        var opt = OptionGroupSingleton<MirageOptions>.Instance;
        if (opt.UsesPerTasks.Value <= 0)
        {
            return;
        }

        if (@event.Task != null && @event.Task.Id != LastTaskId)
        {
            ++ActiveTaskCount;
            LastTaskId = @event.Task.Id;
        }

        var btn = CustomButtonSingleton<MirageDecoyButton>.Instance;
        if (btn.LimitedUses && opt.UsesPerTasks.Value <= ActiveTaskCount)
        {
            ++btn.UsesLeft;
            btn.SetUses(btn.UsesLeft);
            ActiveTaskCount = 0;
        }
    }
}