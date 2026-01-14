using System.Collections.Generic;
using HarmonyLib;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using TouMiraRolesExtension.Modifiers;
using TouMiraRolesExtension.Options.Roles.Impostor;
using TouMiraRolesExtension.Roles.Impostor;
using TouMiraRolesExtension.Assets;
using TownOfUs.Patches;
using TownOfUs.Utilities;
using TMPro;
using UnityEngine;

namespace TouMiraRolesExtension.Patches;

/// <summary>
/// Patch to add spellbound indicator sprite for players who have been spelled by the Witch.
/// Shows HexedSprite only to the Witch during gameplay, and to everyone in meetings or after first meeting if they have meetings left.
/// </summary>
[HarmonyPatch(typeof(HudManagerPatches), nameof(HudManagerPatches.UpdateRoleNameText))]
public static class WitchSpellboundIndicatorPatch
{
    private static readonly Dictionary<byte, SpriteRenderer> HexedSprites = new();

    [HarmonyPostfix]
    public static void UpdateRoleNameTextPostfix()
    {
        if (PlayerControl.LocalPlayer == null)
        {
            return;
        }


        if (MeetingHud.Instance != null)
        {
            foreach (var playerVA in MeetingHud.Instance.playerStates)
            {
                var player = MiscUtils.PlayerById(playerVA.TargetPlayerId);
                if (player == null || player.Data == null)
                {
                    continue;
                }

                var shouldShow = ShouldShowHexedSprite(player);

                if (shouldShow)
                {
                    EnsureHexedSpriteForMeeting(player, playerVA);
                }
                else
                {

                    HideHexedSprite(player);
                    var meetingKey = (byte)(player.PlayerId + 200);
                    if (HexedSprites.TryGetValue(meetingKey, out var meetingSprite) && meetingSprite != null)
                    {
                        meetingSprite.gameObject.SetActive(false);
                    }
                }
            }
        }
        else
        {

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null || player.cosmetics?.nameText == null)
                {
                    continue;
                }

                var shouldShow = ShouldShowHexedSprite(player);

                if (shouldShow)
                {
                    EnsureHexedSprite(player);
                }
                else
                {
                    HideHexedSprite(player);
                }
            }
        }
    }

    private static bool ShouldShowHexedSprite(PlayerControl player)
    {
        if (player == null || !player.HasModifier<WitchSpellboundModifier>())
        {
            return false;
        }

        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null)
        {
            return false;
        }


        if (MeetingHud.Instance != null)
        {
            return true;
        }

        var modifier = player.GetModifier<WitchSpellboundModifier>();
        if (modifier != null)
        {
            var options = OptionGroupSingleton<WitchOptions>.Instance;
            var meetingsUntilDeath = options.MeetingsUntilDeath;
            var currentMeetingCount = Events.Impostor.WitchEvents.GetCurrentMeetingCount();
            var meetingsSinceSpell = currentMeetingCount - modifier.SpellCastMeeting;
            var meetingsRemaining = meetingsUntilDeath - meetingsSinceSpell;


            if (meetingsSinceSpell >= 1 && meetingsRemaining > 0)
            {
                return true;
            }
        }


        if (localPlayer.IsRole<WitchRole>())
        {
            return true;
        }

        return false;
    }

    private static bool IsPlayerNameVisible(PlayerControl player, TextMeshPro nameText)
    {
        if (player == null || nameText == null || PlayerControl.LocalPlayer == null)
        {
            return false;
        }


        if (!nameText.gameObject.activeInHierarchy || !nameText.enabled || nameText.color.a <= 0.01f)
        {
            return false;
        }


        var localPos = PlayerControl.LocalPlayer.GetTruePosition();
        var playerPos = player.GetTruePosition();
        var distance = Vector2.Distance(localPos, playerPos);


        if (distance > PlayerControl.LocalPlayer.lightSource.viewDistance * 1.5f)
        {
            return false;
        }



        var direction = (playerPos - localPos).normalized;
        var checkDistance = Mathf.Min(distance, PlayerControl.LocalPlayer.lightSource.viewDistance);


        var centerHit = PhysicsHelpers.AnyNonTriggersBetween(localPos, direction, checkDistance, Constants.ShipAndObjectsMask);
        if (!centerHit)
        {
            return true;
        }


        var perp = new Vector2(x: -direction.y, y: direction.x);
        var offset1 = localPos + perp * 0.3f;
        var offset2 = localPos - perp * 0.3f;

        var offset1Hit = PhysicsHelpers.AnyNonTriggersBetween(offset1, direction, checkDistance, Constants.ShipAndObjectsMask);
        var offset2Hit = PhysicsHelpers.AnyNonTriggersBetween(offset2, direction, checkDistance, Constants.ShipAndObjectsMask);


        return !offset1Hit || !offset2Hit;
    }

    private static void EnsureHexedSprite(PlayerControl player)
    {
        if (player == null || player.cosmetics?.nameText == null)
        {
            return;
        }

        var playerId = player.PlayerId;
        var nameText = player.cosmetics.nameText;
        var nameTextGameObject = nameText.gameObject;

        nameText.ForceMeshUpdate();


        var shouldShow = IsPlayerNameVisible(player, nameText);

        if (!HexedSprites.TryGetValue(playerId, out var spriteRenderer) || spriteRenderer == null)
        {
            var spriteObj = new GameObject($"HexedSprite_{playerId}");

            spriteObj.transform.SetParent(nameTextGameObject.transform, false);
            spriteObj.transform.localPosition = Vector3.zero;

            float textWidth = 0f;
            if (nameText.textBounds.size.x > 0)
            {
                textWidth = nameText.textBounds.size.x / 2f;
            }
            else if (nameText.preferredWidth > 0)
            {
                textWidth = nameText.preferredWidth / 2f;
            }

            spriteObj.transform.localPosition += new Vector3(textWidth + 0.15f, 0f, -0.1f);

            spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = TouExtensionAssets.HexedSprite.LoadAsset();
            spriteRenderer.sortingOrder = nameText.sortingOrder + 1;
            spriteRenderer.transform.localScale = Vector3.one * 0.4f;


            spriteObj.layer = nameTextGameObject.layer;

            HexedSprites[playerId] = spriteRenderer;
        }
        else
        {

            if (spriteRenderer.transform.parent != nameTextGameObject.transform)
            {
                spriteRenderer.transform.SetParent(nameTextGameObject.transform, false);
            }

            float textWidth = 0f;
            if (nameText.textBounds.size.x > 0)
            {
                textWidth = nameText.textBounds.size.x / 2f;
            }
            else if (nameText.preferredWidth > 0)
            {
                textWidth = nameText.preferredWidth / 2f;
            }

            spriteRenderer.transform.localPosition = new Vector3(textWidth + 0.15f, 0f, -0.1f);


            spriteRenderer.sortingOrder = nameText.sortingOrder + 1;
        }



        spriteRenderer.enabled = shouldShow;

        if (shouldShow)
        {

            var spriteColor = spriteRenderer.color;
            spriteColor.a = nameText.color.a;
            spriteRenderer.color = spriteColor;
        }
        else
        {

            var spriteColor = spriteRenderer.color;
            spriteColor.a = 0f;
            spriteRenderer.color = spriteColor;
        }
    }

    private static void EnsureHexedSpriteForMeeting(PlayerControl player, PlayerVoteArea playerVA)
    {
        if (player == null || playerVA == null || playerVA.NameText == null)
        {
            return;
        }

        var playerId = player.PlayerId;
        var nameText = playerVA.NameText;


        nameText.ForceMeshUpdate();


        var meetingKey = (byte)(playerId + 200);

        if (!HexedSprites.TryGetValue(meetingKey, out var spriteRenderer) || spriteRenderer == null)
        {

            var spriteObj = new GameObject($"HexedSprite_Meeting_{playerId}");
            spriteObj.transform.SetParent(playerVA.transform);


            if (playerVA.Megaphone != null)
            {
                spriteObj.layer = playerVA.Megaphone.gameObject.layer;
            }


            float textWidth = 0f;
            if (nameText.textBounds.size.x > 0)
            {
                textWidth = nameText.textBounds.size.x / 2f;
            }
            else if (nameText.preferredWidth > 0)
            {
                textWidth = nameText.preferredWidth / 2f;
            }



            var nameTextLocalPos = nameText.transform.localPosition;
            spriteObj.transform.localPosition = new Vector3(nameTextLocalPos.x + textWidth + 0.15f, nameTextLocalPos.y, -1f);

            spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = TouExtensionAssets.HexedSprite.LoadAsset();
            spriteRenderer.transform.localScale = Vector3.one * 0.4f;

            HexedSprites[meetingKey] = spriteRenderer;
        }
        else
        {

            float textWidth = 0f;
            if (nameText.textBounds.size.x > 0)
            {
                textWidth = nameText.textBounds.size.x / 2f;
            }
            else if (nameText.preferredWidth > 0)
            {
                textWidth = nameText.preferredWidth / 2f;
            }

            var nameTextLocalPos = nameText.transform.localPosition;
            spriteRenderer.transform.localPosition = new Vector3(nameTextLocalPos.x + textWidth + 0.15f, nameTextLocalPos.y, -1f);


            if (spriteRenderer.transform.parent != playerVA.transform)
            {
                spriteRenderer.transform.SetParent(playerVA.transform);
            }
        }

        spriteRenderer.gameObject.SetActive(true);
    }

    private static void HideHexedSprite(PlayerControl player)
    {
        if (player == null)
        {
            return;
        }

        var playerId = player.PlayerId;
        if (HexedSprites.TryGetValue(playerId, out var spriteRenderer) && spriteRenderer != null)
        {
            spriteRenderer.gameObject.SetActive(false);
        }
    }
}