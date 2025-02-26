namespace Content.Shared.Imperial.ShockWave;


/// <summary>
/// Raised on the equipment when the installation fails.
/// </summary>
public sealed class ShockWaveEntityCollideEvent(EntityUid wave, EntityUid collided) : EntityEventArgs
{
    public EntityUid Wave = wave;
    public EntityUid Collided = collided;
}
