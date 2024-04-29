using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cake.Cli;
using Cake.Common.IO;
using Cake.Common.Solution;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;
using Cake.MinVer;
using Spectre.Console;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}

public sealed class BuildContext : FrostingContext {
    public const string Mono10 = "Mono10";
    public const string Mono11 = "Mono11";
    
    public SolutionProject CessilCellsCeaChells { get; private init; }
    public SolutionProject CessilCellsCeaChellsCli { get; private init; }
    public SolutionProject CessilCellsCeaChellsMSBuild { get; private init; }
    public SolutionProject CessilCellsCeaChellsPreloader { get; private init; }
    public DirectoryPath DistDirectory => Environment.WorkingDirectory.Combine("dist");
    public DirectoryPath DistCessilDirectory => DistDirectory.Combine("cessil");
    public DirectoryPath DistCliDirectory => DistDirectory.Combine("cli");
    public DirectoryPath DistMSBuildDirectory => DistDirectory.Combine("msbuild");
    public List<string> Outputs { get; } = new();

    public static void EventfulLog(object o) => 
        AnsiConsole.Write(new Text(o.ToString() ?? string.Empty, new Style(Color.Green)));

    public MinVerVersion MinVerVersion => this.MinVer(new MinVerSettings() { TagPrefix = "v", DefaultPreReleasePhase = "dev" });
    public string OutputFileName(string projectName, string fileType) => $"{projectName}-{MinVerVersion}.{fileType}";
        
    
    public BuildContext(ICakeContext context)
        : base(context)
    {   
        context.Environment.WorkingDirectory = context.Environment.WorkingDirectory.GetParent();

        DirectoryPath[] dirs = [ DistDirectory, DistCessilDirectory, DistCliDirectory, DistMSBuildDirectory ];
        
        if (context.DirectoryExists(DistDirectory)) context.DeleteDirectory(DistDirectory, new DeleteDirectorySettings(){Force = true, Recursive = true});
        
        foreach (var directoryPath in dirs)
            context.CreateDirectory(directoryPath);
        
        var solution = context.ParseSolution(context.FileSystem.GetFile("./CessilCellsCeaChells.sln").Path);

        foreach (var solutionProject in solution.Projects)
        {
            var easyName = solutionProject.Name.Replace(".", "");
            switch (easyName)
            {
                case nameof(CessilCellsCeaChells):
                    CessilCellsCeaChells = solutionProject;
                    break;
                case nameof(CessilCellsCeaChellsCli):
                    CessilCellsCeaChellsCli = solutionProject;
                    break;
                case nameof(CessilCellsCeaChellsMSBuild):
                    CessilCellsCeaChellsMSBuild = solutionProject;
                    break;
                case nameof(CessilCellsCeaChellsPreloader):
                    CessilCellsCeaChellsPreloader = solutionProject;
                    break;
            }
        }
        
        Log.Information($"Located project '{CessilCellsCeaChells!.Name}'..");
        Log.Information($"Located project '{CessilCellsCeaChellsCli!.Name}'..");
        Log.Information($"Located project '{CessilCellsCeaChellsMSBuild!.Name}'..");
        Log.Information($"Located project '{CessilCellsCeaChellsPreloader!.Name}'..");
    }
}

[TaskName("Restore Solution")] [TaskDescription("Restores the solution dependencies.")]
public class RestoreTask : FrostingTask<BuildContext> {
    public override void Run(BuildContext context) => context.DotNetRestore();
}

[TaskName("Restore Tools")] [TaskDescription("Restores the solution tools.")]
public class RestoreToolsTask : FrostingTask<BuildContext> {
    public override void Run(BuildContext context) => context.DotNetTool("tool restore");
}


[TaskName("Build CessilCellsCeaChells Against Mono.Cecil-0.10.4")]
[TaskDescription("Builds CessilCellsCeaChells for Thunderstore & BepInEx")]
public class BuildC410Task : FrostingTask<BuildContext> {
    public override void Run(BuildContext context)
    {
        context.DotNetBuild(context.CessilCellsCeaChells.Path.FullPath, new DotNetBuildSettings() {
            Configuration = BuildContext.Mono10
        });
    }
}

[TaskName("Build CessilCellsCeaChells Against Mono.Cecil-0.11.5")]
[TaskDescription("Builds CessilCellsCeaChells for MSBuild & CLI")]
public class BuildC411Task : FrostingTask<BuildContext> {
    public override void Run(BuildContext context)
    {
        context.DotNetBuild(context.CessilCellsCeaChells.Path.FullPath, new DotNetBuildSettings() {
            Configuration = BuildContext.Mono11
        });
    }
}

[TaskName("Pack CessilCellsCeaChells")]
[TaskDescription("Packs CessilCellsCeaChells for Nuget")]
public class PackC4Task : FrostingTask<BuildContext> {
    public override void Run(BuildContext context)
    {
        context.DotNetPack(context.CessilCellsCeaChells.Path.FullPath, new DotNetPackSettings() {
            Configuration = BuildContext.Mono10
        });
        var outputDir = context.CessilCellsCeaChells.Path.GetDirectory().Combine("bin").Combine(BuildContext.Mono10);
        var nupkg = context.GetFiles(outputDir.GetFilePath("*.nupkg").FullPath).First();
        var outFileDir = context.DistCessilDirectory
            .GetFilePath(context.OutputFileName(context.CessilCellsCeaChells.Name, "nupkg")).FullPath;
        context.MoveFile(nupkg, outFileDir);

        var friendly = outFileDir.Replace(context.DistDirectory.FullPath, "./dist");
        context.Outputs.Add(friendly);
        BuildContext.EventfulLog(friendly);
    }
}

[TaskName("Pack CessilCellsCeaChells.Cli")]
[TaskDescription("Packs CessilCellsCeaChells.Cli for Github Release")]
public class PackC4CliTask : FrostingTask<BuildContext> {
    public override void Run(BuildContext context)
    {
        context.DotNetBuild(context.CessilCellsCeaChellsCli.Path.FullPath, new DotNetBuildSettings() {
            Configuration = BuildContext.Mono11
        });
        var outputDir = context.CessilCellsCeaChellsCli.Path.GetDirectory().Combine("bin").Combine(BuildContext.Mono11);
        outputDir = context.GetSubDirectories(outputDir).First();
        var outputFile = context.DistCliDirectory
            .GetFilePath(context.CessilCellsCeaChellsCli.Name + $"-{context.MinVerVersion}.zip");
        context.Zip(outputDir, outputFile);
        
        var friendly = outputFile.FullPath.Replace(context.DistDirectory.FullPath, "./dist");
        context.Outputs.Add(friendly);
        BuildContext.EventfulLog(friendly);
    }
}

[TaskName("Pack CessilCellsCeaChells.MSBuild")]
[TaskDescription("Packs CessilCellsCeaChells.MSBuild for Nuget Release")]
public class PackC4MSBuildTask : FrostingTask<BuildContext> {
    public override void Run(BuildContext context)
    {
        context.DotNetPack(context.CessilCellsCeaChellsMSBuild.Path.FullPath, new DotNetPackSettings() {
            Configuration = BuildContext.Mono11
        });
        var outputDir = context.CessilCellsCeaChellsMSBuild.Path.GetDirectory().Combine("bin").Combine(BuildContext.Mono11);
        var nupkg = context.GetFiles(outputDir.GetFilePath("*.nupkg").FullPath).First();
        var outFileDir = context.DistMSBuildDirectory
            .GetFilePath(context.OutputFileName(context.CessilCellsCeaChellsMSBuild.Name, "nupkg")).FullPath;
        context.MoveFile(nupkg, outFileDir);
        
        var friendly = outFileDir.Replace(context.DistDirectory.FullPath, "./dist");
        context.Outputs.Add(friendly);
        BuildContext.EventfulLog(friendly);
    }
}

[TaskName("Pack CessilCellsCeaChells.Preloader")]
[TaskDescription("Packs CessilCellsCeaChells.Preloader for Thunderstore Release")]
public class PackC4PreloaderTask : FrostingTask<BuildContext> {
    public override void Run(BuildContext context)
    {
        context.DotNetBuild(context.CessilCellsCeaChellsPreloader.Path.FullPath, new DotNetBuildSettings() {
            Configuration = BuildContext.Mono10
        });
        var tsAssets = context.Environment.WorkingDirectory.Combine("ts-assets");
        var plainVersion = $"{context.MinVerVersion.Major}.{context.MinVerVersion.Minor}.{context.MinVerVersion.Patch}";
        context.DotNetTool($"tcli build --config-path {tsAssets.GetFilePath("thunderstore.toml").FullPath} --package-version {plainVersion}");
        var fileToRename = context.GetFiles(context.DistDirectory.GetFilePath($"*-{plainVersion}.zip").FullPath).First();
        var newFileName = fileToRename.FullPath.Replace(plainVersion, context.MinVerVersion);
        context.MoveFile(fileToRename, newFileName);
        context.Outputs.Add(newFileName.Replace(context.DistDirectory.FullPath, "./dist"));
        BuildContext.EventfulLog(newFileName.Replace(context.DistDirectory.FullPath, "./dist"));
    }
}

[TaskName("Build")]
[IsDependentOn(typeof(RestoreTask))]
[IsDependentOn(typeof(RestoreToolsTask))]
[IsDependentOn(typeof(BuildC410Task))]
[IsDependentOn(typeof(BuildC411Task))]
[IsDependentOn(typeof(PackC4Task))]
[IsDependentOn(typeof(PackC4CliTask))]
[IsDependentOn(typeof(PackC4MSBuildTask))]
[IsDependentOn(typeof(PackC4PreloaderTask))]
public class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var sb = new StringBuilder("Successfully built the following files!\n");
        foreach (var contextOutput in context.Outputs)
        {
            sb.AppendLine("\t" + contextOutput);
        }
        BuildContext.EventfulLog(sb);
    }
}