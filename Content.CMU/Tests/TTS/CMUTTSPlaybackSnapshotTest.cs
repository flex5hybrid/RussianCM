using System;
using System.Collections.Generic;
using Content.Client.Corvax.TTS;
using NUnit.Framework;

namespace Content.Tests.CMU14.TTS;

[TestFixture]
public sealed class CMUTTSPlaybackSnapshotTest
{
    [Test]
    public void SnapshotRemainsUsableWhenStartingPlaybackRemovesLanes()
    {
        var queue = new CMUTTSPlaybackQueue<int, string>(() => TimeSpan.Zero);
        queue.Enqueue(1, "first", 1);
        queue.Enqueue(2, "second", 1);
        var keys = new List<int> { 99 };
        queue.CopyKeys(keys);
        Assert.That(keys, Is.EquivalentTo(new[] { 1, 2 }));

        foreach (var key in keys)
            Assert.That(queue.TryDequeue(key, out _), Is.True);

        queue.CopyKeys(keys);
        Assert.That(keys, Is.Empty);
    }

    [Test]
    public void ExpirationRemovesMultipleLanesBeforeAdmittingFreshSpeech()
    {
        var now = TimeSpan.Zero;
        var queue = new CMUTTSPlaybackQueue<int, string>(() => now);
        queue.Enqueue(1, "old", 16 * 1024 * 1024);
        queue.Enqueue(2, "old", 16 * 1024 * 1024);
        now = TimeSpan.FromSeconds(13);
        Assert.That(queue.Enqueue(3, "fresh", 32 * 1024 * 1024), Is.True);
        Assert.That(queue.TryDequeue(1, out _), Is.False);
        Assert.That(queue.TryDequeue(2, out _), Is.False);
        Assert.That(queue.TryDequeue(3, out var speech), Is.True);
        Assert.That(speech, Is.EqualTo("fresh"));
    }
}
