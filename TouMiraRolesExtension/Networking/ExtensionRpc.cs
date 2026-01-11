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

    // Added for Mirage decoy refactor:
    // Prime spawns the decoy once (hidden), Place reveals the existing instance.
    MiragePrimeDecoy
}