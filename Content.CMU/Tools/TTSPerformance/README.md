# TTS hot-path allocation check

Run from the repository root with the SDK selected by `global.json`:

```sh
dotnet run --project Content.CMU/Tools/TTSPerformance/TTSPerformance.csproj -c Release -p:UseSharedCompilation=false
```

The tool compiles the actual replacement classes. The radio baseline reproduces the
previous dictionary snapshot/expiry loop, with 32 transmissions to 64 recipients at
one timestamp. The client baseline uses the retained `Keys` snapshot API and compares
it with `CopyKeys` over 120,000 snapshots of 16 queued lanes. Both paths are warmed,
measurement order alternates, and five runs supply independent median elapsed times
and main-thread allocated bytes. Cache initialization is included in the radio results;
client initialization and buffer capacity growth are outside the measured frame loop.

These are synthetic CPU/allocation measurements, not FPS or TPS improvements. They
exclude speech synthesis, packet transmission, WAV decoding, audio drivers, gameplay,
expiry-heavy traffic, prediction and rendering. Timing depends on the machine and
competing work. The tool does not force garbage collection or alter game settings.

One observed Linux/.NET 10.0.401 run of the workloads above produced:

| Workload | Previous elapsed | Replacement elapsed | Previous allocated bytes | Replacement allocated bytes |
| --- | ---: | ---: | ---: | ---: |
| Radio cache, 2,048 deliveries | 5.161 ms | 0.317 ms | 50,633,280 | 314,144 |
| Client, 120,000 snapshots | 2.189 ms | 1.610 ms | 10,560,000 | 0 |

The byte counts measure allocations, not retained heap or exact GC pause time.
Live-round captures are required before attributing player-visible improvement.

To measure the whole game, capture comparable scenes and player counts before and
after the change. On the client, run `cmu_client_perf start 300 20`, close the console,
reproduce the drop, then use `cmu_client_perf open` to retrieve the recording. On the
server, preserve the corresponding log window and run `cmuperf status` and
`cmuperf report`. The existing server capture is described in
`Content.CMU/Server/Diagnostics/Performance/README.md`; client capture limitations
are in `Content.CMU/Client/Diagnostics/Performance/README.md`.

Compare frame-time p95/p99 and stall counts, achieved TPS against the configured
target, allocations and GC pause windows. Include the map, player count, game build,
event and approximate timestamp. Average FPS alone can hide repeated short stalls.

Regression fixtures:

```sh
dotnet test Content.Tests/Content.Tests.csproj -c Release -p:UseSharedCompilation=false --filter 'FullyQualifiedName~CMUTTS'
```

The fixtures exercise per-recipient transmission identity, the existing 30-second
expiration boundary, duplicate attempts that do not extend retention, round-reset
cleanup, stable playback snapshots, speech order and backlog/memory limits.
