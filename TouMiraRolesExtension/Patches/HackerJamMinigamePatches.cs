using System;
using HarmonyLib;
using System.Reflection;
using TouMiraRolesExtension.Modules;
using UnityEngine;
using UnityEngine.UI;

namespace TouMiraRolesExtension.Patches;

[HarmonyPatch]
public static class HackerJamMinigamePatches
{
    private static void ApplyAdminJammed(MapCountOverlay overlay)
    {
        if (overlay == null)
        {
            return;
        }

        overlay.isSab = true;
        try
        {
            overlay.BackgroundColor?.SetColor(Palette.DisabledGrey);
            overlay.SabotageText?.gameObject.SetActive(true);
        }
        catch
        {
            // ignore
        }

        try
        {
            if (overlay.CountAreas != null)
            {
                foreach (var area in overlay.CountAreas)
                {
                    area?.UpdateCount(0);
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    private static void ApplyCamsJammed(SurveillanceMinigame cams)
    {
        if (cams == null)
        {
            return;
        }

        cams.isStatic = true;
        try
        {
            if (cams.ViewPorts != null)
            {
                foreach (var vp in cams.ViewPorts)
                {
                    if (vp != null)
                    {
                        vp.sharedMaterial = cams.StaticMaterial;
                    }
                }
            }

            if (cams.SabText != null)
            {
                foreach (var t in cams.SabText)
                {
                    t?.gameObject.SetActive(true);
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    private static void ApplyPlanetCamsJammed(PlanetSurveillanceMinigame cams)
    {
        if (cams == null)
        {
            return;
        }

        try
        {
            cams.ViewPort.sharedMaterial = cams.StaticMaterial;
            cams.SabText?.gameObject.SetActive(true);
        }
        catch
        {
            // ignore
        }
    }

    private static void TrySetActive(object? obj, bool active)
    {
        if (obj == null)
        {
            return;
        }

        try
        {
            switch (obj)
            {
                case GameObject go:
                    go.SetActive(active);
                    return;
                case Component comp:
                    comp.gameObject.SetActive(active);
                    return;
                case System.Collections.IEnumerable enumerable:
                {
                    foreach (var item in enumerable)
                    {
                        TrySetActive(item, active);
                    }

                    return;
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    private static void TryEnableVitalsCommsDisabledUi(VitalsMinigame vitals)
    {
        if (vitals == null)
        {
            return;
        }

        var type = vitals.GetType();
        var candidateFields = new[]
        {
            "SabText", "SabotageText", "NoCommsText", "CommsDownText", "CommsText",
            "CommsDisabledText", "DisabledText", "sabotageText", "sabText"
        };

        foreach (var fName in candidateFields)
        {
            try
            {
                var field = AccessTools.Field(type, fName);
                if (field == null)
                {
                    continue;
                }

                var value = field.GetValue(vitals);
                TrySetActive(value, true);
            }
            catch
            {
                // ignore
            }
        }

        var candidateContainers = new[]
        {
            "SabotagePanel", "NoCommsPanel", "CommsDownPanel", "CommsDisabledPanel",
            "sabotagePanel", "noCommsPanel"
        };

        foreach (var fName in candidateContainers)
        {
            try
            {
                var field = AccessTools.Field(type, fName);
                if (field == null)
                {
                    continue;
                }

                var value = field.GetValue(vitals);
                TrySetActive(value, true);
            }
            catch
            {
                // ignore
            }
        }
    }

    private static void TryDisableVitalsCommsDisabledUi(VitalsMinigame vitals)
    {
        if (vitals == null)
        {
            return;
        }

        var type = vitals.GetType();
        var candidateFields = new[]
        {
            "SabText", "SabotageText", "NoCommsText", "CommsDownText", "CommsText",
            "CommsDisabledText", "DisabledText", "sabotageText", "sabText"
        };

        foreach (var fName in candidateFields)
        {
            try
            {
                var field = AccessTools.Field(type, fName);
                if (field == null)
                {
                    continue;
                }

                var value = field.GetValue(vitals);
                TrySetActive(value, false);
            }
            catch
            {
                // ignore
            }
        }

        var candidateContainers = new[]
        {
            "SabotagePanel", "NoCommsPanel", "CommsDownPanel", "CommsDisabledPanel",
            "sabotagePanel", "noCommsPanel"
        };

        foreach (var fName in candidateContainers)
        {
            try
            {
                var field = AccessTools.Field(type, fName);
                if (field == null)
                {
                    continue;
                }

                var value = field.GetValue(vitals);
                TrySetActive(value, false);
            }
            catch
            {
                // ignore
            }
        }
    }

    [HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.OnEnable))]
    [HarmonyPrefix]
    public static void MapCountOverlayOnEnablePrefix(MapCountOverlay __instance)
    {
        if (__instance == null)
        {
            return;
        }

        try
        {
            __instance.SetOptions(false, true);
        }
        catch
        {
            // ignore
        }

        if (HackerSystem.IsJammed)
        {
            ApplyAdminJammed(__instance);
        }
    }

    [HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.Update))]
    [HarmonyPrefix]
    public static bool MapCountOverlayUpdatePrefix(MapCountOverlay __instance)
    {
        if (__instance == null)
        {
            return true;
        }

        if (!HackerSystem.IsJammed)
        {
            return true;
        }

        ApplyAdminJammed(__instance);

        return false;
    }

    [HarmonyPatch(typeof(SurveillanceMinigame), nameof(SurveillanceMinigame.Begin))]
    [HarmonyPostfix]
    public static void SurveillanceMinigameBeginPostfix(SurveillanceMinigame __instance)
    {
        if (__instance != null && HackerSystem.IsJammed)
        {
            ApplyCamsJammed(__instance);
        }
    }


    [HarmonyPatch(typeof(SurveillanceMinigame), nameof(SurveillanceMinigame.Update))]
    [HarmonyPrefix]
    public static bool SurveillanceMinigameUpdatePrefix(SurveillanceMinigame __instance)
    {
        if (__instance == null)
        {
            return true;
        }

        if (!HackerSystem.IsJammed)
        {
            return true;
        }

        ApplyCamsJammed(__instance);

        return false;
    }

    [HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.Begin))]
    [HarmonyPostfix]
    public static void PlanetSurveillanceMinigameBeginPostfix(PlanetSurveillanceMinigame __instance)
    {
        if (__instance != null && HackerSystem.IsJammed)
        {
            ApplyPlanetCamsJammed(__instance);
        }
    }

    [HarmonyPatch]
    private static class PlanetSurveillanceMinigameNextCameraPatch
    {
        private static MethodBase? TargetMethod()
        {
            return AccessTools.Method(typeof(PlanetSurveillanceMinigame), "NextCamera");
        }

        private static bool Prefix(PlanetSurveillanceMinigame __instance, [HarmonyArgument(0)] int direction)
        {
            if (__instance == null)
            {
                return true;
            }

            if (HackerSystem.IsJammed)
            {
                ApplyPlanetCamsJammed(__instance);

                try
                {
                    if (direction != 0 && Constants.ShouldPlaySfx())
                    {
                        SoundManager.Instance.PlaySound(__instance.ChangeSound, false, 1f);
                    }

                    __instance.Dots[__instance.currentCamera].sprite = __instance.DotDisabled;

                    var len = __instance.survCameras != null ? __instance.survCameras.Length : 0;
                    if (len > 0)
                    {
                        var next = (__instance.currentCamera + direction) % len;
                        if (next < 0)
                        {
                            next += len;
                        }

                        __instance.currentCamera = next;
                    }

                    __instance.Dots[__instance.currentCamera].sprite = __instance.DotEnabled;

                    var survCamera = __instance.survCameras[__instance.currentCamera];
                    __instance.Camera.transform.position = survCamera.transform.position + survCamera.Offset;
                    __instance.LocationName.text = survCamera.CamName;
                }
                catch
                {
                    // ignore
                }

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.Update))]
    [HarmonyPrefix]
    public static bool PlanetSurveillanceMinigameUpdatePrefix(PlanetSurveillanceMinigame __instance)
    {
        if (__instance == null)
        {
            return true;
        }

        if (!HackerSystem.IsJammed)
        {
            return true;
        }

        ApplyPlanetCamsJammed(__instance);

        return false;
    }

    [HarmonyPatch(typeof(FungleSurveillanceMinigame), nameof(FungleSurveillanceMinigame.Update))]
    [HarmonyPrefix]
    public static bool FungleSurveillanceMinigameUpdatePrefix(FungleSurveillanceMinigame __instance)
    {
        if (__instance == null)
        {
            return true;
        }

        if (!HackerSystem.IsJammed)
        {
            return true;
        }

        __instance.Close();
        return false;
    }

    private static void ApplyDoorLogJammed(SecurityLogGame doorLog)
    {
        if (doorLog == null)
        {
            return;
        }

        try
        {
            if (doorLog.SabText != null)
            {
                doorLog.SabText.gameObject.SetActive(true);
            }
        }
        catch
        {
            // ignore
        }

        try
        {
            var allChildren = doorLog.transform.GetComponentsInChildren<Transform>(true);
            if (allChildren != null)
            {
                Transform? sabTextTransform = null;
                try
                {
                    if (doorLog.SabText != null)
                    {
                        sabTextTransform = doorLog.SabText.transform;
                    }
                }
                catch
                {
                    // ignore
                }

                Transform? logContainer = null;
                foreach (var child in allChildren)
                {
                    if (child == null || child == doorLog.transform)
                    {
                        continue;
                    }

                    var scrollRect = child.GetComponent<ScrollRect>();
                    if (scrollRect != null && scrollRect.content != null)
                    {
                        logContainer = scrollRect.content;
                        break;
                    }

                    var name = child.name.ToLowerInvariant();
                    if (name.Contains("content") || name.Contains("viewport") || 
                        (name.Contains("scroll") && name.Contains("view")))
                    {
                        logContainer = child;
                        break;
                    }
                }

                if (logContainer != null)
                {
                    foreach (Transform entry in logContainer)
                    {
                        if (entry != null && entry != logContainer && entry != sabTextTransform)
                        {
                            entry.gameObject.SetActive(false);
                        }
                    }
                }

                foreach (var child in allChildren)
                {
                    if (child == null || child == doorLog.transform || child == sabTextTransform)
                    {
                        continue;
                    }

                    var name = child.name.ToLowerInvariant();

                    if (name.Contains("log") && (name.Contains("entry") || name.Contains("item") || name.Contains("row")))
                    {
                        child.gameObject.SetActive(false);
                        continue;
                    }
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    [HarmonyPatch(typeof(SecurityLogGame), nameof(SecurityLogGame.Update))]
    [HarmonyPrefix]
    public static bool SecurityLogGameUpdatePrefix(SecurityLogGame __instance)
    {
        if (__instance == null)
        {
            return true;
        }

        if (HackerSystem.IsJammed)
        {
            ApplyDoorLogJammed(__instance);
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Update))]
    [HarmonyPrefix]
    public static bool VitalsMinigameUpdatePrefix(VitalsMinigame __instance)
    {
        if (__instance == null)
        {
            return true;
        }

        if (HackerSystem.IsJammed)
        {
            try
            {
                if (__instance.vitals != null)
                {
                    foreach (var panel in __instance.vitals)
                    {
                        panel?.gameObject.SetActive(false);
                    }
                }
            }
            catch
            {
                // ignore
            }

            try
            {
                if (__instance.SabText != null)
                {
                    __instance.SabText.gameObject.SetActive(true);
                }
                else
                {
                    TryEnableVitalsCommsDisabledUi(__instance);
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }

        try
        {
            var localPlayer = PlayerControl.LocalPlayer;
            if (localPlayer != null && !PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(localPlayer))
            {
                try
                {
                    if (__instance.vitals != null)
                    {
                        foreach (var panel in __instance.vitals)
                        {
                            panel?.gameObject.SetActive(true);
                        }
                    }
                }
                catch
                {
                    // ignore
                }

                try
                {
                    __instance.SabText?.gameObject.SetActive(false);
                }
                catch
                {
                    // ignore
                }

                TryDisableVitalsCommsDisabledUi(__instance);
            }
        }
        catch
        {
            // ignore
        }

        return true;
    }
}