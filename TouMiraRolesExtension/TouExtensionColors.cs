using TownOfUs;
using UnityEngine;

namespace TouMiraRolesExtension;

public static class TouExtensionColors
{
    // Trapper role color (vent traps)
    public static Color Trapper => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(150, 100, 200, 255);
}