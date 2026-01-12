namespace TouMiraRolesExtension.Networking;

public enum ExtensionRpc : uint
{
    TrapperPlaceTrap,
    TrapperTriggerTrap,
    SetLawyerClient,
    SendLawyerChat,
    SendClientChat,
    WitchSpell,
    WitchSpellNotification,
    LawyerObject,
    ForestallerReveal,
    WraithPlaceLantern,
    WraithReturnLantern,
    WraithBreakLantern,
    MiragePlaceDecoy,
    MirageDestroyDecoy,
    MirageTriggerDecoy,
    MiragePrimeDecoy,
    WitchClearAllSpellbound,
    WitchClearSpellboundPlayer,
    HackerActivateJam,
    HackerStartJam,
    HackerSetJamCharges,
    HackerResetRound
}