using System;
using System.Collections.Generic;
using Robust.Packaging.AssetProcessing;

namespace Content.Packaging;

/// <summary>
/// Merges resource mounts before the engine's parallel asset processing starts.
/// Like ResourceManager, the first mount containing a path supplies that file.
/// </summary>
public sealed class CMUResourceInput : AssetPass
{
    private readonly HashSet<string> _paths = new(StringComparer.Ordinal);

    protected override AssetFileAcceptResult AcceptFile(AssetFile file)
    {
        // Callers inject Resources, Content.CMU, then Content.RUMC synchronously.
        // Selecting after the parallel input would make the winning mount race-dependent.
        lock (_paths)
        {
            if (!_paths.Add(file.Path))
                return AssetFileAcceptResult.Consumed;
        }

        SendFile(file);
        return AssetFileAcceptResult.Consumed;
    }
}
