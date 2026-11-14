using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.DonorCapes;

/// <summary>
/// Maps the selected donor cape between the character profile and the profile database model.
/// </summary>
public static class DonorCapeProfilePersistence
{
    public static string? Serialize(ProtoId<RMCDonorCapePrototype>? cape)
    {
        return cape?.Id;
    }

    public static ProtoId<RMCDonorCapePrototype>? Deserialize(string? capeId)
    {
        if (string.IsNullOrWhiteSpace(capeId))
            return null;

        return new ProtoId<RMCDonorCapePrototype>(capeId);
    }
}
