using Robust.Shared.Utility;

namespace Content.Client._RMC14.DonorCapes;

public static class DonorCapePreview
{
    public static SpriteSpecifier GetPreview(SpriteSpecifier? preview, SpriteSpecifier fallback)
    {
        return preview ?? fallback;
    }
}
