using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TouMiraRolesExtension.Utilities;

internal static class SafeReflection
{
    public static bool IsIl2CppOrUnityAssembly(Assembly assembly)
    {
        try
        {
            var name = assembly.GetName().Name ?? string.Empty;
            return name.StartsWith("Il2Cpp", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("UnityEngine", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("__Generated", StringComparison.Ordinal);
        }
        catch
        {
            return true;
        }
    }

    public static IEnumerable<Type> GetTypesSafe(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }
}