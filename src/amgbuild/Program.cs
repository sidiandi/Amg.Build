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
        public virtual async Task<string> New(string? name = null)
        {
            var resolvedCmdFile = ResolveNewCmdFile(name);
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

        [Once, Description("The script (.cmd) to work with.")]
        public virtual string? Script { get; set; }

        [Once, Description("overwrite existing files")]
        public virtual bool Overwrite { get; set; }

        [Once]
        public virtual string CmdFile => FindExistingCmdFile(Script, System.Environment.CurrentDirectory);

        [Once, Description("Fix a script")]
        public virtual Task Fix()
        {
            Logger.Information("Fixing {CmdFile}", CmdFile);
            return FixInternal(CmdFile);
        }

        async Task FixInternal(string cmdFile)
        {
            var sourceLayout = new SourceCodeLayout(cmdFile);
            await sourceLayout.Fix();
        }

        [Once]
        protected virtual SourceCodeLayout SourceCodeLayout => new SourceCodeLayout(CmdFile);

        [Once, Description("Print version")]
        public virtual async Task<string> Version()
        {
            var version = Assembly.GetEntryAssembly()!.NugetVersion();
            return await Task.FromResult(version);
        }

        [Once, Description("Open in Visual Studio")]
        public virtual async Task Open()
        {
            var layout = new SourceCodeLayout(CmdFile);
            await Tools.Cmd.Run("start", layout.CsprojFile);
        }

        [Once]
        protected virtual ITool DotnetTool => Tools.Default
            .WithFileName("dotnet.exe")
            .WithWorkingDirectory(SourceCodeLayout.SourceDir);

        [Once]
        protected virtual async Task<IEnumerable<string>> Pack(ITool dotnet)
        {
            var r = await dotnet.Run("pack");
            return r.Output.SplitLines().WhereMatch(new Regex(@"Successfully created package '([^']+)'."));
        }

        [Once, Description("Pack as dotnet tool")]
        public virtual async Task<string> Pack()
        {
            return (await Pack(DotnetTool)).First();
        }

        [Once, Description("Install as global dotnet tool")]
        public virtual async Task Install()
        {
            var nupkgFile = await Pack();

            await DotnetTool.DoNotCheckExitCode()
                .Run("tool", "uninstall", "--global", SourceCodeLayout.Name);

            await DotnetTool.Run(
                "tool", "install",
                "--global",
                "--add-source", nupkgFile.Parent(),
                SourceCodeLayout.Name
                );
        }

        [Once, Description("Adds the script to the users PATH")]
        public virtual async Task<string> AddToPath()
        {
            var dir = System.Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                .Combine(".dotnet", "tools");

            var layout = SourceCodeLayout;
            var shim = dir.Combine(layout.CmdFile.FileName());

            return await shim
                .WriteAllTextAsync($@"@call {layout.CmdFile.Quote()} %*");
        }
    }
}
