using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace CessilCellsCeaChells.MSBuild;

public class MergeTask : Task {
    [Required]
    public string IntermediateOutputPath { get; [UsedImplicitly] set; } = null!;

    [Required]
    public ITaskItem[] ReferencePath { get; [UsedImplicitly] set; } = null!;

    [Required]
    public ITaskItem[] PackageReference { get; [UsedImplicitly] set; } = null!;
    
    [Required]
    public ITaskItem[] MergeInto { get; [UsedImplicitly] set; } = null!;
    [Required]
    public ITaskItem[] MergeFrom { get; [UsedImplicitly] set; } = null!;

    [Output]
    public ITaskItem[] RemovedReferences { [UsedImplicitly] get; private set; } = null!;

    [Output]
    public ITaskItem[] MergedReferences { [UsedImplicitly] get; private set; } = null!;

    private const string MergeIntoMetadata = "MergeInto";
    private const string MergeFromMetadata = "MergeFrom";
    private const string FileNameMetadata = "FileName";
    private const string NuGetPackageIdMetadata = "NuGetPackageId";
    private const string FullPathMetadata = "FullPath";

    public override bool Execute()
    {
        var outputDirectory = Path.Combine(IntermediateOutputPath, "merges");
        Directory.CreateDirectory(outputDirectory);

        var packagesToMergeFrom = PackageReference
            .Where(task => task.TryGetMetadata(MergeFromMetadata, out _)).ToDictionary(task => task.ItemSpec);
        var assemblyNamesToMergeFrom = MergeFrom.ToDictionary(taskItem => taskItem.ItemSpec);
        var packagesToMergeInto = PackageReference
            .Where(task => task.TryGetMetadata(MergeIntoMetadata, out _)).ToDictionary(task => task.ItemSpec);
        var assemblyNamesToMergeInto = MergeInto.ToDictionary(task => task.ItemSpec);
        
        var removedReferences = new List<ITaskItem>();
        var mergedReferences = new List<ITaskItem>();

        var mergeFromTasks = ReferencePath
            .Where(taskItem => FilterReferencesByTags(taskItem, MergeFromMetadata, packagesToMergeFrom, assemblyNamesToMergeFrom)).ToArray();
        
        var mergeIntoTasks = ReferencePath
            .Where(taskItem => FilterReferencesByTags(taskItem, MergeIntoMetadata, packagesToMergeInto, assemblyNamesToMergeInto)).ToArray();

        if (mergeIntoTasks.Length == 0 || mergeFromTasks.Length == 0)
            return true;
        
        var hash = mergeFromTasks.Concat(mergeIntoTasks)
            .Aggregate("", (currentHash, assemPath) =>
                ComputeHash((currentHash.Length != 0 ? Encoding.UTF8.GetBytes(currentHash) : [ ])
                    .Concat(File.ReadAllBytes(FilePathFromTask(assemPath))).ToArray()));

        var hashPath = Path.Combine(outputDirectory, "hash.md5");
        var cachePath = Path.Combine(outputDirectory, "cache.txt");

        var isCacheSufficient = File.Exists(hashPath) && File.ReadAllText(hashPath) == hash;
        var cachedTargetAssemblies = File.Exists(cachePath) ? File.ReadAllText(cachePath).Split(';') : [ ];

        CessilMerger.LogDebug += o => Log.LogMessage(o.ToString());
        CessilMerger.LogWarn += o => Log.LogWarning(o.ToString());
        var merger = new CessilMerger();

        if (!isCacheSufficient) {
            var resolverDirectories = mergeFromTasks.Concat(mergeIntoTasks)
                .Select(FilePathFromTask)
                .Select(Path.GetDirectoryName).Where(dir => dir != null).Distinct().ToArray();
            var defaultResolver = new DefaultAssemblyResolver();
            defaultResolver.AddSearchDirectory(typeof(MergeTask).Assembly.Location);
            foreach (var resolverDirectory in resolverDirectories)
                defaultResolver.AddSearchDirectory(resolverDirectory);
            merger.CachePath = outputDirectory;
            merger.TypeResolver = defaultResolver;

            foreach (var mergeFromTask in mergeFromTasks)
            {
                var filePath = FilePathFromTask(mergeFromTask);
                if (!merger.LoadMergesFrom(filePath, out var count)) continue;

                CessilMerger.LogDebugSafe($"Successfully loaded {count} merge{(count == 1 ? "" : "s")} from '{Path.GetFileName(filePath)}'");
            }

            cachedTargetAssemblies = merger.TargetDLLFileNames.ToArray();
            File.WriteAllText(cachePath, string.Join(';', cachedTargetAssemblies));
        }
        
        foreach (var task in mergeIntoTasks)
        {
            var filePath = FilePathFromTask(task);
            var fileName = Path.GetFileName(filePath);
            if (!cachedTargetAssemblies.Contains(fileName)) continue;
            
            var outputPath = Path.Combine(outputDirectory, "Cessil." + fileName);
            
            if (!isCacheSufficient)
            {
                var assemblyDefinition = AssemblyDefinition
                    .ReadAssembly(filePath, new ReaderParameters() { ReadWrite = false, AssemblyResolver = merger.TypeResolver });

                merger.MergeInto(assemblyDefinition);

                assemblyDefinition.Write(outputPath);
                assemblyDefinition.Dispose();
            }
            
            var mergedReference = new TaskItem(outputPath);
            task.CopyMetadataTo(mergedReference);
            mergedReference.RemoveMetadata("ReferenceAssembly");
            
            removedReferences.Add(task);
            mergedReferences.Add(mergedReference);
        }

        if (!isCacheSufficient)
            File.WriteAllText(hashPath, hash);
        merger.Dispose();
        MergedReferences = mergedReferences.ToArray();
        RemovedReferences = removedReferences.ToArray();
        return true;
    }

    private static bool FilterReferencesByTags(ITaskItem taskItem, string tagName, Dictionary<string, ITaskItem> packages, 
        Dictionary<string, ITaskItem> assemblyNames)
    {
        if (taskItem.TryGetMetadata(tagName, out _))
            return true;
        if (taskItem.TryGetMetadata(FileNameMetadata, out var fileName) &&
            assemblyNames.TryGetValue(fileName!, out _))
            return true;
        if (taskItem.TryGetMetadata(NuGetPackageIdMetadata, out var nugetPackageId) &&
            packages.TryGetValue(nugetPackageId!, out _))
            return true;
        return false;
    }

    private static string ComputeHash(byte[] bytes)
    {
        using var md5 = MD5.Create();
        
        md5.TransformFinalBlock(bytes, 0, bytes.Length);

        return ByteArrayToString(md5.Hash);
    }

    private static string ByteArrayToString(IReadOnlyCollection<byte> data)
    {
        var builder = new StringBuilder(data.Count * 2);

        foreach (var b in data)
        {
            builder.AppendFormat("{0:x2}", b);
        }

        return builder.ToString();
    }
    private static string FilePathFromTask(ITaskItem taskItem) => taskItem.GetMetadata(FullPathMetadata);
}