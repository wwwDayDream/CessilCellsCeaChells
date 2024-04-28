using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CessilCellsCeaChells.Internal;
using CessilCellsCeaChells.Merges;
using Mono.Cecil;

namespace CessilCellsCeaChells;

public class CessilMerger : IDisposable {
    public static event Action<object>? LogDebug;
    public static event Action<object>? LogWarn;
    public static void LogDebugSafe(object o) => LogDebug?.Invoke(o);
    public static void LogWarnSafe(object o) => LogWarn?.Invoke(o);
    
    public IAssemblyResolver? TypeResolver { get; set; }
    public string CachePath { get; set; } = Path.GetDirectoryName(typeof(CessilMerger).Assembly.Location) ?? "";
    public IEnumerable<string> TargetDLLFileNames => Merges.Select(merge => merge.TargetAssemblyName).Distinct();
    public readonly List<AssemblyDefinition> AssembliesToDispose = [ ];

    private List<CessilMerge> Merges { get; } = [ ];

    public bool LoadMergesFrom(string dllPath, out int count)
    {
        count = 0;
        if (!AssemblyCacheHandler.TryLoadCachedMerges(dllPath, this, out var merges)) return false;
        Merges.AddRange(merges);
        count = merges.Length;
        return true;
    }

    public void MergeInto(AssemblyDefinition assembly)
    {
        foreach (var merge in Merges.Where(merge => merge.TargetAssemblyName == assembly.Name.Name + ".dll"))
        {
            var targetType = assembly.MainModule.GetType(merge.TargetTypeRef.FullName);
            if (targetType == null)
            {
                LogWarnSafe("Disregarding merge as it's target type doesn't exist!");
                continue;
            }
            if (!merge.TryMergeInto(targetType, out var memberDef)) continue;
            
            LogDebugSafe($"Successfully merged '{memberDef?.FullName}' into '{targetType.FullName}'");
        }
    }
    
    public void Dispose()
    {
        LogDebugSafe("Disposing of loaded AssemblyDefinitions..");
        foreach (var assemblyDefinition in AssembliesToDispose)
            assemblyDefinition.Dispose();
    }
}