using HarmonyLib;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Roles.Neutral;
using TouMiraRolesExtension.Options.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;
using TouMiraRolesExtension.Modifiers;
using MiraAPI.Modifiers;
using MiraAPI.GameOptions;
using TownOfUs.Roles;


namespace TouMiraRolesExtension.Patches;

[HarmonyPatch]
public static class SerialKillerVentPatches
{
    [HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
    [HarmonyPostfix]
    public static void EnterVentPostfix(Vent __instance, PlayerControl pc)
    {
        if (pc == null || __instance == null || MeetingHud.Instance)
        {
            return;
        }

        if (pc.AmOwner)
        {
            VentOccupancySystem.SetOccupant(__instance.Id, pc.PlayerId);
        }

        if (pc.AmOwner && pc.IsRole<SerialKillerRole>())
        {
            CheckVentKillOpportunity(__instance, pc);
        }

        if (pc.AmOwner)
        {
            CheckForSerialKillerInVent(__instance, pc);
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.RpcExitVent))]
    [HarmonyPostfix]
    public static void ExitVentPostfix(PlayerPhysics __instance, int ventId)
    {
        var player = __instance?.myPlayer;
        if (player == null || MeetingHud.Instance)
        {
            return;
        }

        if (player.AmOwner)
        {
            VentOccupancySystem.SetOccupant(ventId, 0);
        }
        
        if (player.AmOwner && player.IsRole<SerialKillerRole>())
        {
            SerialKillerVentKillSystem.ClearForPlayer(player.PlayerId);
        }

        if (player.AmOwner)
        {
            CheckSerialKillerVentTargets(ventId);
        }
    }

    private static void CheckForSerialKillerInVent(Vent vent, PlayerControl enteringPlayer)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.PlayerId == enteringPlayer.PlayerId || 
            !localPlayer.IsRole<SerialKillerRole>() || localPlayer.HasModifier<SerialKillerNoVentModifier>() || 
            !localPlayer.inVent || localPlayer.HasDied())
        {
            return;
        }

        int? serialKillerVentId = GetPlayerVentId(localPlayer);
        if (serialKillerVentId.HasValue && serialKillerVentId.Value == vent.Id)
        {
            var options = OptionGroupSingleton<SerialKillerOptions>.Instance;
            if (IsValidVentKillTarget(enteringPlayer, options.VentKillTargets))
            {
                SerialKillerVentKillSystem.SetVentKillTarget(localPlayer.PlayerId, enteringPlayer);
            }
        }
    }

    private static void CheckSerialKillerVentTargets(int ventId)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || !localPlayer.IsRole<SerialKillerRole>() || 
            localPlayer.HasModifier<SerialKillerNoVentModifier>() || !localPlayer.inVent || localPlayer.HasDied())
        {
            return;
        }

        int? serialKillerVentId = GetPlayerVentId(localPlayer);
        if (serialKillerVentId.HasValue && serialKillerVentId.Value == ventId)
        {
            Vent? vent = null;
            foreach (var v in ShipStatus.Instance.AllVents)
            {
                if (v != null && v.Id == ventId)
                {
                    vent = v;
                    break;
                }
            }
            if (vent != null)
            {
                CheckVentKillOpportunity(vent, localPlayer);
            }
        }
    }

    private static void CheckVentKillOpportunity(Vent vent, PlayerControl serialKiller)
    {
        if (serialKiller.HasModifier<SerialKillerNoVentModifier>())
        {
            SerialKillerVentKillSystem.ClearForPlayer(serialKiller.PlayerId);
            return;
        }

        var options = OptionGroupSingleton<SerialKillerOptions>.Instance;
        
        PlayerControl? target = null;
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.PlayerId == serialKiller.PlayerId || player.HasDied() || !player.inVent)
            {
                continue;
            }

            int? playerVentId = GetPlayerVentId(player);
            if (playerVentId.HasValue && playerVentId.Value == vent.Id && IsValidVentKillTarget(player, options.VentKillTargets))
            {
                target = player;
                break;
            }
        }

        if (target != null)
        {
            SerialKillerVentKillSystem.SetVentKillTarget(serialKiller.PlayerId, target);
        }
        else
        {
            SerialKillerVentKillSystem.ClearForPlayer(serialKiller.PlayerId);
        }
    }

    private static int? GetPlayerVentId(PlayerControl player)
    {
        if (player.AmOwner && Vent.currentVent != null)
        {
            return Vent.currentVent.Id;
        }

        if (!player.inVent)
        {
            return null;
        }

        foreach (var vent in ShipStatus.Instance.AllVents)
        {
            if (vent == null)
            {
                continue;
            }
            
            if (VentOccupancySystem.TryGetOccupant(vent.Id, out var occupantId) && occupantId == player.PlayerId)
            {
                return vent.Id;
            }
        }

        var playerPos = player.transform.position;
        foreach (var vent in ShipStatus.Instance.AllVents)
        {
            if (vent == null)
            {
                continue;
            }

            var ventPos = vent.transform.position;
            if (Vector2.Distance(new Vector2(playerPos.x, playerPos.y), new Vector2(ventPos.x, ventPos.y)) < 0.5f)
            {
                return vent.Id;
            }
        }

        return null;
    }

    private static bool IsValidVentKillTarget(PlayerControl target, VentKillTargets ventKillTargets)
    {
        return ventKillTargets switch
        {
            VentKillTargets.Impostors => target.IsImpostor(),
            VentKillTargets.ImpNK => target.IsImpostor() || target.Is(RoleAlignment.NeutralKilling),
            VentKillTargets.ImpNeutrals => target.IsImpostor() || target.IsNeutral(),
            VentKillTargets.Any => true,
            _ => false
        };
    }
}