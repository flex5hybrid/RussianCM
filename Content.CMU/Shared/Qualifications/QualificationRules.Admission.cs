namespace Content.Shared._RuCM.Qualifications;

public static partial class QualificationRules
{
    /// <summary>Admission polling needs only a boolean, without sorting/copying a missing-requirement list.</summary>
    public static bool IsJobAllowed(PlayerTrainingState? player, RoleRequirement? role,
        InstructorAccreditation? accreditation = null)
    {
        if (role == null || role.Synthetic)
            return true;
        var instructor = IsDrillInstructor(role.JobId);
        if (!role.Enabled && !instructor)
            return true;
        if (instructor && GetInstructorRank(false, accreditation) == null)
            return false;
        var minimum = instructor && role.MinimumLevel < MilitaryLevel.Sergeant
            ? MilitaryLevel.Sergeant : role.MinimumLevel;
        if (EffectiveLevel(player) < minimum)
            return false;
        foreach (var id in role.Professional)
        {
            if (!HasProfessionalQualification(player, id))
                return false;
        }
        return true;
    }

    private static bool HasProfessionalQualification(PlayerTrainingState? player, string id)
    {
        if (player?.Grants.TryGetValue(id, out var grant) == true)
            return grant.Status == QualificationStatus.Active;

        // The sergeant qualification formerly covered squad leaders as well as platoon sergeants.
        return id == "squad_leader" && EffectiveLevel(player) >= MilitaryLevel.Sergeant;
    }
}
