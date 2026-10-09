using System;
using System.Collections.Generic;

namespace Content.Server.Corvax.TTS;

/// <summary>
/// Deduplicates radio deliveries without scanning all recent listeners for every recipient.
/// Callers supply monotonically increasing simulation time and clear the cache on round restart.
/// </summary>
public sealed class CMUTTSRadioDeliveryCache<TKey> where TKey : notnull
{
    private static readonly TimeSpan Retention = TimeSpan.FromSeconds(30);
    private readonly HashSet<TKey> _deliveries = new();
    private readonly Queue<(TKey Key, TimeSpan ExpiresAt)> _expiries = new();

    public bool TryAdd(TKey key, TimeSpan now)
    {
        // All entries have the same lifetime, so insertion order is also expiration order.
        while (_expiries.TryPeek(out var entry) && entry.ExpiresAt <= now)
        {
            _expiries.Dequeue();
            _deliveries.Remove(entry.Key);
        }

        if (!_deliveries.Add(key))
            return false;

        _expiries.Enqueue((key, now + Retention));
        return true;
    }

    public void Clear()
    {
        _deliveries.Clear();
        _expiries.Clear();
    }
}
