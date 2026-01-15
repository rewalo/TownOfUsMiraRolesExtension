using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace TouMiraRolesExtension.Assets;

public static class TouExtensionImpAssets
{

    private const string ShortPath = "TouMiraRolesExtension.Resources.Buttons";
    private const string HackerPath = "TouMiraRolesExtension.Resources.Hacker";
    public static LoadableAsset<Sprite> SpellButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.SpellButton.png");
    public static LoadableAsset<Sprite> LanternButtonSprite { get; } = new LoadableResourceAsset($"{ShortPath}.LanternButton.png");


    public static LoadableAsset<Sprite> HackerDownloadButtonSprite { get; } = new LoadableResourceAsset($"{HackerPath}.hackerdownload.png");
    public static LoadableAsset<Sprite> HackerJamButtonSprite { get; } = new LoadableResourceAsset($"{HackerPath}.HackerJam.png");
    public static LoadableAsset<Sprite> HackerDeviceGenericSprite { get; } = new LoadableResourceAsset($"{HackerPath}.evilcamera.png");
    public static LoadableAsset<Sprite> HackerRole { get; } = new LoadableResourceAsset($"{HackerPath}.Hacker_Role.png");
    public static LoadableAsset<Sprite> HackerAdminSkeldSprite { get; } = new LoadableResourceAsset($"{HackerPath}.eviladminskeld.png");
    public static LoadableAsset<Sprite> HackerAdminMiraSprite { get; } = new LoadableResourceAsset($"{HackerPath}.eviladminmira.png");
    public static LoadableAsset<Sprite> HackerAdminPolusSprite { get; } = new LoadableResourceAsset($"{HackerPath}.eviladminpolus.png");
    public static LoadableAsset<Sprite> HackerAdminAirshipSprite { get; } = new LoadableResourceAsset($"{HackerPath}.eviladminairship.png");
    public static LoadableAsset<Sprite> HackerAdminSubmergedSprite { get; } = new LoadableResourceAsset($"{HackerPath}.eviladminsubmerged.png");
    public static LoadableAsset<Sprite> HackerCamerasSprite { get; } = new LoadableResourceAsset($"{HackerPath}.evilcamera.png");
    public static LoadableAsset<Sprite> HackerDoorLogSprite { get; } = new LoadableResourceAsset($"{HackerPath}.evildoorlog.png");
    public static LoadableAsset<Sprite> HackerVitalsSprite { get; } = new LoadableResourceAsset($"{HackerPath}.evilvitals.png");
}