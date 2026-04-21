using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RuMC14.Ordnance;

/// <summary>Маркер: топливная труба 84мм ракеты. Требует заполнения топливом для сборки.</summary>
[RegisterComponent]
public sealed partial class RuMCRocketTubeComponent : Component
{
    /// <summary>Минимальный объём топлива (u) для сборки ракеты.</summary>
    [DataField]
    public float RequiredFuel = 60f;

    /// <summary>Название решения с топливом.</summary>
    [DataField]
    public string FuelSolution = "fuel";
}

/// <summary>Маркер: боеголовка 84мм ракеты. Должна быть Armed и иметь детонатор.</summary>
[RegisterComponent]
public sealed partial class RuMCRocketWarheadComponent : Component { }

/// <summary>Маркер: корпус 80мм миномётного снаряда. Требует заполнения топливом для сборки.</summary>
[RegisterComponent]
public sealed partial class RuMCMortarShellCasingComponent : Component
{
    /// <summary>Минимальный объём топлива (u) для сборки снаряда.</summary>
    [DataField]
    public float RequiredFuel = 60f;

    /// <summary>Название решения с топливом.</summary>
    [DataField]
    public string FuelSolution = "fuel";
}

/// <summary>DoAfter-событие сборки кастомного снаряда.</summary>
[Serializable, NetSerializable]
public sealed partial class RuMCOrdnanceAssembleDoAfterEvent : SimpleDoAfterEvent { }
