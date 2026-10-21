using Content.Shared._RuCM.Qualifications;

namespace Content.Server.Antag;

public sealed partial class AntagSelectionSystem
{
    private bool CMUIsRecruitMind(EntityUid? mind)
    {
        return _jobs.MindTryGetJobId(mind, out var job) && job?.Id == GOVFORRecruitJob.Id;
    }
}
