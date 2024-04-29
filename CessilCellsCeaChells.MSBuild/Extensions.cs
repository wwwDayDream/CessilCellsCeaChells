using System;
using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace CessilCellsCeaChells.MSBuild;

internal static class Extensions
{
    public static bool TryGetMetadata(this ITaskItem taskItem, string metadataName, out string? metadata)
    {
        metadata = default;
        if (((ICollection<string>)taskItem.MetadataNames).Contains(metadataName))
            return (metadata = taskItem.GetMetadata(metadataName)) != null;

        return false;
    }
}