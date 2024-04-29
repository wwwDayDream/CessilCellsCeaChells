using System;
using System.IO;
using CessilCellsCeaChells.Merges;
using Mono.Cecil;

namespace CessilCellsCeaChells.Internal;

internal static class AssemblyCacheHandler {
    private const string AssemblyCacheDLLPrefix = "CessilCache.";
    internal static bool TryLoadCachedMerges(string assemblyPath, CessilMerger cessilMerger, out CessilMerge[] merges)
    {
        merges = [];
        if (!Directory.Exists(cessilMerger.CachePath)) Directory.CreateDirectory(cessilMerger.CachePath);
        
        var cachePath = Path.Combine(cessilMerger.CachePath, AssemblyCacheDLLPrefix + Path.GetFileName(assemblyPath));
        var assemblyLastWriteTime = File.GetLastWriteTimeUtc(assemblyPath).ToFileTimeUtc();
        var shouldUseCacheAssembly = File.Exists(cachePath) && File.GetLastWriteTimeUtc(cachePath).ToFileTimeUtc() == assemblyLastWriteTime;
        var assemblyDefinition = LoadAssembly(shouldUseCacheAssembly ? cachePath : assemblyPath, cessilMerger);

        
        if (shouldUseCacheAssembly)
            CessilMerger.LogDebugSafe($"Falling back to cached merges for '{assemblyDefinition.Name.Name}'. Write Time hasn't changed.");
        else
            CessilMerger.LogDebugSafe($"Loading and caching merges for '{assemblyDefinition.Name.Name}'. " + 
                                      (File.Exists(cachePath) ? "Write Time has changed." : "Cache doesn't exist."));
        
        if (!CessilMerge.TryCreateMerges(assemblyDefinition, out merges))
            CessilMerger.LogDebugSafe($"Plugin '{assemblyDefinition.Name.Name}' doesn't contain any merges.");

        if (!shouldUseCacheAssembly)
            CacheMerges(cachePath, assemblyDefinition, assemblyLastWriteTime, merges);

        return merges.Length > 0;
    }

    internal static AssemblyDefinition LoadAssembly(string assemblyPath, CessilMerger cessilMerger)
    {
        var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters {
            ReadWrite = true,
            AssemblyResolver = cessilMerger.TypeResolver
        });
        cessilMerger.AssembliesToDispose.Add(assemblyDefinition);
        return assemblyDefinition;
    }

    private static void CacheMerges(string cachePath, AssemblyDefinition assemblyDefinition, long writeTime, CessilMerge[] merges)
    {
        if (File.Exists(cachePath)) File.Delete(cachePath);

        var newAssemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyDefinition.Name, assemblyDefinition.MainModule.Name, ModuleKind.Dll);
        foreach (var merge in merges)
        {
            newAssemblyDefinition.CustomAttributes.Add(merge.ConvertToAttribute(newAssemblyDefinition));
        }
        
        newAssemblyDefinition.Write(cachePath);
        if (File.Exists(cachePath))
            File.SetLastWriteTimeUtc(cachePath, DateTime.FromFileTimeUtc(writeTime));
    }
}