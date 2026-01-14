using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using Reactor.Utilities.Extensions;
using System.Text.RegularExpressions;
using TouMiraRolesExtension.Modifiers.Universal;
using TouMiraRolesExtension.Options.Modifiers;
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
    [HarmonyPostfix]
    public static void SetTaskTextPostfix(TaskPanelBehaviour __instance)
    {
        if (!LocalIsClueless())
        {
            return;
        }

        if (HudManager.Instance == null || HudManager.Instance.TaskPanel != __instance)
        {
            return;
        }

        if (__instance?.taskText == null)
        {
            return;
        }

        var original = __instance.taskText.text;
        if (string.IsNullOrEmpty(original))
        {
            return;
        }

        var lines = original.Split('\n');
        var filtered = new List<string>(lines.Length);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                filtered.Add(line);
                continue;
            }

            var trimmedLine = line.TrimStart();
            if (trimmedLine.StartsWith("<color=", StringComparison.Ordinal))
            {
                filtered.Add(line);
            }
            else
            {
                var censoredLine = CensorTaskLine(line);
                if (!string.IsNullOrEmpty(censoredLine))
                {
                    filtered.Add(censoredLine);
                }
            }
        }

        __instance.taskText.text = string.Join("\n", filtered);
    }

    private static string CensorTaskLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return line;
        }

        var openingColorTag = string.Empty;
        var closingColorTag = string.Empty;

        var openingMatch = Regex.Match(line, @"<color=#[0-9A-Fa-f]{8}>");
        if (openingMatch.Success)
        {
            openingColorTag = openingMatch.Value;
        }

        if (line.Contains("</color>"))
        {
            closingColorTag = "</color>";
        }

        var contentWithoutColors = Regex.Replace(line, @"<color=#[0-9A-Fa-f]{8}>|</color>", string.Empty);

        var leadingWhitespace = string.Empty;
        var trailingWhitespace = string.Empty;

        if (contentWithoutColors.Length > 0)
        {
            var trimmedStart = contentWithoutColors.TrimStart();
            leadingWhitespace = contentWithoutColors.Substring(0, contentWithoutColors.Length - trimmedStart.Length);

            var trimmedEnd = trimmedStart.TrimEnd();
            trailingWhitespace = trimmedStart.Substring(trimmedEnd.Length);

            contentWithoutColors = trimmedEnd;
        }

        var contentLength = contentWithoutColors.Length;
        string censoredContent;

        if (contentLength == 0)
        {
            censoredContent = string.Empty;
        }
        else
        {
            var censorType = OptionGroupSingleton<UniversalModifierOptions>.Instance.CluelessCensorType.Value;

            switch (censorType)
            {
                case CluelessCensorType.WhiteBars:
                    censoredContent = new string('█', contentLength);
                    break;
                case CluelessCensorType.Asterisks:
                    censoredContent = new string('*', contentLength);
                    break;
                case CluelessCensorType.QuestionMarks:
                    censoredContent = new string('?', contentLength);
                    break;
                case CluelessCensorType.Remove:
                    return string.Empty;
                default:
                    censoredContent = new string('?', contentLength);
                    break;
            }
        }

        var result = leadingWhitespace + openingColorTag + censoredContent + closingColorTag + trailingWhitespace;

        return result;
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

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.FixedUpdate))]
    [HarmonyPostfix]
    public static void MapBehaviourFixedUpdatePostfix(MapBehaviour __instance)
    {
        if (!LocalIsClueless() || __instance == null)
        {
            return;
        }

        if (__instance.taskOverlay != null && __instance.taskOverlay.isActiveAndEnabled)
        {
            __instance.taskOverlay.Hide();
        }
    }

}