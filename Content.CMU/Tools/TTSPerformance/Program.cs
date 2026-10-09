using System.Diagnostics;
using System.Globalization;
using Content.Client.Corvax.TTS;
using Content.Server.Corvax.TTS;

// Measures only the former cache/snapshot algorithms and their replacements.
// It does not include synthesis, networking, WAV decoding, OpenAL or a running game.
const int recipients = 64;
const int transmissions = 32;
const int frames = 120000;
const int repetitions = 5;
var now = TimeSpan.Zero;
var client = new CMUTTSPlaybackQueue<int, int>(() => now);
for (var lane = 0; lane < 16; lane++)
    client.Enqueue(lane, lane, 1);
var snapshot = new List<int>(16);

Console.WriteLine("Synthetic .NET 10 Release microbenchmark; no live FPS/TPS claim.");
Console.WriteLine($"Radio: {transmissions} transmissions x {recipients} recipients at the same timestamp.");
Console.WriteLine($"Client: {frames} snapshots of 16 waiting lanes; median of {repetitions} warmed runs.");
Compare("radio-delivery-cache", BeforeRadio, AfterRadio);
Compare("client-frame-snapshots", BeforeClient, AfterClient);

int BeforeRadio()
{
    var deliveries = new Dictionary<(int Recipient, ulong Transmission), TimeSpan>();
    var accepted = 0;
    for (ulong transmission = 1; transmission <= transmissions; transmission++)
    {
        for (var recipient = 0; recipient < recipients; recipient++)
        {
            foreach (var (key, expiry) in deliveries.ToArray())
                if (expiry <= now)
                    deliveries.Remove(key);
            if (deliveries.TryAdd((recipient, transmission), now + TimeSpan.FromSeconds(30)))
                accepted++;
        }
    }
    return accepted;
}

int AfterRadio()
{
    var deliveries = new CMUTTSRadioDeliveryCache<(int Recipient, ulong Transmission)>();
    var accepted = 0;
    for (ulong transmission = 1; transmission <= transmissions; transmission++)
        for (var recipient = 0; recipient < recipients; recipient++)
            if (deliveries.TryAdd((recipient, transmission), now))
                accepted++;
    return accepted;
}

int BeforeClient()
{
    var count = 0;
    for (var frame = 0; frame < frames; frame++)
        count += client.Keys.Length;
    return count;
}

int AfterClient()
{
    var count = 0;
    for (var frame = 0; frame < frames; frame++)
    {
        client.CopyKeys(snapshot);
        count += snapshot.Count;
    }
    return count;
}

void Compare(string scenario, Func<int> before, Func<int> after)
{
    if (before() != after())
        throw new InvalidOperationException("Baseline and replacement processed different amounts of work.");
    // Warm both code paths before retaining measurements.
    before();
    after();
    var oldSamples = new List<(double Ms, long Bytes)>();
    var newSamples = new List<(double Ms, long Bytes)>();
    for (var run = 0; run < repetitions; run++)
    {
        if (run % 2 == 0)
        {
            oldSamples.Add(Measure(before));
            newSamples.Add(Measure(after));
        }
        else
        {
            newSamples.Add(Measure(after));
            oldSamples.Add(Measure(before));
        }
    }
    var oldMs = oldSamples.OrderBy(s => s.Ms).ElementAt(repetitions / 2).Ms;
    var newMs = newSamples.OrderBy(s => s.Ms).ElementAt(repetitions / 2).Ms;
    var oldBytes = oldSamples.OrderBy(s => s.Bytes).ElementAt(repetitions / 2).Bytes;
    var newBytes = newSamples.OrderBy(s => s.Bytes).ElementAt(repetitions / 2).Bytes;
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"{scenario}: beforeMs={oldMs:F3} afterMs={newMs:F3} beforeAllocatedBytes={oldBytes} afterAllocatedBytes={newBytes}"));
}

static (double Ms, long Bytes) Measure(Func<int> action)
{
    var bytes = GC.GetAllocatedBytesForCurrentThread();
    var started = Stopwatch.GetTimestamp();
    var result = action();
    var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    var allocated = GC.GetAllocatedBytesForCurrentThread() - bytes;
    GC.KeepAlive(result);
    return (elapsed, allocated);
}
