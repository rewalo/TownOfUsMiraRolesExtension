using System.Text;
using HarmonyLib;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Utilities.Extensions;
using TouMiraRolesExtension.Modifiers;
using TouMiraRolesExtension.Roles.Neutral;
using TownOfUs;
using TownOfUs.Events;
using TownOfUs.Modules.Localization;
using TownOfUs.Options;
using TownOfUs.Patches;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMiraRolesExtension.Patches;

/// <summary>
/// Patch to add target indicator for lawyer's client (similar to executioner target indicator).
/// </summary>
[HarmonyPatch(typeof(PlayerRoleTextExtensions), nameof(PlayerRoleTextExtensions.UpdateTargetSymbols))]
public static class LawyerTargetIndicatorPatch
{
    [HarmonyPostfix]
    public static void UpdateTargetSymbolsPostfix(ref string __result, PlayerControl player, bool hidden = false)
    {
        if (PlayerControl.LocalPlayer == null)
        {
            return;
        }

        var genOpt = OptionGroupSingleton<GeneralOptions>.Instance;

        if (player.HasModifier<LawyerTargetModifier>(x => x.OwnerId == PlayerControl.LocalPlayer.PlayerId) &&
            PlayerControl.LocalPlayer.IsRole<LawyerRole>())
        {
            __result += $"<color=#{TownOfUsColors.Lawyer.ToHtmlStringRGBA()}> L</color>";
        }

        if (player.HasModifier<LawyerTargetModifier>() && PlayerControl.LocalPlayer.HasDied() &&
            genOpt != null && genOpt.TheDeadKnow && !hidden)
        {
            __result += $"<color=#{TownOfUsColors.Lawyer.ToHtmlStringRGBA()}> L</color>";
        }
    }
}

/// <summary>
/// Patch to show lawyer's name to the client in their role description.
/// </summary>
[HarmonyPatch(typeof(TownOfUsEventHandlers), nameof(TownOfUsEventHandlers.IntroRoleRevealEventHandler))]
public static class LawyerClientIntroPatch
{
    [HarmonyPostfix]
    public static void IntroRoleRevealEventHandlerPostfix(IntroRoleRevealEvent @event)
    {
        var instance = @event.IntroCutscene;
        var localPlayer = PlayerControl.LocalPlayer;

        if (localPlayer == null)
        {
            return;
        }

        var lawyerModifier = localPlayer.GetModifiers<LawyerTargetModifier>().FirstOrDefault();
        if (lawyerModifier == null)
        {
            return;
        }

        var lawyer = PlayerControl.AllPlayerControls.ToArray()
            .FirstOrDefault(p => p != null && p.PlayerId == lawyerModifier.OwnerId && p.IsRole<LawyerRole>());

        if (lawyer == null || lawyer.Data == null)
        {
            return;
        }

        var lawyerInfo = TouLocale.GetParsed("ExtensionRoleLawyerClientDescription")
            .Replace("<lawyer>", lawyer.Data.PlayerName);
        var color = TownOfUsColors.Lawyer.ToHtmlStringRGBA();

        instance.RoleBlurbText.text += $"\n<size=2.5><color=#{color}>{lawyerInfo}</color></size>";
    }
}

/// <summary>
/// Patch to show lawyer's name to the client in intro begin event as well.
/// </summary>
[HarmonyPatch(typeof(TownOfUsEventHandlers), nameof(TownOfUsEventHandlers.IntroBeginEventHandler))]
public static class LawyerClientIntroBeginPatch
{
    [HarmonyPostfix]
    public static void IntroBeginEventHandlerPostfix(IntroBeginEvent @event)
    {
        var cutscene = @event.IntroCutscene;
        var localPlayer = PlayerControl.LocalPlayer;

        if (localPlayer == null || cutscene == null)
        {
            return;
        }

        var lawyerModifier = localPlayer.GetModifiers<LawyerTargetModifier>().FirstOrDefault();
        if (lawyerModifier == null)
        {
            return;
        }

        var lawyer = PlayerControl.AllPlayerControls.ToArray()
            .FirstOrDefault(p => p != null && p.PlayerId == lawyerModifier.OwnerId && p.IsRole<LawyerRole>());

        if (lawyer == null || lawyer.Data == null)
        {
            return;
        }

        Reactor.Utilities.Coroutines.Start(AddLawyerInfoToIntro(cutscene, lawyer));
    }

    private static System.Collections.IEnumerator AddLawyerInfoToIntro(IntroCutscene cutscene, PlayerControl lawyer)
    {
        yield return new WaitForSeconds(0.02f);

        if (cutscene == null || lawyer?.Data == null)
        {
            yield break;
        }

        var lawyerInfo = TouLocale.GetParsed("ExtensionRoleLawyerClientDescription")
            .Replace("<lawyer>", lawyer.Data.PlayerName);
        var color = TownOfUsColors.Lawyer.ToHtmlStringRGBA();

        cutscene.RoleBlurbText.text += $"\n<size=2.5><color=#{color}>{lawyerInfo}</color></size>";
    }
}

/// <summary>
/// Patch to show lawyer in killer intro if they have a lawyer (handles both impostors and neutral killers).
/// </summary>
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
public static class LawyerKillerIntroPatch
{
    [HarmonyPrefix]
    public static bool BeginImpostorPrefix(IntroCutscene __instance)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || !localPlayer.IsImpostor())
        {
            return true;
        }

        var lawyerModifier = localPlayer.GetModifiers<LawyerTargetModifier>().FirstOrDefault();
        if (lawyerModifier == null)
        {
            return true;
        }

        var lawyer = PlayerControl.AllPlayerControls.ToArray()
            .FirstOrDefault(p => p != null && p.PlayerId == lawyerModifier.OwnerId && p.IsRole<LawyerRole>());

        if (lawyer == null || lawyer.Data == null)
        {
            return true;
        }

        return true;
    }

    [HarmonyPostfix]
    public static void BeginImpostorPostfix(IntroCutscene __instance)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || !localPlayer.IsImpostor())
        {
            return;
        }

        var lawyerModifier = localPlayer.GetModifiers<LawyerTargetModifier>().FirstOrDefault();
        if (lawyerModifier == null)
        {
            return;
        }

        var lawyer = PlayerControl.AllPlayerControls.ToArray()
            .FirstOrDefault(p => p != null && p.PlayerId == lawyerModifier.OwnerId && p.IsRole<LawyerRole>());

        if (lawyer == null || lawyer.Data == null)
        {
            return;
        }

        var impostorCount = Helpers.GetAlivePlayers().Count(x => x.IsImpostor());
        var lawyerIndex = impostorCount;
        var maxDepth = impostorCount + 1;

        var lawyerPlayer = __instance.CreatePlayer(lawyerIndex, maxDepth, lawyer.Data, true);

        if (lawyerPlayer != null)
        {
            lawyerPlayer.SetNameColor(TownOfUsColors.Lawyer);
        }

        var role = localPlayer.Data?.Role;
        if (role != null && Camera.main != null)
        {
            Camera.main.backgroundColor = role.TeamColor;
        }

        __instance.ImpostorText.gameObject.SetActive(true);
        IntroScenePatches.SetHiddenImpostors(__instance);
    }
}

/// <summary>
/// Patch to show lawyer in crewmate intro if a neutral killer has a lawyer.
/// </summary>
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
public static class LawyerNeutralKillerIntroPatch
{
    [HarmonyPrefix]
    public static void BeginCrewmatePrefix(ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay, IntroCutscene __instance)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.IsImpostor() || localPlayer.IsCrewmate())
        {
            return;
        }

        var role = localPlayer.Data?.Role;
        if (role == null)
        {
            return;
        }

        var alignment = MiscUtils.GetRoleAlignment(role);
        if (alignment != RoleAlignment.NeutralKilling)
        {
            return;
        }

        var lawyerModifier = localPlayer.GetModifiers<LawyerTargetModifier>().FirstOrDefault();
        if (lawyerModifier == null)
        {
            return;
        }

        var lawyer = PlayerControl.AllPlayerControls.ToArray()
            .FirstOrDefault(p => p != null && p.PlayerId == lawyerModifier.OwnerId && p.IsRole<LawyerRole>());

        if (lawyer == null || lawyer.Data == null)
        {
            return;
        }

        var team = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
        team.Add(localPlayer);
        team.Add(lawyer);
        teamToDisplay = team;
    }

    [HarmonyPostfix]
    public static void BeginCrewmatePostfix(IntroCutscene __instance)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.IsImpostor() || localPlayer.IsCrewmate())
        {
            return;
        }

        var role = localPlayer.Data?.Role;
        if (role == null)
        {
            return;
        }

        var alignment = MiscUtils.GetRoleAlignment(role);
        if (alignment != RoleAlignment.NeutralKilling)
        {
            return;
        }

        var lawyerModifier = localPlayer.GetModifiers<LawyerTargetModifier>().FirstOrDefault();
        if (lawyerModifier == null)
        {
            return;
        }

        __instance.TeamTitle.text = TouLocale.Get("NeutralKeyword").ToUpperInvariant();
        __instance.TeamTitle.color = new Color32(138, 138, 138, 255);

        __instance.ImpostorText.gameObject.SetActive(true);
        IntroScenePatches.SetHiddenImpostors(__instance);
    }
}

/// <summary>
/// Patch to set correct background color for defendant in intro based on their alignment.
/// </summary>
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
public static class LawyerIntroBackgroundColorPatch
{
    [HarmonyPostfix]
    public static void BeginCrewmatePostfix(IntroCutscene __instance)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || !localPlayer.HasModifier<LawyerTargetModifier>())
        {
            return;
        }

        var role = localPlayer.Data?.Role;
        if (role == null)
        {
            return;
        }

        var roleColor = role.TeamColor;
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = roleColor;
        }
    }
}

/// <summary>
/// Patch to add lawyer info to tab text for the defendant.
/// </summary>
[HarmonyPatch(typeof(TouRoleUtils), nameof(TouRoleUtils.SetTabText))]
public static class LawyerClientTabTextPatch
{
    [HarmonyPostfix]
    public static void SetTabTextPostfix(ref StringBuilder __result, ICustomRole role)
    {
        AddLawyerInfoToTabText(ref __result);
    }

    private static void AddLawyerInfoToTabText(ref StringBuilder __result)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || localPlayer.Data == null)
        {
            return;
        }

        if (!localPlayer.HasModifier<LawyerTargetModifier>())
        {
            return;
        }

        var lawyerModifier = localPlayer.GetModifiers<LawyerTargetModifier>().FirstOrDefault();
        if (lawyerModifier == null)
        {
            return;
        }

        var lawyer = PlayerControl.AllPlayerControls.ToArray()
            .FirstOrDefault(p => p != null && p.PlayerId == lawyerModifier.OwnerId && p.IsRole<LawyerRole>());

        if (lawyer == null || lawyer.Data == null)
        {
            return;
        }

        var lawyerInfo = TouLocale.GetParsed("ExtensionRoleLawyerClientTabDescription")
            .Replace("<lawyer>", lawyer.Data.PlayerName);
        var color = TownOfUsColors.Lawyer.ToHtmlStringRGBA();

        __result.AppendLine();
        __result.AppendLine(TownOfUsPlugin.Culture, $"<size=70%><color=#{color}>{lawyerInfo}</color></size>");
    }
}

/// <summary>
/// Patch to add lawyer info to dead tab text for the defendant.
/// </summary>
[HarmonyPatch(typeof(TouRoleUtils), nameof(TouRoleUtils.SetDeadTabText))]
public static class LawyerClientDeadTabTextPatch
{
    [HarmonyPostfix]
    public static void SetDeadTabTextPostfix(ref StringBuilder __result, ICustomRole role)
    {
        LawyerClientTabTextPatch.SetTabTextPostfix(ref __result, role);
    }
}