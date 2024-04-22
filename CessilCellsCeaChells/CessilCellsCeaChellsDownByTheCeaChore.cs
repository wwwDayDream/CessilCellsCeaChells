using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;

namespace CessilCellsCeaChells;

internal static class CessilCellsCeaChellsDownByTheCeaChore {
    public static IEnumerable<string> TargetDLLs => Merges.Select(merge => merge.TargetAssemblyName).Distinct();
    public static void Patch(AssemblyDefinition assembly) => CessilMerger.PatchTargetAssembly(assembly);
    public static ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource(nameof(CessilCellsCeaChellsDownByTheCeaChore));

    public static List<Merge> Merges { get; } = [ ];

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

    public static void Finish()
    {
        foreach (var assemblyDefinition in AssembliesToDispose)
            assemblyDefinition.Dispose();
        
        Logger.LogInfo("Finished!");
    }
}
