using Content.Shared._RMC14.Dialog;
using Content.Shared._RuMC14.Reinforcements;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server._RuMC14.Reinforcements;

public sealed class RuMCReinforcementSystem : SharedRuMCReinforcementSystem
{
    [Dependency] private DialogSystem _dialog = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private readonly Dictionary<EntityUid, Pending> _pending = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<ActorComponent, RuMCReinforcementMessageEvent>(OnMessage);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
    }

    public override void Request(EntityUid actor, string side, Action<RuMCReinforcementCall> onSubmit)
    {
        _pending[actor] = new Pending(side, onSubmit);
        _dialog.OpenInput(
            actor,
            Loc.GetString("rumc-reinforcement-dialog-prompt"),
            new RuMCReinforcementMessageEvent(),
            largeInput: true,
            characterLimit: MessageLimit,
            title: Loc.GetString("rumc-reinforcement-dialog-title"));
    }

    private void OnMessage(Entity<ActorComponent> ent, ref RuMCReinforcementMessageEvent args)
    {
        if (!_pending.Remove(ent, out var pending))
            return;

        var message = args.Message.Trim();
        if (message.Length == 0)
        {
            _popup.PopupEntity(Loc.GetString("rumc-reinforcement-message-required"), ent, ent);
            return;
        }

        pending.OnSubmit(new RuMCReinforcementCall(pending.Side, message));
    }

    private void OnPlayerDetached(PlayerDetachedEvent args)
    {
        _pending.Remove(args.Entity);
    }

    private readonly record struct Pending(string Side, Action<RuMCReinforcementCall> OnSubmit);
}
