using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Client._RMC14.DonorCapes;

public readonly record struct DonorCapeSection<T>(int RequiredPriority, IReadOnlyList<T> Capes);

public static class DonorCapeLayout
{
    public static IReadOnlyList<DonorCapeSection<T>> BuildSections<T>(
        IEnumerable<T> capes,
        Func<T, int> requiredPriority,
        Func<T, int> number)
    {
        return capes
            .GroupBy(requiredPriority)
            .OrderByDescending(group => group.Key)
            .Select(group => new DonorCapeSection<T>(group.Key, group.OrderBy(number).ToArray()))
            .ToArray();
    }
}
