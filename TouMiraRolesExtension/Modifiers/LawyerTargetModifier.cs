using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameEnd;
using MiraAPI.Modifiers.Types;
using TouMiraRolesExtension.GameOver;

namespace TouMiraRolesExtension.Modifiers;

/// <summary>
/// Modifier that marks a player as a Lawyer's client (defendant).
/// </summary>
public sealed class LawyerTargetModifier : GameModifier
{
    public override string ModifierName => "Lawyer Client";
    public override bool HideOnUi => true;
    [HideFromIl2Cpp] public bool IsHiddenFromList => true;

    public byte OwnerId { get; set; }

    public LawyerTargetModifier() : this(0)
    {
    }

    public LawyerTargetModifier(byte ownerId)
    {
        OwnerId = ownerId;
    }

    public override int GetAmountPerGame()
    {
        return 0;
    }

    public override int GetAssignmentChance()
    {
        return 0;
    }

    public override bool? DidWin(GameOverReason reason)
    {
        if (reason == CustomGameOver.GameOverReason<LawyerGameOver>())
        {
            return true;
        }

        return null;
    }
}