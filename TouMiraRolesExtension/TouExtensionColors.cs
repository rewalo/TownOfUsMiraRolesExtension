using TownOfUs;
using UnityEngine;

namespace TouMiraRolesExtension;

public static class TouExtensionColors
{
    public static Color Trapper => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(150, 100, 200, 255);
    public static Color SerialKiller => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(58, 102, 192, 255);
    public static Color Witch => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(213, 63, 66, 255);
}