using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._RuCM.Qualifications;

namespace Content.Server._RuCM.Qualifications;

/// <summary>Atomically saves the canonical state with projections from a copy-on-write mutation.</summary>
/// <remarks>
/// The previous store is immutable. Modified players must own a new history object;
/// notes and audit retain their existing prefix and only append. Full Save supports other callers.
/// </remarks>
public interface ICMUQualificationDeltaRepository
{
    Task SaveChanges(QualificationStore store, QualificationStore previous, CancellationToken cancel = default);
}

internal sealed class CMUQualificationWriteSet
{
    public QualificationStore Records { get; }
    public Guid[]? Players { get; }

    private CMUQualificationWriteSet(QualificationStore records, Guid[]? players)
    {
        Records = records;
        Players = players;
    }

    public static CMUQualificationWriteSet Full(QualificationStore store) => new(store, null);

    // The service owns every modified player; unchanged histories retain their published reference.
    // Small configuration projections are retained, including facts reapplied after prototype reloads.
    public static CMUQualificationWriteSet Changes(QualificationStore store, QualificationStore previous)
    {
        if (store.Audit.Count < previous.Audit.Count || store.Notes.Count < previous.Notes.Count)
            throw new QualificationValidationException("history");
        var players = store.Players.Where(pair =>
            !previous.Players.TryGetValue(pair.Key, out var old) || !ReferenceEquals(pair.Value, old))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        var records = new QualificationStore
        {
            Definitions = store.Definitions,
            Roles = store.Roles,
            Players = players,
            Instructors = store.Instructors.Where(pair =>
                !previous.Instructors.TryGetValue(pair.Key, out var old) || pair.Value != old)
                .ToDictionary(pair => pair.Key, pair => pair.Value),
            Notes = store.Notes.Skip(previous.Notes.Count).ToList(),
            Suspensions = store.Suspensions.Where(s => players.ContainsKey(s.Target)).ToList(),
            Audit = store.Audit.Skip(previous.Audit.Count).ToList(),
            Management = store.Management,
            OfficerJobs = store.OfficerJobs,
            CommandingOfficerJobs = store.CommandingOfficerJobs,
            MigrationGroups = store.MigrationGroups,
            TrackerAliases = store.TrackerAliases,
            Migrations = store.Migrations,
        };
        return new(records, players.Keys.ToArray());
    }
}

public sealed partial class QualificationService
{
    private Task PersistChanges(QualificationStore store, QualificationStore previous) =>
        _repository is ICMUQualificationDeltaRepository delta
            ? delta.SaveChanges(store, previous)
            : _repository.Save(store, previous.Revision);
}
