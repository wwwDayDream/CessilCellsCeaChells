using System;
using System.IO;
using BepInEx;
using BepInEx.Bootstrap;
using CessilCellsCeaChells.Merges;
using Mono.Cecil;

namespace CessilCellsCeaChells.Internal;

internal static class AssemblyCacheHandler {
    private const string AssemblyCacheDLLPrefix = "CessilCache.";
    private static string CacheDir = Path.Combine(Paths.CachePath, nameof(CessilCellsCeaChellsDownByTheCeaChore));
    
    internal static bool TryLoadCachedMerges(string assemblyPath, out CessilMerge[] merges)
    {
        merges = [];
        if (!Directory.Exists(CacheDir)) Directory.CreateDirectory(CacheDir);
        
        var cachePath = GetCachePath(assemblyPath);
        var assemblyLastWriteTime = File.GetLastWriteTimeUtc(assemblyPath).ToFileTimeUtc();
        var shouldUseCacheAssembly = File.Exists(cachePath) && File.GetLastWriteTimeUtc(cachePath).ToFileTimeUtc() == assemblyLastWriteTime;
        var assemblyDefinition = LoadAssembly(shouldUseCacheAssembly ? cachePath : assemblyPath);

        
        if (shouldUseCacheAssembly)
            CessilCellsCeaChellsDownByTheCeaChore.Logger.LogDebug($"Falling back to cached merges for '{assemblyDefinition.Name.Name}'. Write Time hasn't changed.");
        else
            CessilCellsCeaChellsDownByTheCeaChore.Logger.LogDebug($"Loading and caching merges for '{assemblyDefinition.Name.Name}'. " + 
                                                                  (File.Exists(cachePath) ? "Write Time has changed." : "Cache doesn't exist."));
        
        if (!CessilMerge.TryCreateMerges(assemblyDefinition, out merges))
            CessilCellsCeaChellsDownByTheCeaChore.Logger.LogDebug($"Plugin '{assemblyDefinition.Name.Name}' doesn't contain any merges.");

        if (!shouldUseCacheAssembly)
            CacheMerges(cachePath, assemblyDefinition, assemblyLastWriteTime, merges);

        return merges.Length > 0;
    }

    private static AssemblyDefinition LoadAssembly(string assemblyPath)
    {
        var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters {
            ReadWrite = true,
            AssemblyResolver = TypeLoader.Resolver
        });
        CessilCellsCeaChellsDownByTheCeaChore.AssembliesToDispose.Add(assemblyDefinition);
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
        
        CessilCellsCeaChellsDownByTheCeaChore.Logger
            .LogDebug($"Caching merges for '{assemblyDefinition.Name.Name}' to '{Path.GetFileName(cachePath.Replace(Paths.BepInExRootPath, "./BepInEx"))}");
        
        newAssemblyDefinition.Write(cachePath);
        if (File.Exists(cachePath))
            File.SetLastWriteTimeUtc(cachePath, DateTime.FromFileTimeUtc(writeTime));
    }

    private static string GetCachePath(string assemblyPath)
    {
        var fileName = Path.GetFileName(assemblyPath);
        return Path.Combine(CacheDir, AssemblyCacheDLLPrefix + fileName);
    }
}