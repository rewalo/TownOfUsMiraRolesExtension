using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using System.Reflection;
using TMPro;
using TouMiraRolesExtension.Modifiers.Universal;
using TownOfUs.Events;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modules.Localization;
using TownOfUs.Options;
using TownOfUs.Utilities;

namespace TouMiraRolesExtension.Patches;

/// <summary>
/// Patches to ensure Clueless modifier shows the intro blurb instead of just "Modifier: Clueless".
/// </summary>
[HarmonyPatch]
public static class CluelessIntroInfoPatch
{
#pragma warning disable CS8601 
#pragma warning disable S3011 
    private static readonly FieldInfo ModifierTextField = typeof(TownOfUsEventHandlers).GetField("ModifierText", BindingFlags.NonPublic | BindingFlags.Static);
#pragma warning restore S3011 
#pragma warning restore CS8601 

    [HarmonyPatch(typeof(TownOfUsEventHandlers), nameof(TownOfUsEventHandlers.RunModChecks))]
    [HarmonyPostfix]
    public static void RunModChecksPostfix()
    {
        var option = OptionGroupSingleton<GeneralOptions>.Instance.ModifierReveal;
        var uniModifier = PlayerControl.LocalPlayer.GetModifiers<UniversalGameModifier>().FirstOrDefault();

        if (uniModifier is CluelessModifier && option is ModReveal.Universal)
        {
            var modifierText = ModifierTextField?.GetValue(null) as TextMeshPro;
            if (modifierText != null)
            {
                var introBlurb = TouLocale.GetParsed("ExtensionModifierCluelessIntroBlurb");
                modifierText.text = $"<size={uniModifier.IntroSize}>{introBlurb}</size>";
                modifierText.color = MiscUtils.GetModifierColour(uniModifier);
            }
        }
    }
}

#pragma warning disable SA1313