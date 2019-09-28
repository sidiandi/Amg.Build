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
    internal static class Json
    {
        public static T Read<T>(string path)
        {
            // deserialize JSON directly from a file
            using (var file = new StreamReader(path))
            {
                var serializer = new JsonSerializer();
                var reader = new JsonTextReader(file);
                return serializer.Deserialize<T>(reader);
            }
        }

        public static void Write<T>(string path, T data)
        {
            // serialize JSON directly to a file
            using (var file = new StreamWriter(path))
            {
                var serializer = new JsonSerializer();
                var writer = new JsonTextWriter(file);
                serializer.Serialize(writer, data);
            }
        }

    }

    class RebuildMyself
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static bool IsOutOfDate(string lastFileVersionFile, FileVersion current, string dllFile)
        {
            if (lastFileVersionFile.IsFile())
            {
                try
                {
                    var lastBuildSourceVersion = Json.Read<FileVersion>(lastFileVersionFile);
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
                Json.Write(lastFileVersionFile, current);
                var dllVersion = FileVersion.Get(dllFile);
                return !dllVersion.IsNewer(current);
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
                var name = sourceFile.FileNameWithoutExtension();
                var sourceDir = sourceFile.Parent();
                var csprojFile = sourceDir.Combine(name + ".csproj");
                var cmdFile = sourceDir.Parent().Combine(sourceFile.FileNameWithoutExtension() + ".cmd");
                if (csprojFile.IsFile() && cmdFile.IsFile())
                {
                    var currentFileVersion = FileVersion.Get(sourceDir);
                    var fileVersionFile = dll + ".sources";
                    if (IsOutOfDate(fileVersionFile, currentFileVersion, dll))
                    {
                        Logger.Information("Source files at {sourceDir} have changed. Rebuilding {dll}", sourceDir, dll);
                        var lastBuildFileVersion = Json.Read<FileVersion>(fileVersionFile);
                        var oldDll = (dll + "." + Path.GetRandomFileName() + ".old").EnsureFileNotExists();
                        dll.Move(oldDll);
                        var dotnet = await Once.Create<Dotnet>().Tool();
                        
                        await dotnet
                            .WithEnvironment("Configuration", "Debug")
                            .Run("build",
                            "--force",
                            csprojFile);
                        
                        Json.Write(fileVersionFile, currentFileVersion);
                        
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
