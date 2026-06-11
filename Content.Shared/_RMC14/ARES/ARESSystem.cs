using Content.Shared.Corvax.TTS;

namespace Content.Shared._RMC14.ARES;

public sealed partial class ARESSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _metaData = default!;

    private bool TryGetARES(out Entity<ARESCoreComponent> alert)
    {
        var query = EntityQueryEnumerator<ARESCoreComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            alert = (uid, comp);
            return true;
        }

        alert = default;
        return false;
    }

    public Entity<ARESCoreComponent> EnsureARES()
    {
        if (TryGetARES(out var alert))
            return alert;

        var uid = Spawn();
        _metaData.SetEntityName(uid, "ARES v3.2");
        var comp = EnsureComp<ARESCoreComponent>(uid);
        EnsureComp<TTSComponent>(uid).VoicePrototypeId = "ADJUTANT"; // RuMC TTS Announce
        return (uid, comp);
    }
}
