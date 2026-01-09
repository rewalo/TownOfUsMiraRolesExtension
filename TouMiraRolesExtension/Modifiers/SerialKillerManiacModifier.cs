using MiraAPI.Modifiers.Types;
using TMPro;
using TownOfUs.Utilities;
using TownOfUs.Assets;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using TownOfUs.Modules.Localization;
using MiraAPI.Networking;

namespace TouMiraRolesExtension.Modifiers;

/// <summary>
/// Maniac mode timer for Serial Killer - suicides if timer expires, resets on kill.
/// </summary>
public sealed class SerialKillerManiacModifier(float timerDuration, float cooldownDuration) : TimedModifier
{
    private Image? maniacBar;
    private TextMeshProUGUI? maniacText;
    private GameObject? maniacUI;
    private float soundTimer = 1f;
    private bool hasMadeFirstKill;
    
    public override string ModifierName => TouLocale.Get("ExtensionModifierSerialKillerManiac", "Maniac");
    public override float Duration => timerDuration;
    public override bool AutoStart => false;
    public override bool HideOnUi => true;
    public override bool RemoveOnComplete => false;

    public float CooldownDuration => cooldownDuration;

    public override string GetDescription()
    {
        var roundedTime = (int)Math.Round(Math.Max(TimeRemaining, 0), 0);

        var textColor = roundedTime switch
        {
            > 10 => Color.green,
            > 5 => Color.yellow,
            _ => Color.red
        };

        return $"{textColor.ToTextColor()}<size=80%>{roundedTime}s</size></color>";
    }

    public override void OnActivate()
    {
        base.OnActivate();

        if (!Player.AmOwner)
        {
            return;
        }

        maniacUI = Object.Instantiate(TouAssets.ScatterUI.LoadAsset(), HudManager.Instance.transform);
        maniacUI.transform.localPosition = new Vector3(-3.22f, 2.26f, -10f);
        maniacUI!.SetActive(false);

        maniacText = maniacUI.transform.FindChild("ScatterCanvas").FindChild("ScatterText").gameObject
            .GetComponent<TextMeshProUGUI>();
        maniacText.text = $"Maniac: {Duration}s";
        maniacText!.gameObject.SetActive(false);

        maniacBar = maniacUI.transform.FindChild("ScatterCanvas").FindChild("ScatterBar").gameObject
            .GetComponent<Image>();
        maniacBar.fillAmount = 1f;

        var maniacIcon = maniacUI.transform.FindChild("ScatterCanvas").FindChild("ScatterIcon").gameObject
            .GetComponent<Image>();
        maniacIcon.sprite = Player.Data.Role.RoleIconSolid;
    }

    public override void OnMeetingStart()
    {
        ResetTimer();
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!Player.AmOwner || Player.HasDied() || MeetingHud.Instance)
        {
            soundTimer = 1f;
            if (maniacUI != null)
            {
                maniacUI.SetActive(false);
            }
            if (maniacText != null)
            {
                maniacText.gameObject.SetActive(false);
            }
            return;
        }

        if (!TimerActive)
        {
            if (maniacUI != null)
            {
                maniacUI.SetActive(false);
            }
            if (maniacText != null)
            {
                maniacText.gameObject.SetActive(false);
            }
            return;
        }

        var roundedTime = (int)Math.Round(Math.Max(TimeRemaining, 0f), 0f);

        var textColor = roundedTime switch
        {
            > 10 => Color.green,
            > 5 => Color.yellow,
            _ => Color.red
        };

        if (maniacText != null)
        {
            maniacText.text = $"Maniac: {textColor.ToTextColor()}{roundedTime}s</color>";
        }

        if (maniacBar != null)
        {
            maniacBar.fillAmount = Math.Clamp(TimeRemaining / Duration, 0f, 1f);
            maniacBar.color = textColor;
        }

        if (roundedTime <= 11f)
        {
            soundTimer -= Time.fixedDeltaTime;

            if (soundTimer <= 0f)
            {
                var num = roundedTime / 10f;
                var pitch = 1.5f - num / 2f;
                SoundManager.Instance.PlaySoundImmediate(
                    GameManagerCreator.Instance.HideAndSeekManagerPrefab.FinalHideCountdownSFX, false, 1f, pitch,
                    SoundManager.Instance.SfxChannel);
                soundTimer = 1f;
            }
        }

        if (maniacUI != null)
        {
            maniacUI.SetActive(true);
        }
        if (maniacText != null)
        {
            maniacText.gameObject.SetActive(true);
        }
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();

        soundTimer = 1f;
        TimeRemaining = Duration;

        if (maniacUI != null)
        {
            maniacUI.SetActive(false);
        }
        if (maniacText != null)
        {
            maniacText.gameObject.SetActive(false);
        }
    }

    public override void OnTimerComplete()
    {
        if (Player.AmOwner && !Player.HasDied())
        {
            Player.RpcCustomMurder(Player);
        }
    }

    public void ResetOnKill()
    {
        if (!Player.AmOwner)
        {
            return;
        }

        hasMadeFirstKill = true;

        ResetTimer();
        ResumeTimer();
    }

    public void OnRoundStart()
    {
        ResetTimer();
        if (hasMadeFirstKill)
        {
            ResumeTimer();
        }
    }
}