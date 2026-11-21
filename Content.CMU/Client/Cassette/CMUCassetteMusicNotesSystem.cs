using Content.Shared._RMC14.Cassette;
using Content.Shared.CCVar;
using Content.Shared.CMU14.Cassette;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Shared.Audio.Components;
using Robust.Shared.Configuration;

namespace Content.Client.CMU14.Cassette;

/// <summary>
///     Redraws everyone's cassette players when the setting to hide their music notes changes.
/// </summary>
public sealed class CMUCassetteMusicNotesSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedItemSystem _item = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        Subs.CVar(_cfg, CCVars.CMUHideCassetteMusicNotes, _ => Refresh());
        SubscribeLocalEvent<CassettePlayerComponent, CassettePlayerStateChangedEvent>(OnPlayerState);
    }

    private void OnPlayerState(Entity<CassettePlayerComponent> ent, ref CassettePlayerStateChangedEvent args)
    {
        if (!HasComp<CMUPublicCassettePlayerComponent>(ent) ||
            !TryComp(ent, out SpriteComponent? sprite) ||
            !sprite.LayerMapTryGet("cassette_music", out var layer))
        {
            return;
        }

        _sprite.LayerSetVisible((ent.Owner, sprite), layer, args.State == AudioState.Playing);
    }

    private void Refresh()
    {
        var query = EntityQueryEnumerator<CassettePlayerComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _item.VisualsChanged(uid);
        }
    }
}
