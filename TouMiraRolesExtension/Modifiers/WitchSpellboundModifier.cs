using MiraAPI.Modifiers;
using TouMiraRolesExtension.Events.Impostor;

namespace TouMiraRolesExtension.Modifiers;

public sealed class WitchSpellboundModifier : BaseModifier
{
    public byte WitchId { get; set; }
    public int SpellCastMeeting { get; set; }

    public WitchSpellboundModifier(byte witchId)
    {
        WitchId = witchId;
        SpellCastMeeting = WitchEvents.GetCurrentMeetingCount();
    }

    public override string ModifierName => "Spellbound";
    public override bool HideOnUi => true;
}