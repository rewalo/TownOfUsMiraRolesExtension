using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace TouMiraRolesExtension.Assets;

public static class TouExtensionImpAssets
{
    // THIS FILE SHOULD ONLY HOLD BUTTONS AND ROLE BANNERS, EVERYTHING ELSE BELONGS IN TouExtensionAssets.cs
    private const string ShortPath = "TouMiraRolesExtension.Resources.Buttons";
    public static LoadableAsset<Sprite> SpellButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.SpellButton.png");
}