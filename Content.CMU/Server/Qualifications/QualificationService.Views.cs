using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Content.Shared._RuCM.Qualifications;

namespace Content.Server._RuCM.Qualifications;

public sealed partial class QualificationService
{
    /// <summary>Filter private history before cloning, while retaining independent editable DTOs.</summary>
    public QualificationStore RecordSnapshot(QualificationAuthority actor, ref Guid target)
    {
        var cache = Volatile.Read(ref _cache);
        var manager = IsManagement(actor, cache);
        var instructor = QualificationRules.GetInstructorRank(false, cache.Instructors.GetValueOrDefault(actor.Context.Actor)) != null
            || actor.CurrentParticipant && QualificationRules.IsDrillInstructor(actor.Context.Job);
        if (target == Guid.Empty || !manager && !instructor && !actor.CurrentOfficer && !actor.CurrentCo)
            target = actor.Context.Actor;
        var selected = target;
        var view = new QualificationStore
        {
            Revision = cache.Revision,
            Definitions = cache.Definitions,
            Roles = cache.Roles,
            Notes = manager || instructor ? cache.Notes.Where(n => n.Target == selected).ToList() : new(),
            Suspensions = cache.Suspensions.Where(s => s.Target == selected
                || (manager || actor.CurrentOfficer || actor.CurrentCo) && s.Status == "pending").ToList(),
        };
        if (cache.Players.TryGetValue(selected, out var player))
            view.Players[selected] = player;
        if (manager)
        {
            view.Management = cache.Management;
            view.Instructors = cache.Instructors;
            view.OfficerJobs = cache.OfficerJobs;
            view.CommandingOfficerJobs = cache.CommandingOfficerJobs;
            view.MigrationGroups = cache.MigrationGroups;
            view.TrackerAliases = cache.TrackerAliases;
            view.Migrations = cache.Migrations;
            // Walking backwards stops after the last 100 relevant entries, rather than copying all audit.
            for (var i = cache.Audit.Count - 1; i >= 0 && view.Audit.Count < 100; i--)
            {
                var entry = cache.Audit[i];
                if (entry.Target == selected || entry.Target == null)
                    view.Audit.Add(entry);
            }
            view.Audit.Reverse();
        }
        else
        {
            if (cache.Instructors.TryGetValue(selected, out var accreditation))
                view.Instructors[selected] = accreditation;
            if (instructor && cache.Instructors.TryGetValue(actor.Context.Actor, out var own))
                view.Instructors[actor.Context.Actor] = own;
        }
        return view.Clone();
    }
}
