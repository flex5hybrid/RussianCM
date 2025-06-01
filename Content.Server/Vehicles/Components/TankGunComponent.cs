using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Vehicles.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TankGunComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxAmmo")]
    public int MaxAmmo = 20;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("ammo")]
    public int Ammo = 20;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("cooldown")]
    public float Cooldown = 1.5f; // Секунд между выстрелами

    [ViewVariables]
    public TimeSpan NextFire = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("projectilePrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ProjectilePrototype = "TankShell";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("projectileSpeed")]
    public float ProjectileSpeed = 25f;

    // Добавлено новое свойство
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanShoot = true;
}
