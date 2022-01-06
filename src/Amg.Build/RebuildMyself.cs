using Amg.Extensions;
using Amg.FileSystem;
using System.Reflection;

namespace Amg.Build;

/// <summary>
/// Rebuilds own assembly
/// </summary>
/// Procedure:
/// Compare the version of the source files during the last build with the current version of the source files.
/// The version of the source files during the last build is stored in source.json in the directory of the entry assembly.
/// file name, last modified date, and length are used to determine the version of a file.
/// The obj and bin directories are ignored when for determining the source file version.
/// 
/// When the versions are not identical, the assembly is built new in
/// {sourceDir}\obj\temp\Debug\net6.0
/// The new assembly is executed from within the old assembly and the exit code is passed through
/// to the caller.
/// As last step before exiting the process of the old assembly, the new assembly is started again
/// without waiting for process end. This run of the new assembly will then 
/// delete the original assembly directory {sourceDir}\bin\Debug\net6.0 and hardlink copy
/// the new assembly directory to this location.
/// The parameters for this operation are passed in an environment variable MoveToKey.
/// 
class RebuildMyself
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

    internal class SourceInfo
    {
        public SourceInfo(
            string assemblyPath,
            string sourceDir,
            string csprojFile,
            string configuration,
            string targetFramework
            )
        {
            AssemblyFile = assemblyPath;
            SourceDir = sourceDir;
            CsprojFile = csprojFile;
            Configuration = configuration;
            TargetFramework = targetFramework;
        }

        public string AssemblyFile { get; }
        public string SourceDir { get; }
        public string CsprojFile { get; }
        public string Configuration { get; }
        public string TargetFramework { get; }
        public string SourceFileVersionFile => AssemblyFile + ".source";

        string BuildRoot => SourceDir.Combine("bin", "r");
        string TemporaryBuildRoot => SourceDir.Combine("bin", "t");

        public IEnumerable<string> BuildDirectories()
        {
            return BuildRoot.EnumerateDirectories("*")
                .OrderBy(_ => _.FileName())
                .ToList();
        }

        public string TemporaryBuildDirectory(FileVersion fileVersion)
            => TemporaryBuildRoot.Combine(Json.Hash(fileVersion));

        public string BuildDirectory(FileVersion fileVersion)
            => BuildRoot.Combine(Json.Hash(fileVersion));
    }

    const string CsprojExt = ".csproj";

    internal static SourceInfo? GetSourceInfo(Assembly assembly)
    {
        var assemblyPath = Assembly.GetEntryAssembly().Location;
        return GetSourceInfo(assemblyPath);
    }

    internal static SourceInfo? GetSourceInfo(string assemblyFile)
    {
        var p = assemblyFile.SplitDirectories();
        if (p.Length < 5)
        {
            Logger.Debug("Cannot determine source directory for {assemblyPath}.", assemblyFile);
            return null;
        }

        var name = assemblyFile.FileNameWithoutExtension();
        var sourceDir = p.Take(p.Length - 4).CombineDirectories();
        var csprojFile = sourceDir.Combine(name + CsprojExt);

        if (!csprojFile.IsFile())
        {
            Logger.Debug("project file {csprojFile} not found.", csprojFile);
            return null;
        }

        var cmdFile = sourceDir.Parent()!.Combine(name + ".cmd");
        if (cmdFile == null || !cmdFile.IsFile())
        {
            Logger.Debug("{cmdFile} not found.", cmdFile);
            return null;
        }

        return new SourceInfo(
            assemblyPath: assemblyFile,
            sourceDir: sourceDir,
            csprojFile: csprojFile,
            configuration: p[p.Length - 3],
            targetFramework: p[p.Length - 2]
            );
    }

    static async Task MoveAwayExistingAssembly(SourceInfo sourceInfo)
    {
        var a = sourceInfo.AssemblyFile;
        await a.Parent().MoveToBackup();
    }

    /// <summary>
    /// Rebuilds and restarts the entry assembly if the source files have changed since the last time this method was called.
    /// </summary>
    /// If a rebuild is triggered, the new assembly is started automatically with commandLineArguments
    /// <param name="sourceFile"></param>
    /// <param name="commandLineArguments"></param>
    /// <returns></returns>
    public static async Task<SourceInfo?> BuildIfSourcesChanged(string[] commandLineArguments)
    {
        try
        {
            await RebuildCleanup.Handle();

            var entryAssembly = Assembly.GetEntryAssembly();
            Logger.Debug(new { entryAssembly });
            var sourceInfo = GetSourceInfo(entryAssembly);
            if (sourceInfo == null)
            {
                Logger.Debug("no source code found for {entryAssembly}. Rebuild not possible.", entryAssembly);
                return null;
            }

            var currentSourceVersion = (await GetCurrentSourceVersion(sourceInfo))!;

            if (await SourcesChanged(sourceInfo, currentSourceVersion))
            {
                var assemblyFile = await ProvideAssembly(sourceInfo, currentSourceVersion);

                var dotnet = await Once.Create<Dotnet>().Tool();

                var result = await dotnet
                    .Passthrough()
                    .DoNotCheckExitCode()
                    .WithArguments(assemblyFile)
                    .Run(commandLineArguments);

                RebuildCleanup.Start(
                    assemblyFile.WithExtension(".exe"),
                    new RebuildCleanup.Args
                    {
                        Source = assemblyFile.Parent(),
                        Dest = sourceInfo.AssemblyFile.Parent()
                    });

                Environment.Exit(result.ExitCode);
            }
            else
            {
                _ = CleanupOldBuildDirectories(sourceInfo);
            }

            return sourceInfo;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Rebuild attempt failed. Executed code might be out of date.", ex);
        }
    }

    internal static async Task<string> ProvideAssembly(SourceInfo sourceInfo, FileVersion currentSourceVersion)
    {
        var outputDirectory = sourceInfo.BuildDirectory(currentSourceVersion);

        if (!outputDirectory.IsDirectory())
        {
            Logger.Information("Rebuild {csprojFile} into output directory {outputDirectory}",
                sourceInfo.CsprojFile, outputDirectory);

            var tempOutputDirectory = sourceInfo.TemporaryBuildDirectory(currentSourceVersion);
            await Build(
                csprojFile: sourceInfo.CsprojFile,
                sourceFileVersion: currentSourceVersion,
                configuration: sourceInfo.Configuration,
                targetFramework: sourceInfo.TargetFramework,
                outputDirectory: tempOutputDirectory.EnsureDirectoryIsEmpty());
            await tempOutputDirectory.CopyTree(outputDirectory.EnsureParentDirectoryExists());
        }
        var tempAssembly = outputDirectory.Combine(sourceInfo.AssemblyFile.FileName());
        return tempAssembly;
    }

    static DateTime BuildDate(string buildDirectory)
    {
        var source = SourceVersionFile(buildDirectory);
        var info = source.Info();
        return info == null
            ? DateTime.MinValue
            : info.LastWriteTimeUtc;
    }

    static async Task CleanupOldBuildDirectories(SourceInfo source)
    {
        foreach (var dir in source.BuildDirectories()
            .OrderBy(BuildDate)
            .TakeAllBut(1)
            )
        {
            try
            {
                Logger.Debug("Delete old build directory {dir}", dir);
                var toBeDeleted = dir.Parent().Combine("toBeDeleted-" + dir.FileName());
                await dir.Move(toBeDeleted);
                await toBeDeleted.EnsureNotExists();
            }
            catch (System.UnauthorizedAccessException ex)
            {
                Logger.Debug("Cannot delete {dir} at the moment: {ex}", dir, ex);
            }
        }
    }

    internal static async Task WriteSourceVersion(
        string outputDirectory,
        FileVersion sourceVersion)
    {
        var sourceVersionFile = SourceVersionFile(outputDirectory);

        try
        {
            await Json.Write(sourceVersionFile, sourceVersion);
        }
        catch (Exception e)
        {
            Logger.Warning(e, "Cannot write {sourceVersionFile}", sourceVersionFile);
        }
    }

    internal static async Task<FileVersion?> ReadSourceVersion(
        string outputDirectory)
    {
        var f = SourceVersionFile(outputDirectory);
        try
        {
            if (f.IsFile())
            {
                return await Json.Read<FileVersion>(f);
            }
        }
        catch
        {
            // ignore and fall through
        }

        return null;
    }

    static string SourceVersionFile(string dir) => dir.Combine("source.json");

    internal static async Task Build(
        string csprojFile,
        FileVersion sourceFileVersion,
        string configuration,
        string targetFramework,
        string outputDirectory)
    {
        var dotnet = await Once.Create<Dotnet>().Tool();

        await dotnet
            .WithEnvironment("Configuration", configuration)
            .WithEnvironment("TargetFramework", targetFramework)
            .Run("build", "--force", csprojFile,
            "--output", outputDirectory
            );
        await WriteSourceVersion(outputDirectory, sourceFileVersion);
    }

    internal static async Task<bool> SourcesChanged(SourceInfo sourceInfo, FileVersion sourceVersion)
    {
        if (sourceInfo.SourceFileVersionFile.IsFile() && sourceInfo.SourceFileVersionFile.LastWriteTimeUtc() >= sourceInfo.AssemblyFile.LastWriteTimeUtc())
        {
            var lastBuildSourceVersion = await ReadLastBuildSourceVersion(sourceInfo);
            if (lastBuildSourceVersion == null)
            {
                return true;
            }
            Logger.Debug(new { lastBuildSourceVersion, sourceVersion });
            return !lastBuildSourceVersion.Equals(sourceVersion);
        }
        else
        {
            await Json.Write(sourceInfo.SourceFileVersionFile, sourceVersion);
            var assemblyFileVersion = await FileVersion.Get(sourceInfo.AssemblyFile);
            Logger.Debug(new { assemblyFileVersion, sourceVersion });
            if (assemblyFileVersion == null || sourceVersion == null)
            {
                return true;
            }
            return !assemblyFileVersion.IsNewer(sourceVersion);
        }
    }

    internal static async Task<FileVersion?> GetCurrentSourceVersion(SourceInfo sourceInfo)
    {
        return await FileVersion.Get(sourceInfo.SourceDir);
    }

    internal static async Task<FileVersion?> ReadLastBuildSourceVersion(SourceInfo sourceInfo)
    {
        try
        {
            return await Json.Read<FileVersion>(sourceInfo.SourceFileVersionFile);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Error while reading source version from {sourceVersionFile}",
                sourceInfo.SourceFileVersionFile);
            return null;
        }
    }
}
