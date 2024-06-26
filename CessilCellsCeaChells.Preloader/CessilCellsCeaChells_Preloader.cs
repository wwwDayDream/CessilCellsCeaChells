using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using JetBrains.Annotations;
using Mono.Cecil;

namespace CessilCellsCeaChells.Preloader;

[UsedImplicitly]
internal static class CessilCellsCeaChellsDownByTheCeaChore {
    [UsedImplicitly]
    public static IEnumerable<string> TargetDLLs => Merger.TargetDLLFileNames;

    private static CessilMerger Merger { get; } = new CessilMerger();
    private static ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource(nameof(CessilCellsCeaChellsDownByTheCeaChore));
    
    [UsedImplicitly]
    public static void Initialize()
    {
        CessilMerger.LogDebug += Logger.LogDebug;
        CessilMerger.LogWarn += Logger.LogWarning;
        
        Merger.CachePath = Path.Combine(Paths.CachePath, nameof(CessilCellsCeaChellsDownByTheCeaChore));
        Merger.TypeResolver = TypeLoader.Resolver;
        
        LoadPlugins();
        
        Logger.LogInfo("Initialized!");
    }

    private static void LoadPlugins()
    {
        var bepInExPlugins = Paths.PluginPath ?? "";
        if (!Directory.Exists(bepInExPlugins))
            throw new ArgumentException("Invalid plugin directory!", bepInExPlugins);
        
        foreach (var potentialAssemblySource in Directory
                     .GetFiles(bepInExPlugins, "*.dll", SearchOption.AllDirectories)
                     .OrderBy(path =>
                     {
                         using var sha256 = SHA256.Create();
                         var hashValue = path.Replace(Paths.PluginPath, "");
                         var int32 = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashValue))
                             .Aggregate(0, (i, b) => i + b);
                         Logger.LogDebug($"Hashed '{hashValue}' to '{int32}'");
                         return int32;
                     }))
        {
            if (!Merger.LoadMergesFrom(potentialAssemblySource, out var count)) continue;
            
            Logger.LogInfo($"Successfully loaded {count} merge{(count == 1 ? "" : "s")} from '{Path.GetFileName(potentialAssemblySource)}'");
        }
    }

    [UsedImplicitly]
    public static void Patch(AssemblyDefinition assembly)
    {
        Logger.LogInfo($"Patching '{assembly.Name.Name}'..");

        Merger.MergeInto(assembly);
        
        if (!Directory.Exists(Paths.CachePath)) Directory.CreateDirectory(Paths.CachePath);

        var outputPath = Path.Combine(Paths.CachePath, "Cessil." + assembly.Name.Name + ".dll");
        assembly.Write(outputPath);

        Logger.LogInfo($"Patching '{assembly.Name.Name}' done! Cached to '{outputPath}'");
    }
    
    [UsedImplicitly]
    public static void Finish()
    {
        Merger.Dispose();
        
        Logger.LogInfo("Finished!");
    }
}
