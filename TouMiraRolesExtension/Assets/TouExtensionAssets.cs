using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace TouMiraRolesExtension.Assets
{
    public static class TouExtensionAssets
    {
        public static LoadableAsset<Sprite> HexedSprite { get; } = new LoadableResourceAsset("TouMiraRolesExtension.Resources.Hexed.png");
    }
}
