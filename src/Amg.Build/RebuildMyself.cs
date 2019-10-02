using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Amg.Build
{
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
                AssemblyPath = assemblyPath;
                SourceDir = sourceDir;
                CsprojFile = csprojFile;
                Configuration = configuration;
                TargetFramework = targetFramework;
            }

            public string AssemblyPath { get; }
            public string SourceDir { get; }
            public string CsprojFile { get; }
            public string Configuration { get; }
            public string TargetFramework { get; }
            public string SourceFileVersionFile => AssemblyPath + ".source";
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
            var a = sourceInfo.AssemblyPath;
            if (a.IsFile())
            {
                var old = a + ".old";
                if (old.IsFile())
                {
                    old.EnsureFileNotExists();
                }
                a.Move(old);
            }
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
                var entryAssembly = Assembly.GetEntryAssembly();
                var sourceInfo = GetSourceInfo(entryAssembly);
                if (sourceInfo == null)
                {
                    return;
                }

                var currentSourceVersion = await GetCurrentSourceVersion(sourceInfo);
                if (currentSourceVersion == null) throw new NullReferenceException();

                if (await SourcesChanged(sourceInfo, currentSourceVersion))
                {
                    // move away current file
                    MoveAwayExistingAssembly(sourceInfo);

                    await Build(
                        sourceInfo.CsprojFile,
                        sourceInfo.Configuration,
                        sourceInfo.TargetFramework
                        );

                    try
                    {
                        await WriteSourceVersion(sourceInfo, currentSourceVersion!);
                    }
                    catch
                    {
                        Logger.Warning("Cannot write source version.");
                    }

                    var dotnet = await Once.Create<Dotnet>().Tool();
                    var result = await dotnet
                        .Passthrough()
                        .DoNotCheckExitCode()
                        .WithArguments(sourceInfo.AssemblyPath)
                        .Run(commandLineArguments);

                    Environment.Exit(result.ExitCode);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Rebuild attempt failed. Executed code might be out of date.");
            }
        }

        internal static async Task WriteSourceVersion(SourceInfo sourceInfo, FileVersion currentSourceVersion)
        {
            await Json.Write(sourceInfo.SourceFileVersionFile, currentSourceVersion);
        }

        internal static async Task Build(
            string csprojFile, 
            string configuration, 
            string targetFramework)
        {
            var dotnet = await Once.Create<Dotnet>().Tool();

            await dotnet
                .WithEnvironment("Configuration", configuration)
                .WithEnvironment("TargetFramework", targetFramework)
                .Run("build", "--force", csprojFile);
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
                var assemblyFileVersion = await FileVersion.Get(sourceInfo.AssemblyPath);
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
