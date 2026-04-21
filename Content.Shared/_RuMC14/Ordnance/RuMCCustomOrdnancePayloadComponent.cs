using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RuMC14.Ordnance;

/// <summary>
/// Хранит параметры взрыва/пожара для кастомного снаряда (84мм ракета или 80мм миномётный снаряд).
/// Заполняется при сборке из деталей на основе реагентов боеголовки.
/// Для ракеты копируется с картриджа на снаряд-проджектайл в момент выстрела.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RuMCCustomOrdnancePayloadComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool HasExplosion;

    [DataField, AutoNetworkedField]
    public float TotalIntensity;

    [DataField, AutoNetworkedField]
    public float IntensitySlope;

    [DataField, AutoNetworkedField]
    public float MaxIntensity;

    [DataField, AutoNetworkedField]
    public bool HasFire;

    [DataField, AutoNetworkedField]
    public float FireIntensity;

    [DataField, AutoNetworkedField]
    public float FireRadius;

    [DataField, AutoNetworkedField]
    public float FireDuration;

    [DataField, AutoNetworkedField]
    public EntProtoId FireEntity = "RMCTileFire";
}
