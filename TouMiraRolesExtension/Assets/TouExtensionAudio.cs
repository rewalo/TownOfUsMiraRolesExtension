using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace TouMiraRolesExtension.Assets;

public static class TouExtensionAudio
{
    // THIS FILE SHOULD ONLY HOLD AUDIO
    private const string AudioPath = "TouMiraRolesExtension.Resources.Audio";
    public static LoadableAsset<AudioClip> WitchLaugh { get; } = new LoadableAudioResourceAsset($"{AudioPath}.witch_laugh.wav");
    public static LoadableAsset<AudioClip> ObjectionSound { get; } = new LoadableAudioResourceAsset($"{AudioPath}.objection.wav");
}