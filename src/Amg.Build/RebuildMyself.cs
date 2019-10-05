using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Amg.Build
{
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
    /// {sourceDir}\obj\temp\Debug\netcoreapp3.0
    /// The new assembly is executed from within the old assembly and the exit code is passed through
    /// to the caller.
    /// As last step before exiting the process of the old assembly, the new assembly is started again
    /// without waiting for process end. This run of the new assembly will then 
    /// delete the original assembly directory {sourceDir}\bin\Debug\netcoreapp3.0 and hardlink copy
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
                time = DateTime.UtcNow;
            }

            public string AssemblyFile { get; }
            public string SourceDir { get; }
            public string CsprojFile { get; }
            public string Configuration { get; }
            public string TargetFramework { get; }
            public string SourceFileVersionFile => AssemblyFile + ".source";
            public string TempAssemblyFile => SourceDir.Combine("bin", "r" + time.ToShortFileName(), AssemblyFile.FileName());

            readonly DateTime time;
        }

        const string CsprojExt = ".csproj";

        internal static SourceInfo? GetSourceInfo(Assembly assembly)
        {
            // build\bin\Debug\netcoreapp2.1\build.dll
            // build
            var assemblyPath = Assembly.GetEntryAssembly().Location;
            return GetSourceInfo(assemblyPath);
        }

        internal static SourceInfo? GetSourceInfo(string assemblyFile)
        {
            // build\bin\Debug\netcoreapp2.1\build.dll
            // build
            var p = assemblyFile.SplitDirectories();
            if (p.Length < 5 )
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

            var cmdFile = sourceDir.Parent().Combine(name + ".cmd");
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
                targetFramework: p[p.Length-2]
                );
        }

        static void MoveAwayExistingAssembly(SourceInfo sourceInfo)
        {
            var a = sourceInfo.AssemblyFile;
            a.Parent().MoveToBackup();
        }

        static async Task HandleMoveTo()
        {
            try
            {
                var args = GetMoveToArgs();
                if (args == null) return;
                await HandleMoveToInternal(args);
            }
            catch
            {
                // do not complain if things go wrong
            }
            Environment.Exit(0);
        }

        static async Task HandleMoveToInternal(MoveTo move)
        {
            var old = move.dest!.MoveToBackup();
            if (old != null)
            {
                _ = old.EnsureNotExists();
            }
            await move.source!.CopyTree(move.dest!, useHardlinks: true);
        }

        internal static MoveTo? GetMoveToArgs()
        {
            var argsJson = System.Environment.GetEnvironmentVariable(MoveToKey);
            if (String.IsNullOrEmpty(argsJson))
            {
                return null;
            }

            var args = JsonConvert.DeserializeObject<MoveTo>(argsJson);
            return args;
        }

        internal static void SetMoveToArgs(MoveTo move, ProcessStartInfo processStartInfo)
        {
            var json = JsonConvert.SerializeObject(move);
            processStartInfo.EnvironmentVariables.Add(MoveToKey, json);
        }

        internal static string MoveToKey => "key8ce0a148334b44e58b2cd832fdf935ea";
        internal class MoveTo
        {
            public string? source;
            public string? dest;
        }

        /// <summary>
        /// Rebuilds and restarts the entry assembly if the source files have changed since the last time this method was called.
        /// </summary>
        /// If a rebuild is triggered, the new assembly is started automatically with commandLineArguments
        /// <param name="sourceFile"></param>
        /// <param name="commandLineArguments"></param>
        /// <returns></returns>
        public static async Task BuildIfSourcesChanged(string[] commandLineArguments)
        {
            try
            {
                await HandleMoveTo();
                
                var entryAssembly = Assembly.GetEntryAssembly();
                var sourceInfo = GetSourceInfo(entryAssembly);
                if (sourceInfo == null)
                {
                    return;
                }

                var currentSourceVersion = (await GetCurrentSourceVersion(sourceInfo))!;

                if (await SourcesChanged(sourceInfo, currentSourceVersion))
                {
                    Logger.Information("Rebuild {csprojFile}", sourceInfo.AssemblyFile, sourceInfo.CsprojFile);

                    await Build(
                        csprojFile: sourceInfo.CsprojFile,
                        sourceFileVersion: currentSourceVersion,
                        configuration: sourceInfo.Configuration,
                        targetFramework: sourceInfo.TargetFramework,
                        outputDirectory: sourceInfo.TempAssemblyFile.Parent().EnsureDirectoryIsEmpty());

                    var dotnet = await Once.Create<Dotnet>().Tool();
                    
                    var result = await dotnet
                        .Passthrough()
                        .DoNotCheckExitCode()
                        .WithArguments(sourceInfo.TempAssemblyFile)
                        .Run(commandLineArguments);

                    // before exiting, start a process that moves TempAssemblyFile to AssemblyFile
                    var si = new ProcessStartInfo
                    {
                        FileName = sourceInfo.TempAssemblyFile.WithExtension(".exe"),
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };

                    var move = new MoveTo
                    {
                        source = sourceInfo.TempAssemblyFile.Parent(),
                        dest = sourceInfo.AssemblyFile.Parent()
                    };

                    SetMoveToArgs(move, si);
                    Process.Start(si);

                    Environment.Exit(result.ExitCode);
                }
                else
                {
                    _ = CleanupOldBuildDirectories();
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Rebuild attempt failed. Executed code might be out of date.");
            }
        }

        static async Task CleanupOldBuildDirectories()
        {
            await Task.CompletedTask;
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
            if (sourceInfo.SourceFileVersionFile.IsFile())
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
}
