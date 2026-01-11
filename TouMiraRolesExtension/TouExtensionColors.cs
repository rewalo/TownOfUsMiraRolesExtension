using TownOfUs;
using UnityEngine;

namespace TouMiraRolesExtension;

public static class TouExtensionColors
{
    public static Color Trapper => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(166, 209, 179, 255);
    public static Color SerialKiller => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(58, 102, 192, 255);
    public static Color Witch => TownOfUsColors.UseBasic ? Palette.ImpostorRed : new Color32(213, 63, 66, 255);
    public static Color Forestaller => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(241, 196, 15, 255);
    public static Color Wraith => Palette.ImpostorRed;
    public static Color Mirage => TownOfUsColors.UseBasic ? Palette.CrewmateBlue : new Color32(222, 168, 94, 255);
}