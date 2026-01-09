using TownOfUs;
using UnityEngine;

namespace TouMiraRolesExtension;

public static class TouExtensionColors
{
    public static Color Trapper => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(150, 100, 200, 255);
    public static Color SerialKiller => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(139, 0, 0, 255); // Dark red
}