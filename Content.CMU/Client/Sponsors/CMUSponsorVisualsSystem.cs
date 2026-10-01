using System.Numerics;
using Content.Client.Clothing;
using Content.Shared.Clothing;
using Content.Shared.CMU14.Sponsors;
using Content.Shared.Item;
using Robust.Client.GameObjects;

namespace Content.Client.CMU14.Sponsors;

public sealed class CMUSponsorVisualsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedItemSystem _item = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CMUSponsorGhostComponent, ComponentStartup>(OnGhostStartup);
        SubscribeLocalEvent<CMUSponsorGhostComponent, AfterAutoHandleStateEvent>(OnGhostState);
        SubscribeLocalEvent<CMUSponsorCapeComponent, ComponentStartup>(OnCapeStartup);
        SubscribeLocalEvent<CMUSponsorCapeComponent, AfterAutoHandleStateEvent>(OnCapeState);
        SubscribeLocalEvent<CMUSponsorCapeComponent, GetEquipmentVisualsEvent>(OnCapeVisuals, after: [typeof(ClientClothingSystem)]);
    }

    private void OnGhostStartup(Entity<CMUSponsorGhostComponent> ent, ref ComponentStartup args) => UpdateGhost(ent);
    private void OnGhostState(Entity<CMUSponsorGhostComponent> ent, ref AfterAutoHandleStateEvent args) => UpdateGhost(ent);

    public void UpdateGhost(Entity<CMUSponsorGhostComponent> ent)
    {
        if (TryComp(ent, out SpriteComponent? sprite))
            _sprite.LayerSetRsiState((ent, sprite), 0, ent.Comp.State);
    }

    private void OnCapeStartup(Entity<CMUSponsorCapeComponent> ent, ref ComponentStartup args) => UpdateCape(ent);
    private void OnCapeState(Entity<CMUSponsorCapeComponent> ent, ref AfterAutoHandleStateEvent args) => UpdateCape(ent);

    public void UpdateCape(Entity<CMUSponsorCapeComponent> ent)
    {
        if (TryComp(ent, out SpriteComponent? sprite))
        {
            _sprite.LayerSetData((ent, sprite), 0, new PrototypeLayerData { Shader = TrimShader(ent.Comp.Trim) });
            const string emblemLayer = "cmu-sponsor-emblem";
            if (CMUSponsorCatalog.Patches.TryGetValue(ent.Comp.Emblem, out var icon))
            {
                var layer = _sprite.LayerMapReserve((ent, sprite), emblemLayer);
                _sprite.LayerSetData((ent, sprite), layer, EmblemData(icon, "icon"));
            }
            else if (_sprite.LayerMapTryGet((ent, sprite), emblemLayer, out var layer, false))
                _sprite.LayerSetVisible((ent, sprite), layer, false);
        }
        _item.VisualsChanged(ent);
    }

    private void OnCapeVisuals(Entity<CMUSponsorCapeComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        // Clone layer data: the base clothing layers may be shared by all entities of this prototype.
        for (var i = 0; i < args.Layers.Count; i++)
        {
            var (key, data) = args.Layers[i];
            args.Layers[i] = (key, new PrototypeLayerData
            {
                RsiPath = data.RsiPath, State = data.State, Color = data.Color, Scale = data.Scale,
                Offset = data.Offset, Rotation = data.Rotation, Visible = data.Visible,
                Shader = TrimShader(ent.Comp.Trim),
            });
        }
        if (CMUSponsorCatalog.Patches.TryGetValue(ent.Comp.Emblem, out var icon))
            args.Layers.Add(("cmu-sponsor-cape-emblem", EmblemData(icon, "patch")));
    }

    private static string TrimShader(string trim) => trim switch
    {
        "gold" => "CMUSponsorCapeTrimGold",
        "silver" => "CMUSponsorCapeTrimSilver",
        "red" => "CMUSponsorCapeTrimRed",
        _ => "",
    };

    private static PrototypeLayerData EmblemData(string icon, string state) => new()
    {
        RsiPath = $"_RMC14/Objects/Clothing/Accessory/Patches/Misc/{icon}.rsi",
        State = state,
        // A decorative badge sits on the cape rather than replacing the character's uniform.
        Offset = state == "icon" ? new Vector2(0, 0.1f) : new Vector2(0, -0.05f),
        Scale = state == "icon" ? new Vector2(0.5f) : Vector2.One,
        Visible = true,
    };
}
