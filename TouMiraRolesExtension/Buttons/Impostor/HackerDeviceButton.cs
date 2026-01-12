using AmongUs.GameOptions;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMiraRolesExtension.Assets;
using TouMiraRolesExtension.Modules;
using TouMiraRolesExtension.Options.Roles.Impostor;
using TouMiraRolesExtension.Roles.Impostor;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMiraRolesExtension.Buttons.Impostor;

public sealed class HackerDeviceButton : TownOfUsRoleButton<HackerRole>
{
    public static bool IsPortableDeviceOpen { get; private set; }

    private Minigame? _minigame;
    private VitalsMinigame? _vitals;
    private bool _usingAdminMap;

    public override string Name => TouLocale.GetParsed("ExtensionRoleHackerDevice", "Device");
    public override BaseKeybind Keybind => OptionGroupSingleton<HackerOptions>.Instance.SimpleModeJamOnly
        ? Keybinds.SecondaryAction
        : Keybinds.TertiaryAction; // U when not in simple mode
    public override Color TextOutlineColor => TouExtensionColors.Hacker;
    public override float Cooldown => 0.001f;
    public override LoadableAsset<Sprite> Sprite => TouExtensionImpAssets.HackerDeviceGenericSprite;

    public override bool ZeroIsInfinite { get; set; } = true;

    public override bool Enabled(RoleBehaviour? role)
    {
        return base.Enabled(role) && !OptionGroupSingleton<HackerOptions>.Instance.SimpleModeJamOnly;
    }

    public override bool CanUse()
    {
        if (!base.CanUse())
        {
            return false;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null || player.HasDied())
        {
            return false;
        }

        if (player.AreCommsAffected())
        {
            return false;
        }

        var locked = HackerSystem.GetLockedSource(player.PlayerId);
        if (locked == HackerInfoSource.None)
        {
            return false;
        }

        return HackerSystem.GetBatterySeconds(player.PlayerId) > 0f;
    }

    protected override void FixedUpdate(PlayerControl playerControl)
    {
        base.FixedUpdate(playerControl);

        var player = PlayerControl.LocalPlayer;
        if (Button == null || player == null || MeetingHud.Instance)
        {
            return;
        }

        UpdateDeviceSprite();

        // Keep charges/battery visible.
        var battery = HackerSystem.GetBatterySeconds(player.PlayerId);
        Button.usesRemainingText.gameObject.SetActive(true);
        Button.usesRemainingSprite.gameObject.SetActive(true);
        Button.usesRemainingText.text = $"{Mathf.CeilToInt(battery)}s";

        // If comms are down (real comms or Hacker Jam), mimic sabotage behavior by force-closing.
        if (player.AreCommsAffected())
        {
            CloseAll();
            return;
        }

        // Close detection (player manually closed).
        if (_usingAdminMap && MapBehaviour.Instance != null && !MapBehaviour.Instance.gameObject.activeSelf)
        {
            _usingAdminMap = false;
            IsPortableDeviceOpen = false;
        }
        if (_minigame != null && Minigame.Instance == null)
        {
            _minigame = null;
            IsPortableDeviceOpen = false;
        }
        if (_vitals != null && Minigame.Instance == null)
        {
            _vitals = null;
            IsPortableDeviceOpen = false;
        }

        var isOpen = _usingAdminMap || _minigame != null || _vitals != null;
        if (!isOpen)
        {
            return;
        }

        battery -= Time.deltaTime;
        if (battery <= 0f)
        {
            CloseAll();
            HackerSystem.SetBatterySeconds(player.PlayerId, 0f);
            return;
        }

        HackerSystem.SetBatterySeconds(player.PlayerId, battery);
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            return;
        }

        // Toggle off.
        if (_usingAdminMap || _minigame != null || _vitals != null)
        {
            CloseAll();
            return;
        }

        if (Minigame.Instance != null)
        {
            return;
        }

        var opts = OptionGroupSingleton<HackerOptions>.Instance;
        if (!opts.MoveWithDevice)
        {
            // Match other "MoveWithMenu" toggles: if disabled, hard stop the player before opening.
            player.NetTransform.Halt();
        }

        var locked = HackerSystem.GetLockedSource(player.PlayerId);
        switch (locked)
        {
            case HackerInfoSource.Admin:
                OpenPortableAdmin(opts.MoveWithDevice);
                _usingAdminMap = true;
                IsPortableDeviceOpen = true;
                break;
            case HackerInfoSource.Cameras:
                OpenConsoleMinigame(HackerSystem.FindCameraConsole());
                IsPortableDeviceOpen = _minigame != null;
                break;
            case HackerInfoSource.DoorLog:
                OpenConsoleMinigame(HackerSystem.FindDoorLogConsole());
                IsPortableDeviceOpen = _minigame != null;
                break;
            case HackerInfoSource.Vitals:
                OpenPortableVitals();
                IsPortableDeviceOpen = _vitals != null;
                break;
        }
    }

    private void OpenConsoleMinigame(SystemConsole? console)
    {
        if (console == null || console.MinigamePrefab == null)
        {
            return;
        }

        _minigame = Object.Instantiate(console.MinigamePrefab, Camera.main.transform, false);
        _minigame.transform.SetParent(Camera.main.transform, false);
        _minigame.transform.localPosition = new Vector3(0f, 0f, -50f);
        _minigame.Begin(null);
    }

    private void OpenPortableVitals()
    {
        var sci = RoleManager.Instance.GetRole(RoleTypes.Scientist).TryCast<ScientistRole>();
        if (sci == null || sci.VitalsPrefab == null)
        {
            return;
        }

        _vitals = Object.Instantiate<VitalsMinigame>(sci.VitalsPrefab);
        _vitals.transform.SetParent(Camera.main.transform, false);
        _vitals.transform.localPosition = new Vector3(0f, 0f, -50f);
        _vitals.BatteryText.gameObject.SetActive(false);
        _vitals.Begin(null);
    }

    private static void OpenPortableAdmin(bool canMove)
    {
        if (MapBehaviour.Instance && MapBehaviour.Instance.gameObject.activeSelf)
        {
            MapBehaviour.Instance.Close();
            return;
        }

        if (!ShipStatus.Instance)
        {
            return;
        }

        HudManager.Instance.InitMap();
        if (!PlayerControl.LocalPlayer.CanMove && !MeetingHud.Instance)
        {
            return;
        }

        var mapOpts = GameManager.Instance.GetMapOptions();
        var portableAdmin = MapBehaviour.Instance;
        portableAdmin.GenericShow();
        portableAdmin.countOverlay.gameObject.SetActive(true);
        portableAdmin.countOverlay.SetOptions(mapOpts.ShowLivePlayerPosition, mapOpts.IncludeDeadBodies);
        portableAdmin.countOverlayAllowsMovement = canMove;
        portableAdmin.taskOverlay.Hide();
        portableAdmin.HerePoint.enabled = !mapOpts.ShowLivePlayerPosition;
        portableAdmin.TrackedHerePoint.gameObject.SetActive(false);
        if (portableAdmin.HerePoint.enabled)
        {
            PlayerControl.LocalPlayer.SetPlayerMaterialColors(portableAdmin.HerePoint);
        }
    }

    private void CloseAll()
    {
        try
        {
            if (_usingAdminMap && MapBehaviour.Instance != null)
            {
                MapBehaviour.Instance.Close();
            }
        }
        catch
        {
            // ignore
        }

        try
        {
            _minigame?.Close();
        }
        catch
        {
            // ignore
        }

        try
        {
            _vitals?.Close();
        }
        catch
        {
            // ignore
        }

        _usingAdminMap = false;
        _minigame = null;
        _vitals = null;
        IsPortableDeviceOpen = false;
    }

    private void UpdateDeviceSprite()
    {
        if (Button?.graphic == null || PlayerControl.LocalPlayer == null)
        {
            return;
        }

        var locked = HackerSystem.GetLockedSource(PlayerControl.LocalPlayer.PlayerId);
        var spr = locked switch
        {
            HackerInfoSource.Admin => GetAdminSpriteForCurrentMap(),
            HackerInfoSource.Vitals => TouExtensionImpAssets.HackerVitalsSprite.LoadAsset(),
            HackerInfoSource.DoorLog => TouExtensionImpAssets.HackerDoorLogSprite.LoadAsset(),
            HackerInfoSource.Cameras => TouExtensionImpAssets.HackerCamerasSprite.LoadAsset(),
            _ => TouExtensionImpAssets.HackerDeviceGenericSprite.LoadAsset()
        };

        if (spr != null && Button.graphic.sprite != spr)
        {
            Button.graphic.sprite = spr;
        }
    }

    private static Sprite GetAdminSpriteForCurrentMap()
    {
        var mapId = (ExpandedMapNames)GameOptionsManager.Instance.currentNormalGameOptions.MapId;
        if (TutorialManager.InstanceExists)
        {
            mapId = (ExpandedMapNames)AmongUsClient.Instance.TutorialMapId;
        }

        return mapId switch
        {
            ExpandedMapNames.Skeld or ExpandedMapNames.Dleks => TouExtensionImpAssets.HackerAdminSkeldSprite.LoadAsset(),
            ExpandedMapNames.MiraHq => TouExtensionImpAssets.HackerAdminMiraSprite.LoadAsset(),
            ExpandedMapNames.Polus => TouExtensionImpAssets.HackerAdminPolusSprite.LoadAsset(),
            ExpandedMapNames.Airship => TouExtensionImpAssets.HackerAdminAirshipSprite.LoadAsset(),
            ExpandedMapNames.Submerged => TouExtensionImpAssets.HackerAdminSubmergedSprite.LoadAsset(),
            _ => TouExtensionImpAssets.HackerAdminSkeldSprite.LoadAsset()
        };
    }
}