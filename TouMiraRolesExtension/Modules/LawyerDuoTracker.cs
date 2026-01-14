namespace TouMiraRolesExtension.Modules;

/// <summary>
/// Tracks Lawyer -> Client links for UI purposes (end-game summary markers, etc).
/// This is intentionally independent of live role/modifier state so it still works
/// if one/both players are dead or if transient state is cleared during end-game.
/// </summary>
public static class LawyerDuoTracker
{
    private static readonly Dictionary<byte, byte> LawyerToClient = new();
    private static readonly Dictionary<byte, HashSet<byte>> ClientToLawyers = new();

    public static void ClearAll()
    {
        LawyerToClient.Clear();
        ClientToLawyers.Clear();
    }

    public static void SetClient(byte lawyerId, byte clientId)
    {
        // Remove old mapping if it exists.
        if (LawyerToClient.TryGetValue(lawyerId, out var oldClientId) &&
            ClientToLawyers.TryGetValue(oldClientId, out var oldSet))
        {
            oldSet.Remove(lawyerId);
            if (oldSet.Count == 0)
            {
                ClientToLawyers.Remove(oldClientId);
            }
        }

        LawyerToClient[lawyerId] = clientId;

        if (!ClientToLawyers.TryGetValue(clientId, out var set))
        {
            set = new HashSet<byte>();
            ClientToLawyers[clientId] = set;
        }
        set.Add(lawyerId);
    }

    public static IReadOnlyCollection<byte> GetLawyers()
    {
        return LawyerToClient.Keys.ToArray();
    }

    public static IReadOnlyCollection<byte> GetClients()
    {
        return ClientToLawyers.Keys.ToArray();
    }
}