using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using CessilCellsCeaChells.Internal;
using CessilCellsCeaChells.Merges;
using Mono.Cecil;

namespace CessilCellsCeaChells;

internal static class CessilCellsCeaChellsDownByTheCeaChore {
    public static IEnumerable<string> TargetDLLs => Merges.Select(merge => merge.TargetAssemblyName).Distinct();

    public static ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource(nameof(CessilCellsCeaChellsDownByTheCeaChore));

    public static List<CessilMerge> Merges { get; } = [ ];

    public static List<AssemblyDefinition> AssembliesToDispose = [ ];
    
    public static void Initialize()
    {
        Logger.LogInfo("Initialized!");

        LoadPlugins();
    }

    private static void LoadPlugins()
    {
        var bepInExPlugins = Paths.PluginPath ?? "";
        if (!Directory.Exists(bepInExPlugins))
            throw new ArgumentException("Invalid plugin directory!", bepInExPlugins);
        
        foreach (var potentialAssemblySource in Directory.GetFiles(bepInExPlugins, "*.dll", SearchOption.AllDirectories))
        {
            if (!AssemblyCacheHandler.TryLoadCachedMerges(potentialAssemblySource, out var merges)) continue;
            
            Logger.LogInfo($"Successfully loaded {merges.Length} merge{(merges.Length == 1 ? "" : "s")} from '{Path.GetFileName(potentialAssemblySource)}'");
            Merges.AddRange(merges);
        }
    }

    public static void Patch(AssemblyDefinition assembly)
    {
        Logger.LogInfo($"Patching '{assembly.Name.Name}'..");

        foreach (var merge in Merges.Where(merge => merge.TargetAssemblyName == assembly.Name.Name + ".dll"))
        {
            var targetType = assembly.MainModule.GetType(merge.TargetTypeRef.FullName);
            if (targetType == null)
            {
                Logger.LogWarning($"Disregarding merge as it's target type doesn't exist!");
                continue;
            }
            if (merge.TryMergeInto(targetType, out var memberDef))
            {
                Logger.LogDebug($"Successfully merged '{memberDef?.FullName}' into '{targetType.FullName}'");
            }
        }
        
        if (!Directory.Exists(Paths.CachePath)) Directory.CreateDirectory(Paths.CachePath);

        var outputPath = Path.Combine(Paths.CachePath, $"Cessil." + assembly.Name.Name + ".dll");
        assembly.Write(outputPath);

        Logger.LogInfo($"Patching '{assembly.Name.Name}' done! Cached to '{outputPath.Replace(Paths.GameRootPath, ".")}'");
    }
    
    public static void Finish()
    {
        foreach (var assemblyDefinition in AssembliesToDispose)
            assemblyDefinition.Dispose();
        
        Logger.LogInfo("Finished!");
    }
}
