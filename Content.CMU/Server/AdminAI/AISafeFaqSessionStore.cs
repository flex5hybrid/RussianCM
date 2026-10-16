using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.CMU14.AdminAI;

public sealed record AISafeFaqTicket(Guid Id, Guid Channel, int Round, long Epoch, TimeSpan Created, Guid ClarificationCase = default);

/// <summary>Game-thread state only: one current request per player, finite retention and cooldown.</summary>
public sealed class AISafeFaqSessionStore
{
    private readonly Dictionary<Guid, (AISafeFaqTicket? Pending, TimeSpan Next)> _channels = new();
    private long _epoch;
    private TimeSpan _globalNext;
    private readonly HashSet<Guid> _clarified = new();
    public int Count => _channels.Count;

    public AISafeFaqTicket? Begin(Guid channel, int round, TimeSpan now, Guid clarificationCase = default)
    {
        Sweep(now);
        if (channel == Guid.Empty || now < _globalNext || _channels.Count >= 128 && !_channels.ContainsKey(channel)) return null;
        if (clarificationCase != Guid.Empty && (_clarified.Contains(clarificationCase) || _clarified.Count >= 128)) return null;
        if (_channels.TryGetValue(channel, out var current) && now < current.Next) return null;
        var ticket = new AISafeFaqTicket(Guid.NewGuid(), channel, round, _epoch, now, clarificationCase);
        _channels[channel] = (ticket, now + TimeSpan.FromSeconds(5));
        _globalNext = now + TimeSpan.FromMilliseconds(500);
        return ticket;
    }

    public void Invalidate(Guid channel)
    {
        if (_channels.TryGetValue(channel, out var current)) _channels[channel] = (null, current.Next);
    }

    public bool Complete(AISafeFaqTicket ticket, int round, TimeSpan now, bool eligible)
    {
        if (!_channels.TryGetValue(ticket.Channel, out var current) || current.Pending?.Id != ticket.Id) return false;
        _channels[ticket.Channel] = (null, current.Next);
        var valid = eligible && round == ticket.Round && ticket.Epoch == _epoch && now - ticket.Created < TimeSpan.FromSeconds(5);
        if (valid && ticket.ClarificationCase != Guid.Empty) _clarified.Add(ticket.ClarificationCase);
        return valid;
    }

    public void Discard(AISafeFaqTicket ticket)
    {
        if (_channels.TryGetValue(ticket.Channel, out var current) && current.Pending?.Id == ticket.Id) Invalidate(ticket.Channel);
    }

    public void Reset(bool clearClarifications = true)
    { _epoch++; _channels.Clear(); _globalNext = TimeSpan.Zero; if (clearClarifications) _clarified.Clear(); }
    public void Sweep(TimeSpan now)
    {
        foreach (var channel in _channels.Where(pair => now >= pair.Value.Next).Select(pair => pair.Key).ToArray()) _channels.Remove(channel);
    }
}
