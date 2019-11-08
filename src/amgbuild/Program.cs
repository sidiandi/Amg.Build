using Amg.Build;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Amg.Extensions;
using Amg.FileSystem;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace amgbuild
{
    internal class Program
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod()!.DeclaringType);

        static int Main(string[] args)
        {
            return Runner.Run(args);
        }

        static string ResolveNewCmdFile(string? name)
        {
            var cmdFile = name == null
                ? defaultCmdFileName
                : name;

            if (!cmdFile.HasExtension(SourceCodeLayout.CmdExtension))
            {
                cmdFile = cmdFile + SourceCodeLayout.CmdExtension;
            }

            cmdFile = cmdFile.Absolute();

            return cmdFile;
        }

        [Once]
        [Description("Create an Amg.Build script")]
        public virtual async Task<string> New(string? scriptName = null)
        {
            var resolvedCmdFile = ResolveNewCmdFile(scriptName);
            var sourceLayout = await Amg.Build.SourceCodeLayout.Create(resolvedCmdFile, overwrite: Overwrite);
            Logger.Information("Amg.Build script {cmdFile} created.", sourceLayout.CmdFile);
            return sourceLayout.CmdFile;
        }

        const string defaultCmdFileName = "build.cmd";

        static string FindDefaultCmdFile(string startDirectory)
        {
            var c = startDirectory.Combine(defaultCmdFileName);
            if (c.IsFile())
            {
                return c;
            }

            foreach (var d in startDirectory.Up())
            {
                var p = d.ParentOrNull();
                if (p != null)
                {
                    c = p.Combine(d.FileName() + SourceCodeLayout.CmdExtension);
                    if (c.IsFile())
                    {
                        return c;
                    }
                }
            }

            throw new ArgumentOutOfRangeException(
                nameof(startDirectory),
                startDirectory,
                $"No {SourceCodeLayout.CmdExtension} file found in {startDirectory}");
        }

        static string FindExistingCmdFile(string? cmdFileSpec, string startDirectory)
        {
            return FindExistingCmdFileInternal(cmdFileSpec, startDirectory).Absolute();
        }

        static string FindExistingCmdFileInternal(string? cmdFileSpec, string startDirectory)
        {
            return cmdFileSpec.Map(spec =>
            {
                if (spec.IsDirectory())
                {
                    return FindDefaultCmdFile(spec);
                }
                var cmdFile = spec;
                if (!cmdFile.HasExtension(SourceCodeLayout.CmdExtension))
                {
                    cmdFile = cmdFile + SourceCodeLayout.CmdExtension;
                }

                if (!cmdFile.IsFile())
                {
                    throw new ArgumentOutOfRangeException(nameof(spec), spec, "File not found.");
                }

                return cmdFile;
            },
            () =>
            {
                return FindDefaultCmdFile(startDirectory);
            });
        }

        [Once, Description("overwrite existing files")]
        public virtual bool Overwrite { get; set; }

        [Once]
        public virtual string CmdFile(string? script) => FindExistingCmdFile(script, System.Environment.CurrentDirectory);

        [Once, Description("Fix a script")]
        public virtual Task Fix(string? script = null)
        {
            var cmdFile = CmdFile(script);
            Logger.Information("Fixing {CmdFile}", cmdFile);
            return FixInternal(cmdFile);
        }

        async Task FixInternal(string cmdFile)
        {
            var sourceLayout = new SourceCodeLayout(cmdFile);
            await sourceLayout.Fix();
        }

        [Once]
        protected virtual SourceCodeLayout SourceCode(string cmdFile) => new SourceCodeLayout(cmdFile);

        [Once, Description("Print version")]
        public virtual async Task<string> Version()
        {
            var version = Assembly.GetEntryAssembly()!.NugetVersion();
            return await Task.FromResult(version);
        }

        [Once, Description("Open in Visual Studio")]
        public virtual async Task Open(string? script = null)
        {
            var cmdFile = CmdFile(script);
            var layout = new SourceCodeLayout(cmdFile);
            await Tools.Cmd.Run("start", layout.CsprojFile);
        }

        [Once]
        protected virtual ITool DotnetTool(SourceCodeLayout sourceCode) => Tools.Default
            .WithFileName("dotnet.exe")
            .WithWorkingDirectory(sourceCode.SourceDir);

        [Once]
        protected virtual async Task<IEnumerable<string>> Pack(ITool dotnet)
        {
            var r = await dotnet.Run("pack");
            return r.Output.SplitLines().WhereMatch(new Regex(@"Successfully created package '([^']+)'."));
        }

        [Once, Description("Pack as dotnet tool")]
        public virtual async Task<string> Pack(string? script=null)
        {
            var cmdFile = CmdFile(script);
            var sourceCode = SourceCode(cmdFile);
            return (await Pack(DotnetTool(sourceCode))).First();
        }

        [Once, Description("Install as global dotnet tool")]
        public virtual async Task Install(string? script=null)
        {
            var cmdFile = CmdFile(script);
            var sourceCode = SourceCode(cmdFile);

            var nupkgFile = await Pack();
            var dotnet = DotnetTool(sourceCode);
            
            await dotnet.DoNotCheckExitCode()
                .Run("tool", "uninstall", "--global", sourceCode.Name);

            await dotnet.Run(
                "tool", "install",
                "--global",
                "--add-source", nupkgFile.Parent(),
                sourceCode.Name
                );
        }

        [Once, Description("Adds the script to the users PATH")]
        public virtual async Task<string> AddToPath(string? script = null)
        {
            var cmdFile = CmdFile(script);
            var sourceCode = SourceCode(cmdFile);

            var dir = System.Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                .Combine(".dotnet", "tools");

            var shim = dir.Combine(sourceCode.CmdFile.FileName());

            return await shim
                .WriteAllTextAsync($@"@call {sourceCode.CmdFile.Quote()} %*");
        }
    }
}
