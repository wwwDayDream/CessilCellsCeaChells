using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using JetBrains.Annotations;
using Mono.Cecil;

namespace CessilCellsCeaChells.Cli;

internal static class Program {
    public class Options {
        [Value(0, Required = true, HelpText = "Directory containing assemblies to merge into.", MetaName = "MergeInto")]
        public string? MergeIntoArgument { get; [UsedImplicitly] set; }
        [Value(1, Required = true, HelpText = "Directory containing assemblies to load merges from.", MetaName = "MergeFrom")]
        public string? MergeFromArgument { get; [UsedImplicitly] set; }
        
        [Value(2, Required = false, HelpText = "A sequence of directories that contain dependencies to resolve form.", MetaName = "ResolveFrom")]
        public IEnumerable<string>? DependencyDirs { get; [UsedImplicitly] set; }
    }
    
    public static int Main(string[] args) => Parser.Default.ParseArguments<Options>(args).MapResult(Success, Failure);

    private static int Success(Options option)
    {
        var mergeIntoDir = DirOrFileParent(option.MergeIntoArgument!);
        var mergeFromDir = DirOrFileParent(option.MergeFromArgument!);

        if (!Directory.Exists(mergeFromDir))
            throw new ArgumentException("Invalid from directory!", mergeFromDir);
        if (!Directory.Exists(mergeIntoDir))
            throw new ArgumentException("Invalid to directory!", mergeIntoDir);
        
        CessilMerger.LogDebug += o => Console.WriteLine($"[DEBUG]\t{o}");
        CessilMerger.LogWarn += o => Console.WriteLine($"[WARN]\t{o}");

        var resolverDirectories = option.DependencyDirs!.Concat([ mergeIntoDir, mergeFromDir ]).ToArray();

        var defaultResolver = new DefaultAssemblyResolver();
        foreach (var resolverDirectory in resolverDirectories)
            defaultResolver.AddSearchDirectory(resolverDirectory);
        
        var merger = new CessilMerger();
        merger.CachePath = Path.Combine(mergeIntoDir, "Cessil.Cache");
        merger.TypeResolver = defaultResolver;
                
        foreach (var potentialAssemblySource in Directory.GetFiles(mergeFromDir, "*.dll", SearchOption.AllDirectories))
        {
            if (!merger.LoadMergesFrom(potentialAssemblySource, out var count)) continue;
                    
            Console.WriteLine($"Successfully loaded {count} merge{(count == 1 ? "" : "s")} from '{Path.GetFileName(potentialAssemblySource)}'");
        }

        var targetDLLs = merger.TargetDLLFileNames.ToArray();
        foreach (var potentialPatchInto in Directory.GetFiles(mergeIntoDir, "*.dll", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(potentialPatchInto);
            if (!targetDLLs.Contains(fileName)) continue;
            if (!Directory.Exists(merger.CachePath)) Directory.CreateDirectory(merger.CachePath);
            
            Console.WriteLine($"Patching '{fileName}'..");
            
            var outputPath = Path.Combine(merger.CachePath, "Cessil." + fileName);

            var assemblyDefinition = AssemblyDefinition
                .ReadAssembly(potentialPatchInto, new ReaderParameters() { ReadWrite = false, AssemblyResolver = merger.TypeResolver });
            
            merger.MergeInto(assemblyDefinition);
            
            assemblyDefinition.Write(outputPath);
            assemblyDefinition.Dispose();
            
            Console.WriteLine($"Patching '{fileName}' done! Cached to '{outputPath}'");
        }

        return 0;
    }
    private static int Failure(IEnumerable<Error> enumerable)
    {
        var result = -2;
        var errors = enumerable as Error[] ?? enumerable.ToArray();
        if (errors.Any(x => x is HelpRequestedError or VersionRequestedError))
            result = -1;
        return result;
    }

    private static string DirOrFileParent(string dir) => Directory.Exists(dir) ? dir : Path.GetDirectoryName(dir) ?? dir;
}