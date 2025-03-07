using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.ImperialLightning.Events;


[Serializable, NetSerializable]
public sealed partial class SpawnLightningEffectsMessage : EntityEventArgs
{
    public (Vector2 StartCoords, NetEntity? StartEntityPoint) StartPoint;
    public (Vector2 TargetCoords, NetEntity? TargetEntityPoint) TargetPoint;
    public Color LightningColor = Color.FromHex("#1A40F0");
    public Vector2 Offset = Vector2.Zero;
    public float Speed = 0.0f;
    public float Intensity = 2.0f;
    public float Seed = 0.0f;
    public float Amplitude = 0.2f;
    public float Frequency = 3.0f;

    public TimeSpan LifeTime = TimeSpan.FromSeconds(1);
}
