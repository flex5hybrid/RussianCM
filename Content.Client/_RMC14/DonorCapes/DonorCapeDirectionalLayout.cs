using Robust.Shared.Graphics.RSI;

namespace Content.Client._RMC14.DonorCapes;

public static class DonorCapeDirectionalLayout
{
    public const int GameplayFrameCount = 12;

    public static int GetSourceFrame(RsiDirection direction, int animationFrame)
    {
        if (animationFrame is < 0 or >= GameplayFrameCount)
            throw new ArgumentOutOfRangeException(nameof(animationFrame));

        return direction switch
        {
            RsiDirection.South => 0,
            RsiDirection.North => animationFrame + 1,
            RsiDirection.East => 13,
            RsiDirection.West => 14,
            _ => throw new ArgumentOutOfRangeException(nameof(direction)),
        };
    }
}
