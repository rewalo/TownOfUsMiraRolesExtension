using MiraAPI.Modifiers;
using TouMiraRolesExtension.Modifiers;
using TouMiraRolesExtension.Roles.Neutral;
using TownOfUs.Utilities;

namespace TouMiraRolesExtension.Utilities;

/// <summary>
/// Utility methods for working with Lawyer/Client relationships, with support for multiple lawyers.
/// </summary>
public static class LawyerUtils
{
    /// <summary>
    /// Finds the client (defendant) for a given lawyer id by scanning for the replicated <see cref="LawyerTargetModifier"/>.
    /// This is safer than relying on <see cref="LawyerRole.Client"/> which can be null/desynced on some clients.
    /// </summary>
    /// <param name="lawyerId">The lawyer's PlayerId</param>
    /// <returns>The client player if found, null otherwise</returns>
    public static PlayerControl? FindClientForLawyer(byte lawyerId)
    {
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc == null)
            {
                continue;
            }

            if (pc.HasModifier<LawyerTargetModifier>(m => m.OwnerId == lawyerId))
            {
                return pc;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the lawyer for a specific client player.
    /// </summary>
    /// <param name="client">The client player</param>
    /// <param name="lawyerId">The specific lawyer's PlayerId to check for</param>
    /// <returns>The lawyer role if found, null otherwise</returns>
    public static LawyerRole? GetLawyerForClient(PlayerControl client, byte lawyerId)
    {
        if (client == null || !client.HasModifier<LawyerTargetModifier>(x => x.OwnerId == lawyerId))
        {
            return null;
        }

        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (pc != null && pc.PlayerId == lawyerId && pc.IsRole<LawyerRole>())
            {
                return pc.GetRole<LawyerRole>();
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all lawyers for a specific client player.
    /// </summary>
    /// <param name="client">The client player</param>
    /// <returns>List of lawyer roles for this client</returns>
    public static List<LawyerRole> GetAllLawyersForClient(PlayerControl client)
    {
        if (client == null)
        {
            return [];
        }

        var lawyerModifiers = client.GetModifiers<LawyerTargetModifier>();
        var lawyers = new List<LawyerRole>();

        foreach (var modifier in lawyerModifiers)
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && pc.PlayerId == modifier.OwnerId && pc.IsRole<LawyerRole>())
                {
                    var lawyerRole = pc.GetRole<LawyerRole>();
                    if (lawyerRole != null)
                    {
                        lawyers.Add(lawyerRole);
                    }

                    break;
                }
            }
        }

        return lawyers;
    }

    /// <summary>
    /// Gets the client for a specific lawyer.
    /// </summary>
    /// <param name="lawyer">The lawyer player</param>
    /// <returns>The client player if found, null otherwise</returns>
    public static PlayerControl? GetClientForLawyer(PlayerControl lawyer)
    {
        if (lawyer == null)
        {
            return null;
        }

        var lawyerRole = lawyer.GetRole<LawyerRole>();
        return lawyerRole?.Client ?? FindClientForLawyer(lawyer.PlayerId);
    }

    /// <summary>
    /// Checks if a player is a client of a specific lawyer.
    /// </summary>
    /// <param name="player">The player to check</param>
    /// <param name="lawyerId">The lawyer's PlayerId</param>
    /// <returns>True if the player is a client of the specified lawyer</returns>
    public static bool IsClientOfLawyer(PlayerControl player, byte lawyerId)
    {
        return player != null && player.HasModifier<LawyerTargetModifier>(x => x.OwnerId == lawyerId);
    }

    /// <summary>
    /// Checks if a player is a client of any lawyer.
    /// </summary>
    /// <param name="player">The player to check</param>
    /// <returns>True if the player is a client of any lawyer</returns>
    public static bool IsClientOfAnyLawyer(PlayerControl player)
    {
        return player != null && player.HasModifier<LawyerTargetModifier>();
    }

    /// <summary>
    /// Checks if two players have a lawyer/client relationship.
    /// </summary>
    /// <param name="lawyer">The lawyer player</param>
    /// <param name="client">The client player</param>
    /// <returns>True if they have a lawyer/client relationship</returns>
    public static bool HasLawyerClientRelationship(PlayerControl lawyer, PlayerControl client)
    {
        if (lawyer == null || client == null)
        {
            return false;
        }

        var lawyerRole = lawyer.GetRole<LawyerRole>();
        return lawyerRole != null &&
               lawyerRole.Client != null &&
               lawyerRole.Client.PlayerId == client.PlayerId;
    }
}
