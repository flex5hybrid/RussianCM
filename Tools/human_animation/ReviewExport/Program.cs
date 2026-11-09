using System.Text.Json;
using Content.Shared.CMU14.HumanAnimation;

if (args.Length != 1)
    throw new ArgumentException("Supply the review output JSON path.");
var clips = Enum.GetValues<CMUHumanAnimationKind>().Where(kind => kind != CMUHumanAnimationKind.None).Select(kind =>
    new
    {
        name = kind.ToString(),
        frames = Enumerable.Range(0, 48).Select(frame =>
        {
            var seconds = frame / 12f;
            var oneShotDuration = kind switch
            {
                CMUHumanAnimationKind.Melee => CMUHumanAnimation.MeleeDuration,
                CMUHumanAnimationKind.Shoot => CMUHumanAnimation.ShootDuration,
                CMUHumanAnimationKind.Fall => CMUHumanAnimation.FallDuration,
                _ => CMUHumanAnimation.GetUpDuration,
            };
            var progress = Math.Clamp(seconds % 2 / oneShotDuration, 0, 1);
            var cycle = seconds * (kind == CMUHumanAnimationKind.Run ? 1.9f : 1.15f);
            var pose = CMUHumanAnimation.Sample(kind, cycle, seconds, progress);
            return new
            {
                seconds,
                progress,
                gait = new[] { pose.Gait.X, pose.Gait.Y, pose.Gait.Z, pose.Gait.W },
                action = new[] { pose.Action.X, pose.Action.Y, pose.Action.Z, pose.Action.W },
            };
        }).ToArray(),
    }).ToArray();
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0]))!);
File.WriteAllText(args[0], JsonSerializer.Serialize(clips));
Console.WriteLine($"Exported {clips.Length} clips, 48 poses each, from the production sampler.");
