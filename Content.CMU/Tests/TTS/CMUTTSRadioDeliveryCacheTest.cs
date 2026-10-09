using System;
using Content.Server.Corvax.TTS;
using NUnit.Framework;

namespace Content.Tests.CMU14.TTS;

[TestFixture]
public sealed class CMUTTSRadioDeliveryCacheTest
{
    [Test]
    public void DeduplicatesPerRecipientAndTransmissionWithoutExtendingExpiration()
    {
        var cache = new CMUTTSRadioDeliveryCache<(int Recipient, ulong Transmission)>();
        Assert.That(cache.TryAdd((1, 10), TimeSpan.Zero), Is.True);
        Assert.That(cache.TryAdd((2, 10), TimeSpan.Zero), Is.True);
        Assert.That(cache.TryAdd((1, 11), TimeSpan.Zero), Is.True);
        Assert.That(cache.TryAdd((1, 10), TimeSpan.FromSeconds(29)), Is.False);
        Assert.That(cache.TryAdd((1, 10), TimeSpan.FromSeconds(30)), Is.True);
    }

    [Test]
    public void ExpiringOlderDeliveriesDoesNotRemoveNewerOnes()
    {
        var cache = new CMUTTSRadioDeliveryCache<int>();
        cache.TryAdd(1, TimeSpan.Zero);
        cache.TryAdd(2, TimeSpan.FromSeconds(10));
        Assert.That(cache.TryAdd(1, TimeSpan.FromSeconds(30)), Is.True);
        Assert.That(cache.TryAdd(2, TimeSpan.FromSeconds(30)), Is.False);
        Assert.That(cache.TryAdd(2, TimeSpan.FromSeconds(40)), Is.True);
        Assert.That(cache.TryAdd(1, TimeSpan.FromSeconds(40)), Is.False);
    }

    [Test]
    public void RoundRestartClearsDeliveriesAndExpirationOrder()
    {
        var cache = new CMUTTSRadioDeliveryCache<int>();
        cache.TryAdd(1, TimeSpan.FromHours(1));
        cache.Clear();
        Assert.That(cache.TryAdd(1, TimeSpan.Zero), Is.True);
        Assert.That(cache.TryAdd(1, TimeSpan.FromSeconds(30)), Is.True);
    }
}
