using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace TouMiraRolesExtension.Assets;

public static class TouExtensionCrewAssets
{
    // THIS FILE SHOULD ONLY HOLD BUTTONS AND ROLE BANNERS, EVERYTHING ELSE BELONGS IN TouExtensionAssets.cs
    private const string ShortPath = "TouMiraRolesExtension.Resources.Buttons";

    public static LoadableAsset<Sprite> DecoyButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.Decoy_Button.png");
}