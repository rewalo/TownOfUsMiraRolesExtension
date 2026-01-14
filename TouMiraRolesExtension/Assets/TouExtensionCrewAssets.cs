using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace TouMiraRolesExtension.Assets;

public static class TouExtensionCrewAssets
{

    private const string ShortPath = "TouMiraRolesExtension.Resources.Buttons";

    public static LoadableAsset<Sprite> DecoyButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.Decoy_Button.png");
}