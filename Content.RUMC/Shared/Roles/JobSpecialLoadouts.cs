// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 wray-git
namespace Content.Shared.CMU14.Roles;

// Links a job's role loadout to its extra special loadout.
public static class JobSpecialLoadouts
{
    public const string ColonistRoleLoadout = "JobAU14JobCivilianColonist";
    public const string ColonistSpecialLoadout = "AU14ColonistSpecialLoadout";

    public static readonly IReadOnlyDictionary<string, string> ByRoleLoadout = new Dictionary<string, string>
    {
        [ColonistRoleLoadout] = ColonistSpecialLoadout,
    };
}
