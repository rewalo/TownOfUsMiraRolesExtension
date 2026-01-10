using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace TouMiraRolesExtension.Assets
{
    public static class TouExtensionAssets
    {
    public static LoadableAsset<Sprite> HexedSprite { get; } = new LoadableResourceAsset("TouMiraRolesExtension.Resources.Hexed.png");
    public static LoadableAsset<Sprite> ObjectionButtonSprite { get; } = new LoadableResourceAsset("TouMiraRolesExtension.Resources.Buttons.Object.png");
    public static LoadableAsset<Sprite> ObjectionAnimationSprite { get; } = new LoadableResourceAsset("TouMiraRolesExtension.Resources.Objection!.png");
    public static LoadableAsset<Sprite> LanternSprite { get; } = new LoadableResourceAsset("TouMiraRolesExtension.Resources.Lantern.png");
    public static LoadableAsset<Sprite> BrokenLanternSprite { get; } = new LoadableResourceAsset("TouMiraRolesExtension.Resources.BrokenLantern.png");
    }
}
