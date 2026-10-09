using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Client.Corvax.TTS;

/// <summary>
/// Bounded per-speaker queues. Expired speech is discarded rather than played long after the conversation.
/// </summary>
public sealed class CMUTTSPlaybackQueue<TKey, TValue>(Func<TimeSpan> now) where TKey : notnull
{
    private const int MaxPerSpeaker = 6;
    private const int MaxQueued = 48;
    private const int MaxBytes = 32 * 1024 * 1024;
    private static readonly TimeSpan MaxAge = TimeSpan.FromSeconds(12);
    private readonly Dictionary<TKey, Queue<(TValue Value, int Bytes, TimeSpan Added)>> _queues = new();
    private readonly List<TKey> _expiryKeys = new();
    private int _count;
    private int _bytes;

    public TKey[] Keys => _queues.Count == 0 ? Array.Empty<TKey>() : _queues.Keys.ToArray();

    /// <summary>Copies a stable key snapshot into a caller-owned reusable buffer.</summary>
    public void CopyKeys(List<TKey> keys)
    {
        keys.Clear();
        keys.AddRange(_queues.Keys);
    }

    public bool Enqueue(TKey key, TValue value, int bytes)
    {
        CopyKeys(_expiryKeys);
        foreach (var queuedKey in _expiryKeys)
            DiscardExpired(queuedKey);
        if (bytes < 0 || bytes > MaxBytes || _count >= MaxQueued || _bytes > MaxBytes - bytes)
            return false;
        if (!_queues.TryGetValue(key, out var queue))
        {
            queue = new();
            _queues.Add(key, queue);
        }
        if (queue.Count >= MaxPerSpeaker)
            return false;
        queue.Enqueue((value, bytes, now()));
        _count++;
        _bytes += bytes;
        return true;
    }

    private void DiscardExpired(TKey key)
    {
        var queue = _queues[key];
        while (queue.TryPeek(out var entry) && now() - entry.Added >= MaxAge)
        {
            queue.Dequeue();
            _count--;
            _bytes -= entry.Bytes;
        }
        if (queue.Count == 0)
            _queues.Remove(key);
    }

    public bool TryDequeue(TKey key, out TValue value)
    {
        value = default!;
        if (!_queues.TryGetValue(key, out var queue))
            return false;
        while (queue.TryDequeue(out var entry))
        {
            _count--;
            _bytes -= entry.Bytes;
            if (queue.Count == 0)
                _queues.Remove(key);
            if (now() - entry.Added >= MaxAge)
                continue;
            value = entry.Value;
            return true;
        }
        return false;
    }

    public void Clear(TKey key)
    {
        if (!_queues.Remove(key, out var queue))
            return;
        foreach (var entry in queue)
        {
            _count--;
            _bytes -= entry.Bytes;
        }
    }

    public void Clear()
    {
        _queues.Clear();
        _count = 0;
        _bytes = 0;
    }
}
