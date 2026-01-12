using HarmonyLib;
using TouMiraRolesExtension.Modules;

namespace TouMiraRolesExtension.Patches;

[HarmonyPatch]
public static class HackerJamMinigamePatches
{
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

        __instance.isSab = true;
        try
        {
            __instance.BackgroundColor?.SetColor(Palette.DisabledGrey);
            if (__instance.SabotageText != null)
            {
                __instance.SabotageText.gameObject.SetActive(true);
            }
        }
        catch
        {
            // ignore
        }

        try
        {
            if (__instance.CountAreas != null)
            {
                foreach (var area in __instance.CountAreas)
                {
                    area?.UpdateCount(0);
                }
            }
        }
        catch
        {
            // ignore
        }

        return false;
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

        __instance.isStatic = true;
        try
        {
            if (__instance.ViewPorts != null)
            {
                foreach (var vp in __instance.ViewPorts)
                {
                    if (vp != null)
                    {
                        vp.sharedMaterial = __instance.StaticMaterial;
                    }
                }
            }

            if (__instance.SabText != null)
            {
                foreach (var t in __instance.SabText)
                {
                    if (t != null)
                    {
                        t.gameObject.SetActive(true);
                    }
                }
            }
        }
        catch
        {
            // ignore
        }

        return false;
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

        try
        {
            __instance.ViewPort.sharedMaterial = __instance.StaticMaterial;
            __instance.SabText.gameObject.SetActive(true);
        }
        catch
        {
            // ignore
        }

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

    [HarmonyPatch(typeof(SecurityLogGame), nameof(SecurityLogGame.Update))]
    [HarmonyPrefix]
    public static bool SecurityLogGameUpdatePrefix(SecurityLogGame __instance)
    {
        if (__instance == null)
        {
            return true;
        }

        if (!HackerSystem.IsJammed)
        {
            return true;
        }

        try
        {
            __instance.SabText?.gameObject.SetActive(true);
        }
        catch
        {
            // ignore
        }

        return false;
    }

    [HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Update))]
    [HarmonyPrefix]
    public static bool VitalsMinigameUpdatePrefix(VitalsMinigame __instance)
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
}