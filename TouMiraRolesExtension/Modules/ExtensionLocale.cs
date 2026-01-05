using System.Reflection;
using BepInEx.Logging;
using MiraAPI.Utilities;
using TownOfUs.Modules.Localization;

namespace TouMiraRolesExtension.Modules;

public static class ExtensionLocale
{
    internal static ManualLogSource LocaleLogger { get; } = BepInEx.Logging.Logger.CreateLogSource("ExtensionLocale");

    public static void SearchInternalLocale()
    {
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var locale in TouLocale.LangList)
        {
            using var resourceStream =
                assembly.GetManifestResourceStream("TouMiraRolesExtension.Resources.Locale." + locale.Value);
            if (resourceStream == null)
            {
                LocaleLogger.LogError($"Extension Language is not added: {locale.Key.ToDisplayString()}");
                continue;
            }

            LocaleLogger.LogWarning($"Extension Language is being added: {locale.Key.ToDisplayString()}");
            using StreamReader reader = new(resourceStream);
            string xmlContent = reader.ReadToEnd();

            TouLocale.TouLocalization.TryAdd((SupportedLangs)locale.Key, []);
            TouLocale.ParseXmlFile(xmlContent, (SupportedLangs)locale.Key);
        }
    }
}