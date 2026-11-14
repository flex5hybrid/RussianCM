using System;
using System.Linq;
using System.Threading;
using Content.Shared._RuCM.Qualifications;
namespace Content.Server._RuCM.Qualifications;
public sealed partial class QualificationService
{
    public bool CMUCanTeach(QualificationAuthority instructor, string qualification, string item)
    {
        var cache = Volatile.Read(ref _cache);
        return cache.Definitions.TryGetValue(qualification, out var definition) && definition.Enabled
            && definition.Items.Any(step => step.Id == item && step.Enabled)
            && QualificationRules.CanTrain(GetInstructorRank(instructor, cache), qualification);
    }
    public (int Completed, int Required) CMUChecklistSummary(Guid recruit)
    {
        var cache = Volatile.Read(ref _cache);
        if (!cache.Definitions.TryGetValue("enlisted", out var definition))
            return default;
        cache.Players.TryGetValue(recruit, out var player);
        var items = definition.Items.Where(item => item.Enabled && item.Required).ToArray();
        return (items.Count(item => player?.Progress.GetValueOrDefault("enlisted")?.ContainsKey(item.Id) == true), items.Length);
    }
}
