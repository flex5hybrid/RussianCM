using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Log;

namespace Content.Shared._RuMC14.Audio;

public abstract class SharedGridAmbienceSystem : EntitySystem
{
    private EntityQuery<GridAmbienceComponent> _query;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GridAmbienceComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<GridAmbienceComponent, ComponentHandleState>(OnHandleState);

        _query = GetEntityQuery<GridAmbienceComponent>();
    }

    public virtual void SetEnabled(EntityUid uid, bool value, GridAmbienceComponent? component = null)
    {
        if (!_query.Resolve(uid, ref component, false))
        {
            return;
        }

        if (component.Enabled == value)
            return;

        component.Enabled = value;
        Dirty(uid, component);
    }

    public virtual void SetSound(EntityUid uid, SoundSpecifier sound, GridAmbienceComponent? component = null)
    {
        if (!_query.Resolve(uid, ref component, false))
        {
            return;
        }

        if (component.Sound == sound)
            return;

        component.Sound = sound;
        Dirty(uid, component);
    }

    public virtual void SetVolume(EntityUid uid, float value, GridAmbienceComponent? component = null)
    {
        if (!_query.Resolve(uid, ref component, false))
        {
            return;
        }

        if (MathHelper.CloseToPercent(component.Volume, value))
            return;

        component.Volume = value;
        Dirty(uid, component);
    }

    private void OnGetState(EntityUid uid, GridAmbienceComponent component, ref ComponentGetState args)
    {
        args.State = new GridAmbienceComponentState
        {
            Enabled = component.Enabled,
            Sound = component.Sound,
            Volume = component.Volume,
        };
    }

    private void OnHandleState(EntityUid uid, GridAmbienceComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not GridAmbienceComponentState state)
        {
            return;
        }

        SetEnabled(uid, state.Enabled, component);
        SetSound(uid, state.Sound, component);
        SetVolume(uid, state.Volume, component);
    }
}
