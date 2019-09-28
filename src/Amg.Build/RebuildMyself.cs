using Newtonsoft.Json;
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
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static T ReadJson<T>(string path)
        {
            // deserialize JSON directly from a file
            using (StreamReader file = File.OpenText(path))
            {
                var serializer = new JsonSerializer();
                var reader = new JsonTextReader(file);
                return serializer.Deserialize<T>(reader);
            }
        }

        static void WriteJson<T>(string path, T data)
        {
            // serialize JSON directly to a file
            using (var file = new StreamWriter(path))
            {
                var serializer = new JsonSerializer();
                var writer = new JsonTextWriter(file);
                serializer.Serialize(writer, data);
            }
        }

        static bool IsOutOfDate(string lastFileVersionFile, FileVersion current)
        {
            if (lastFileVersionFile.IsFile())
            {
                try
                {
                    var lastBuildSourceVersion = ReadJson<FileVersion>(lastFileVersionFile);
                    var outOfDate = !lastBuildSourceVersion.Equals(current);
                    return outOfDate;
                }
                catch
                {
                    return true;
                }
            }
            else
            {
                WriteJson<FileVersion>(lastFileVersionFile.EnsureParentDirectoryExists(), null);
                return true;
            }
        }

        /// <summary>
        /// Rebuilds assembly from sourceFile if the source files have changed since the last time this method was called.
        /// </summary>
        /// If a rebuild is triggered, the new assembly is started automatically with commandLineArguments
        /// <param name="sourceFile"></param>
        /// <param name="commandLineArguments"></param>
        /// <returns></returns>
        public static async Task BuildIfOutOfDate(
            Assembly assembly,
            string sourceFile,
            string[] commandLineArguments)
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (!entryAssembly.Equals(assembly))
            {
                Logger.Warning("Cannot rebuild an assembly {assembly} that is not the entry assembly {entryAssembly}", assembly, entryAssembly);
                return;
            }

            if (sourceFile.IsFile())
            {
                var dll = assembly.Location;
                var sourceDir = sourceFile.Parent();
                var csprojFile = sourceDir.Glob("*.csproj").SingleOrDefault();
                if (csprojFile != null)
                {
                    var currentFileVersion = FileVersion.Get(sourceDir);
                    var fileVersionFile = dll + ".sources";
                    if (IsOutOfDate(fileVersionFile, currentFileVersion))
                    {
                        Logger.Information("Source files at {sourceDir} have changed. Rebuilding {dll}", sourceDir, dll);
                        var lastBuildFileVersion = ReadJson<FileVersion>(fileVersionFile);
                        var oldDll = (dll + ".old").EnsureFileNotExists();
                        dll.Move(oldDll);
                        var dotnet = await Once.Create<Dotnet>().Tool();
                        
                        await dotnet
                            .WithEnvironment("Configuration", "Debug")
                            .Run("build",
                            "--force",
                            csprojFile);
                        
                        WriteJson(fileVersionFile, currentFileVersion);
                        
                        var result = await dotnet
                            .Passthrough()
                            .DoNotCheckExitCode()
                            .WithArguments(dll)
                            .Run(commandLineArguments);
                        
                        Environment.Exit(result.ExitCode);
                    }
                }
            }
        }
    }
}
