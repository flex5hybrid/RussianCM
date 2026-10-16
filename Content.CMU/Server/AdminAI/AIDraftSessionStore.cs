using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.CMU14.AdminAI;

public sealed record AIDraftContextOptions(int MaxConversations = 128, int MaxViews = 32, int MaxMessages = 8,
    int MessageChars = 512, int ContextTtlSeconds = 300, int DraftTtlSeconds = 120, int CooldownSeconds = 5)
{
    public bool IsValid() => MaxConversations is >= 1 and <= 512 && MaxViews is >= 1 and <= 64
        && MaxMessages is >= 1 and <= 16 && MessageChars is >= 64 and <= 1024
        && ContextTtlSeconds is >= 30 and <= 600 && DraftTtlSeconds is >= 15 and <= 300 && CooldownSeconds is >= 2 and <= 60;
}

public sealed record AIDraftTicket(Guid Id, Guid Owner, Guid Channel, Guid CaseId, Guid ViewId,
    int Round, long Revision, long Epoch, string Conversation, CancellationToken Token)
{
    public string Question { get; init; } = "";
    public string[] PlayerMessages { get; init; } = Array.Empty<string>();
    public bool HasHistory { get; init; }
    public ImmutableArray<AIInferenceTurn> Dialogue { get; init; } = ImmutableArray<AIInferenceTurn>.Empty;
    public DateTime ComplaintUtc { get; init; }
}

/// <summary>Game-thread-owned, bounded current-round public ahelp context and one private view per admin.</summary>
public sealed class AIDraftSessionStore(AIDraftContextOptions options)
{
    private readonly Dictionary<Guid, Conversation> _conversations = new();
    private readonly Dictionary<Guid, View> _views = new();
    private readonly Dictionary<Guid, TimeSpan> _prepareAfter = new();
    private long _epoch;
    public event Action<View>? ViewRemoved;
    public IEnumerable<Guid> Owners => _views.Keys.ToArray();
    public int ConversationCount => _conversations.Count;
    public View? Peek(Guid owner) => _views.GetValueOrDefault(owner);
    public Guid? CaseId(Guid channel, int round, TimeSpan now)
    {
        Sweep(now);
        return _conversations.TryGetValue(channel, out var conversation) && conversation.Round == round ? conversation.Id : null;
    }

    public void Accept(Guid channel, int round, bool fromAdmin, string text, bool adminOnly, TimeSpan now)
    {
        if (!options.IsValid() || channel == Guid.Empty || string.IsNullOrWhiteSpace(text)) return;
        Sweep(now);
        if (!_conversations.TryGetValue(channel, out var conversation) || conversation.Round != round)
        {
            // A private admin message alone is never an AI case or context.
            if (adminOnly || fromAdmin) return;
            if (conversation != null) RemoveConversation(channel);
            if (_conversations.Count >= options.MaxConversations)
                RemoveConversation(_conversations.MinBy(pair => pair.Value.LastActivity).Key);
            conversation = new(Guid.NewGuid(), round, now); _conversations[channel] = conversation;
        }
        conversation.Revision++; conversation.LastActivity = now;
        if (!adminOnly)
        {
            var bounded = text.Length > options.MessageChars ? text[..options.MessageChars] : text;
            conversation.Messages.Add(new(Guid.NewGuid(), fromAdmin, bounded));
            if (!fromAdmin) conversation.ComplaintUtc = DateTime.UtcNow;
            // Keep the opening player message and the newest turns within the same count limit.
            while (conversation.Messages.Count > options.MaxMessages)
                conversation.Messages.RemoveAt(options.MaxMessages > 1 ? 1 : 0);
        }
        foreach (var view in _views.Values.Where(v => v.Channel == channel))
        { Retire(view); view.Revision = conversation.Revision; }
    }

    public View? Open(Guid owner, Guid channel, Guid selectionId, int round, TimeSpan now, bool authorized)
    {
        if (!authorized || !options.IsValid() || owner == Guid.Empty || selectionId == Guid.Empty) return null;
        Sweep(now);
        _views.TryGetValue(owner, out var old);
        var cooldown = _prepareAfter.GetValueOrDefault(owner);
        if (old != null) { Retire(old); _views.Remove(owner); }
        if (!_conversations.TryGetValue(channel, out var conversation) || conversation.Round != round) return null;
        if (_views.Count >= options.MaxViews) return null;
        var view = new View(owner, channel, selectionId, conversation.Id, round, conversation.Revision, now) { NextPrepare = cooldown };
        _views.Add(owner, view); return view;
    }

    public View? Get(Guid owner, Guid channel, Guid viewId, Guid selectionId, int round, TimeSpan now, bool authorized)
    {
        if (!authorized) return null;
        Sweep(now);
        return _views.TryGetValue(owner, out var view) && view.Channel == channel && view.Id == viewId
            && view.SelectionId == selectionId && view.Round == round
            && _conversations.TryGetValue(channel, out var conversation) && conversation.Id == view.CaseId ? view : null;
    }

    public AIDraftTicket? Begin(View view, TimeSpan now, int maxConversationChars)
    {
        Sweep(now);
        if (!_views.TryGetValue(view.Owner, out var current) || current != view || view.Pending != null || now < _prepareAfter.GetValueOrDefault(view.Owner)
            || !_prepareAfter.ContainsKey(view.Owner) && _prepareAfter.Count >= options.MaxViews
            || !_conversations.TryGetValue(view.Channel, out var conversation) || maxConversationChars < 64) return null;
        Retire(view); view.NextPrepare = now + TimeSpan.FromSeconds(options.CooldownSeconds);
        _prepareAfter[view.Owner] = view.NextPrepare;
        static string Line(AcceptedMessage message) => $"{(message.FromAdmin ? "Администратор" : "Игрок")}: {message.Text}\n";
        static AcceptedMessage Fit(AcceptedMessage message, int chars)
            => message with { Text = message.Text[..Math.Min(message.Text.Length, chars - (message.FromAdmin ? 16 : 8))] };
        var selected = new List<AcceptedMessage>();
        var text = new StringBuilder();
        if (conversation.Messages.Sum(message => Line(message).Length) <= maxConversationChars)
            selected.AddRange(conversation.Messages);
        else
        {
            var anchorChars = conversation.Messages.Count > 1 ? Math.Min(Line(conversation.Messages[0]).Length, maxConversationChars / 4) : 0;
            var recentChars = maxConversationChars - anchorChars;
            var used = 0;
            foreach (var message in conversation.Messages.Skip(anchorChars > 0 ? 1 : 0).Reverse())
            {
                var length = Line(message).Length;
                if (used + length > recentChars)
                {
                    if (selected.Count == 0) selected.Add(Fit(message, recentChars));
                    break;
                }
                selected.Insert(0, message); used += length;
            }
            if (anchorChars > 0)
                selected.Insert(0, Fit(conversation.Messages[0], anchorChars));
        }
        foreach (var message in selected) text.Append(Line(message));
        if (text.Length == 0) return null;
        view.Cancellation = new();
        view.Pending = new(Guid.NewGuid(), view.Owner, view.Channel, view.CaseId, view.Id, view.Round,
            conversation.Revision, _epoch, text.ToString(), view.Cancellation.Token)
        { Question = conversation.Messages.LastOrDefault(message => !message.FromAdmin)?.Text ?? "",
            PlayerMessages = conversation.Messages.Where(message => !message.FromAdmin).Select(message => message.Text).ToArray(),
            Dialogue = selected.Select(message => new AIInferenceTurn(message.FromAdmin, message.Text)).ToImmutableArray(),
            HasHistory = conversation.Messages.Count > 1, ComplaintUtc = conversation.ComplaintUtc };
        return view.Pending;
    }

    public bool IsCurrent(AIDraftTicket ticket, int round, TimeSpan now, bool authorized)
    {
        if (!authorized || ticket.Epoch != _epoch || ticket.Round != round || ticket.Token.IsCancellationRequested) return false;
        Sweep(now);
        return _views.TryGetValue(ticket.Owner, out var view) && view.Id == ticket.ViewId && view.Pending?.Id == ticket.Id
            && _conversations.TryGetValue(ticket.Channel, out var conversation)
            && conversation.Id == ticket.CaseId && conversation.Revision == ticket.Revision;
    }

    public View? Complete(AIDraftTicket ticket, int round, TimeSpan now, bool authorized, AIInferenceStatus status, string draft)
    {
        if (!IsCurrent(ticket, round, now, authorized)) return null;
        var view = _views[ticket.Owner];
        view.Pending = null; view.Cancellation?.Dispose(); view.Cancellation = null;
        view.Draft = status == AIInferenceStatus.Success ? draft : "";
        view.DraftExpires = now + TimeSpan.FromSeconds(options.DraftTtlSeconds);
        view.LastResult = status;
        return view;
    }

    public void Cancel(View view) => Retire(view);
    public void Revoke(Guid owner)
    {
        if (!_views.Remove(owner, out var view)) return;
        Retire(view); ViewRemoved?.Invoke(view);
    }
    public void Reset()
    {
        _epoch++;
        foreach (var view in _views.Values) Retire(view);
        _views.Clear(); _conversations.Clear(); _prepareAfter.Clear();
    }

    public void Sweep(TimeSpan now)
    {
        foreach (var owner in _prepareAfter.Where(pair => now >= pair.Value).Select(pair => pair.Key).ToArray()) _prepareAfter.Remove(owner);
        foreach (var channel in _conversations.Where(pair => now - pair.Value.LastActivity >= TimeSpan.FromSeconds(options.ContextTtlSeconds))
                     .Select(pair => pair.Key).ToArray()) RemoveConversation(channel);
        foreach (var view in _views.Values)
        {
            if (view.Draft.Length > 0 && now >= view.DraftExpires) Retire(view);
            if (view.Report != null && now >= view.ReportExpires) view.Report = null;
        }
    }

    private void RemoveConversation(Guid channel)
    {
        _conversations.Remove(channel);
        foreach (var owner in _views.Where(pair => pair.Value.Channel == channel).Select(pair => pair.Key).ToArray()) Revoke(owner);
    }
    private static void Retire(View view)
    {
        var cancellation = view.Cancellation;
        view.Cancellation = null; view.Pending = null; view.Draft = ""; view.Sources = Array.Empty<AIRulePassage>(); view.LastResult = null;
        view.Report = null;
        if (cancellation != null) _ = Task.Run(() => { try { cancellation.Cancel(); } catch (AggregateException) { } finally { cancellation.Dispose(); } });
    }

    private sealed class Conversation(Guid id, int round, TimeSpan now)
    {
        public readonly Guid Id = id;
        public readonly int Round = round;
        public long Revision;
        public TimeSpan LastActivity = now;
        public DateTime ComplaintUtc;
        public readonly List<AcceptedMessage> Messages = new();
    }
    private sealed record AcceptedMessage(Guid Id, bool FromAdmin, string Text);
    public sealed class View(Guid owner, Guid channel, Guid selectionId, Guid caseId, int round, long revision, TimeSpan now)
    {
        public readonly Guid Id = Guid.NewGuid();
        public readonly Guid Owner = owner, Channel = channel, SelectionId = selectionId, CaseId = caseId;
        public readonly int Round = round;
        public long Revision = revision;
        public TimeSpan NextPrepare, DraftExpires;
        public readonly TimeSpan OpenedAt = now;
        public AIDraftTicket? Pending;
        public CancellationTokenSource? Cancellation;
        public string Draft = "";
        public AIInferenceStatus? LastResult;
        public AIRulePassage[] Sources = Array.Empty<AIRulePassage>();
        public AICaseReport? Report;
        public Guid ReportId;
        public Guid LastSentReport;
        public TimeSpan ReportExpires;
    }
}
