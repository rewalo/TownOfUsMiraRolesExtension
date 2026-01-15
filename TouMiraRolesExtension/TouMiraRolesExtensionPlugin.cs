using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using MiraAPI;
using MiraAPI.PluginLoading;
using Reactor;
using Reactor.Networking;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using System.Globalization;
using System.Reflection;
using TouMiraRolesExtension.Patches;
using TouMiraRolesExtension.Patches.WinConditions;
using TouMiraRolesExtension.Utilities;
using TownOfUs;
using TownOfUs.Patches;

namespace TouMiraRolesExtension;

[BepInAutoPlugin("toumiragames.tou.extension", "Tou Mira Roles Extension")]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
[BepInDependency(MiraApiPlugin.Id)]
[BepInDependency(TownOfUsPlugin.Id)]
[ReactorModFlags(ModFlags.RequireOnAllClients)]
public partial class TouMiraRolesExtensionPlugin : BasePlugin, IMiraPlugin
{
    /// <summary>
    ///     Gets the specified Culture for string manipulations.
    /// </summary>
    public static CultureInfo Culture => TownOfUsPlugin.Culture;

    /// <inheritdoc />
    public string OptionsTitleText => "TOU Extension";

    /// <summary>
    ///     Determines if the current build is a dev build or not. This will change certain visuals as well as always grab news locally to be up to date.
    /// </summary>
    public static bool IsDevBuild => true;

    /// <inheritdoc />
    public ConfigFile GetConfigFile()
    {
        return Config;
    }

    public Harmony Harmony { get; } = new(Id);

    public override void Load()
    {
        ReactorCredits.Register("Tou Mira Roles Extension", Version, IsDevBuild, ReactorCredits.AlwaysShow);
        IL2CPPChainloader.Instance.Finished += Modules.ExtensionLocale.SearchInternalLocale;
        IL2CPPChainloader.Instance.Finished += LawyerTeamChatRegistration.Register;
        PatchAllWithErrorHandling();

        WinConditionRegistry.Register(new LawyerDuoWinCondition());
        WinConditionRegistry.Register(new LawyerParityWinCondition());
    }

    private void PatchAllWithErrorHandling()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var patchTypes = SafeReflection.GetTypesSafe(assembly)
            .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch), true).Length > 0)
            .ToList();

        int successCount = 0;
        int failCount = 0;
        List<string> failedTypes = new();

        foreach (var type in patchTypes)
        {
            try
            {
                Harmony.PatchAll(type);
                successCount++;
            }
            catch (System.Exception ex)
            {
                failCount++;
                failedTypes.Add(type.FullName ?? type.Name);
                Error($"Failed to patch class: {type.FullName}");
                Error($"Error type: {ex.GetType().FullName}");
                Error($"Error message: {ex.Message}");

                if (ex.InnerException != null)
                {
                    Error($"Inner exception: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
                }

                Debug($"Stack trace: {ex.StackTrace}");
            }
        }

        Info($"Harmony patching completed: {successCount} classes patched successfully, {failCount} classes had errors");

        if (failCount > 0)
        {
            Warning($"Failed to patch the following classes: {string.Join(", ", failedTypes)}");
            Warning("The mod may function partially. If you experience issues, please report which patch classes failed.");
            Warning("This error may be due to:");
            Warning("  - Environment-specific issues (corrupted .NET runtime, incompatible game version)");
            Warning("  - Missing dependencies or incompatible mod versions");
            Warning("  - Methods that cannot be patched due to JIT compilation issues");
        }

        if (successCount == 0 && failCount > 0)
        {
            Error("All Harmony patches failed! The mod cannot function without patches.");
            Error("Please check your .NET runtime installation and game version compatibility.");
            throw new System.InvalidOperationException($"Failed to apply any Harmony patches. {failCount} patch classes failed. See log for details.");
        }
    }
}