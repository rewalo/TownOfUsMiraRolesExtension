using System.Reflection;
using HarmonyLib;
using MiraAPI.Modifiers;
using Reactor.Utilities.Extensions;
using TouMiraRolesExtension.Modifiers.Universal;
using UnityEngine;

namespace TownOfUs.Patches.Misc;

/// <summary>
/// Clueless modifier: hides task list, task arrows/markers, and map task overlay for the local player.
/// </summary>
[HarmonyPatch]
public static class CluelessTaskGuidancePatches
{
    internal static bool LocalIsClueless()
    {
        return PlayerControl.LocalPlayer != null &&
               PlayerControl.LocalPlayer.HasModifier<CluelessModifier>();
    }

    [HarmonyPatch(typeof(TaskPanelBehaviour), nameof(TaskPanelBehaviour.SetTaskText))]
    [HarmonyPrefix]
    public static bool TaskPanelSetTaskTextPrefix(TaskPanelBehaviour __instance)
    {
        if (!LocalIsClueless())
        {
            return true;
        }

        if (HudManager.Instance != null && HudManager.Instance.TaskPanel == __instance)
        {
            if (__instance.taskText != null)
            {
                __instance.taskText.text = string.Empty;
            }
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(NormalPlayerTask), nameof(NormalPlayerTask.UpdateArrowAndLocation))]
    [HarmonyPrefix]
    public static bool NormalPlayerTaskUpdateArrowAndLocationPrefix(NormalPlayerTask __instance)
    {
        if (!LocalIsClueless() || __instance == null)
        {
            return true;
        }

        if (__instance.Owner != PlayerControl.LocalPlayer)
        {
            return true;
        }

        TryDestroyExistingTaskArrow(__instance);
        return false;
    }

    private static void TryDestroyExistingTaskArrow(NormalPlayerTask task)
    {
        try
        {
            var t = task.GetType();
            var field =
                AccessTools.Field(t, "Arrow") ??
                AccessTools.Field(t, "arrow") ??
                AccessTools.Field(t, "taskArrow") ??
                AccessTools.Field(t, "_arrow");

            if (field == null)
            {
                return;
            }

            var arrowObj = field.GetValue(task) as MonoBehaviour;
            if (arrowObj == null)
            {
                return;
            }

            if (arrowObj.gameObject != null && arrowObj.gameObject.activeSelf)
            {
                arrowObj.gameObject.Destroy();
            }

            field.SetValue(task, null);
        }
        catch
        {
            // ignored
        }
    }

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowNormalMap))]
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap))]
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowCountOverlay))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void MapBehaviourShowPostfix(MapBehaviour __instance)
    {
        if (!LocalIsClueless() || __instance == null)
        {
            return;
        }

        __instance.taskOverlay?.Hide();
    }

    [HarmonyPatch]
    public static class TaskOverlayShowPatch
    {
        private static System.Type? _taskOverlayType;

        private static System.Type? GetTaskOverlayType()
        {
            if (_taskOverlayType != null)
            {
                return _taskOverlayType;
            }

            _taskOverlayType = typeof(MapBehaviour).Assembly.GetType("TaskOverlay") ??
                               typeof(MapBehaviour).Assembly.GetType("AmongUs.GameOptions.TaskOverlay") ??
                               typeof(MapBehaviour).Assembly.GetTypes()
                                   .FirstOrDefault(t => t.Name == "TaskOverlay" && t.GetMethod("Show") != null);

            return _taskOverlayType;
        }

        [HarmonyPatch]
        [HarmonyPrefix]
        public static bool TaskOverlayShowPrefix()
        {
            if (LocalIsClueless())
            {
                return false;
            }

            return true;
        }

        private static IEnumerable<MethodBase> TargetMethods()
        {
            var taskOverlayType = GetTaskOverlayType();
            if (taskOverlayType == null)
            {
                return Enumerable.Empty<MethodBase>();
            }

            var showMethod = AccessTools.Method(taskOverlayType, "Show");
            if (showMethod != null)
            {
                return new[] { showMethod };
            }

            return Enumerable.Empty<MethodBase>();
        }
    }
}