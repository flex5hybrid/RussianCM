using Content.Shared._RMC14.LinkAccount;

namespace Content.Shared._RMC14.DonorCapes;

public static class DonorCapeAccess
{
    public static bool HasAccess(SharedRMCPatronTier? tier, int requiredPriority)
    {
        return tier is { Priority: var priority } && priority <= requiredPriority;
    }
}
