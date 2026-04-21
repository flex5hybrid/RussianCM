using Content.Server.Popups;
using Content.Shared._RuMC14.Ordnance;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._RuMC14.Ordnance;

public sealed class RMCOrdnanceAssemblySystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;

    private static readonly EntProtoId AssemblyPrototype = "RMCOrdnanceAssembly";
    private static readonly ProtoId<TagPrototype> LockedTag = "RMCOrdnanceAssemblyLocked";
    private static readonly ProtoId<ToolQualityPrototype> PryingQuality = "Prying";
    private static readonly ProtoId<ToolQualityPrototype> ScrewingQuality = "Screwing";

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCOrdnancePartComponent, InteractUsingEvent>(OnPartInteractUsing);
        SubscribeLocalEvent<RMCOrdnanceAssemblyComponent, InteractUsingEvent>(OnAssemblyInteractUsing);
    }

    private void OnPartInteractUsing(Entity<RMCOrdnancePartComponent> target, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<RMCOrdnancePartComponent>(args.Used, out var usedPart))
            return;

        args.Handled = true;

        // Предмет в активной руке — LEFT, цель взаимодействия — RIGHT
        var leftType = usedPart.PartType;
        var rightType = target.Comp.PartType;

        var assemblyEnt = Spawn(AssemblyPrototype, _transform.GetMapCoordinates(args.User));
        var assembly = EnsureComp<RMCOrdnanceAssemblyComponent>(assemblyEnt);
        assembly.LeftPartType = leftType;
        assembly.RightPartType = rightType;

        QueueDel(args.Used);
        QueueDel(target.Owner);

        _appearance.SetData(assemblyEnt, RMCAssemblyVisualKey.LeftType, leftType);
        _appearance.SetData(assemblyEnt, RMCAssemblyVisualKey.RightType, rightType);
    }

    private void OnAssemblyInteractUsing(Entity<RMCOrdnanceAssemblyComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (_toolSystem.HasQuality(args.Used, PryingQuality))
        {
            if (ent.Comp.IsLocked)
            {
                _popup.PopupEntity("Сначала разберите сборку отвёрткой.", ent, args.User, PopupType.SmallCaution);
                args.Handled = true;
                return;
            }

            Disassemble(ent, args.User);
            args.Handled = true;
            return;
        }

        if (_toolSystem.HasQuality(args.Used, ScrewingQuality))
        {
            ToggleLocked(ent, args.User);
            args.Handled = true;
        }
    }

    private void ToggleLocked(Entity<RMCOrdnanceAssemblyComponent> ent, EntityUid user)
    {
        ent.Comp.IsLocked = !ent.Comp.IsLocked;
        Dirty(ent);

        if (ent.Comp.IsLocked)
        {
            _tags.AddTag(ent, LockedTag);
            _popup.PopupEntity("Сборка закрыта. Можно вставить в корпус.", ent, user);
        }
        else
        {
            _tags.RemoveTag(ent, LockedTag);
            _popup.PopupEntity("Сборка открыта. Можно настроить сенсоры.", ent, user);
        }
    }

    /// <summary>
    /// Разбирает Assembly обратно на две части.
    /// </summary>
    private void Disassemble(Entity<RMCOrdnanceAssemblyComponent> ent, EntityUid user)
    {
        var xform = Transform(ent).Coordinates;

        if (ent.Comp.LeftPartType is { } left)
            Spawn(GetPartProto(left), xform);

        if (ent.Comp.RightPartType is { } right)
            Spawn(GetPartProto(right), xform);

        _popup.PopupEntity("Сборка разобрана.", ent, user);
        QueueDel(ent);
    }

    private static EntProtoId GetPartProto(RMCOrdnancePartType type)
    {
        return type switch
        {
            RMCOrdnancePartType.RMCOrdnanceIgniter => "RMCOrdnanceIgniter",
            RMCOrdnancePartType.RMCOrdnanceTimer => "RMCOrdnanceTimer",
            RMCOrdnancePartType.RMCOrdnanceSignaler => "RMCOrdnanceSignaler",
            RMCOrdnancePartType.RMCOrdnanceProximitySensor => "RMCOrdnanceProximitySensor",
            _ => "RMCOrdnanceIgniter"
        };
    }
}
