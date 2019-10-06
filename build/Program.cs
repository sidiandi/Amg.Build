using System.Threading.Tasks;
using System;
using System.IO;
using Amg.Build;
using System.Diagnostics;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;

namespace Build
{
    public partial class Program
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static int Main(string[] args) => Amg.Build.Runner.Run<Program>(args);

        string productName => Name;
        string year => DateTime.UtcNow.ToString("yyyy");
        string copyright => $"Copyright (c) {Company} {year}";

        [Description("Release or Debug. Default: Release")]
        public string Configuration { get; set; } = ConfigurationRelease;

        const string ConfigurationRelease = "Release";
        const string ConfigurationDebug = "Debug";

        string Root => Runner.RootDirectory();
        string OutDir => Root.Combine("out", Configuration.ToString());
        string PackagesDir => OutDir.Combine("packages");
        string SrcDir => Root.Combine("src");
        string CommonAssemblyInfoFile => OutDir.Combine("CommonAssemblyInfo.cs");
        string VersionPropsFile => OutDir.Combine("Version.props");

        string SlnFile => SrcDir.Combine($"{Name}.sln");
        string LibDir => SrcDir.Combine(Name);

        [Once]
        protected virtual Dotnet Dotnet => Once.Create<Dotnet>();

        [Once]
        protected virtual Git Git => Git.Create(Runner.RootDirectory());

        [Once]
        protected virtual async Task PrepareBuild()
        {
            await WriteAssemblyInformationFile();
            await WriteVersionPropsFile();
        }

        [Once]
        [Description("Build")]
        public virtual async Task Build()
        {
            await WriteAssemblyInformationFile();
            await WriteVersionPropsFile();
            await (await Dotnet.Tool()).Run("build", SlnFile,
                "--configuration", this.Configuration);
        }

        [Once]
        protected virtual async Task<string> WriteAssemblyInformationFile()
        {
            var v = await Git.GetVersion();
            return await CommonAssemblyInfoFile.WriteAllTextIfChangedAsync(
    $@"// Generated. Changes will be lost.
[assembly: System.Reflection.AssemblyCopyright({copyright.Quote()})]
[assembly: System.Reflection.AssemblyCompany({Company.Quote()})]
[assembly: System.Reflection.AssemblyProduct({productName.Quote()})]
[assembly: System.Reflection.AssemblyVersion({v.AssemblySemVer.Quote()})]
[assembly: System.Reflection.AssemblyFileVersion({v.AssemblySemFileVer.Quote()})]
[assembly: System.Reflection.AssemblyInformationalVersion({v.InformationalVersion.Quote()})]
[assembly: System.Reflection.AssemblyMetadata({nameof(v.NuGetVersionV2).Quote()}, {v.NuGetVersionV2.Quote()})]
");
        }

        [Once]
        protected virtual async Task<string> WriteVersionPropsFile()
        {
            var v = await Git.GetVersion();
            return await VersionPropsFile.WriteAllTextIfChangedAsync(
    $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
    <PropertyGroup>
        <VersionPrefix>{v.MajorMinorPatch}</VersionPrefix>
        <VersionSuffix>{v.NuGetPreReleaseTagV2}</VersionSuffix>
    </PropertyGroup>
</Project>

");
        }

        [Once, Description("run unit tests")]
        public virtual async Task Test()
        {
            await Build();
            await (await Dotnet.Tool()).Run("test",
                SlnFile,
                "--no-build",
                "--configuration", Configuration
                );
        }

        [Once, Description("measure code coverage")]
        public virtual async Task CodeCoverage()
        {
            await Build();
            await (await Dotnet.Tool()).Run("test", SlnFile, "--no-build",
                "--collect:Code Coverage"
                );
        }

        [Once]
        protected virtual ITool DotnetTool => Dotnet.Tool().Result;

        [Once, Description("pack nuget package")]
        public virtual async Task<IEnumerable<string>> Pack()
        {
            var version = (await Git.GetVersion()).NuGetVersionV2;
            await Build();
            await DotnetTool.Run("pack", "--nologo",
                SlnFile,
                "--configuration", Configuration,
                "--no-build",
                "--include-source",
                "--include-symbols",
                "--output", PackagesDir.EnsureDirectoryExists()
                );

            return new[]
            {
                "Amg.Build",
                "Amg.Build.Cake",
                Amgbuild
            }
            .Select(name => PackagesDir.Combine($"{name}.{version}.nupkg"));
        }

        [Once, Description("Commit pending changes and run end to end test")]
        public virtual async Task<string> CommitAndRunEndToEndTest(string message)
        {
            var git = Git.GitTool.DoNotCheckExitCode();
            await git.Run("add", ".");
            await git.Run("commit", "-m", message, "-a");
            await Test();
            await EndToEndTest();
            await Install();
            return (await this.Git.GetVersion()).NuGetVersionV2;
        }

        string TargetFramework => "netcoreapp3.0";

        [Once, Description("Complete test with .cmd bootstrapper file")]
        public virtual async Task EndToEndTest()
        {
            await Git.EnsureNoPendingChanges();
            await Pack();

            var testDir = OutDir.Combine("EndToEndTest");
            await testDir.EnsureNotExists();
            testDir.EnsureDirectoryExists();

            var nugetConfigFile = testDir.Combine("nuget.config");
            await CreateNugetConfigFile(nugetConfigFile, PackagesDir);
            await Nuget
                .WithWorkingDirectory(testDir)
                .Run("source");

            // create script
            var name = "end-to-end-test-of-build";
            var amgbuildTool = (await Dotnet.Tool())
                .WithWorkingDirectory(testDir)
                .WithArguments(Root.Combine("src", Amgbuild, "bin", Configuration, TargetFramework, "amgbuild.dll"));

            await amgbuildTool.Run("new", name);

            var script = testDir.Combine($"{name}.cmd");

            foreach (var d in new[] { "obj", "bin" })
            {
                await testDir.Combine("build", d).EnsureNotExists();
            }

            var version = await Git.GetVersion();

            var build = Tools.Default.WithFileName(script).DoNotCheckExitCode()
                .WithEnvironment(new Dictionary<string, string> { { "AmgBuildVersion", version.NuGetVersion } })
                .WithArguments("--summary", "-vd")
                ;

            void AssertRebuild(IToolResult result)
            {
                if (!result.Output.Contains("INF|Rebuild"))
                {
                    throw new InvalidOperationException("Script was not rebuild.");
                }
            }

            void AssertExitCode(IToolResult result, int expectedExitCode)
            {
                if (!result.ExitCode.Equals(expectedExitCode))
                {
                    throw new InvalidOperationException($"Exit code expected: {expectedExitCode}. Actual: {result.ExitCode}");
                }
            }

            IDisposable AssertRuntime()
            {
                var s = Stopwatch.StartNew();
                return OnDispose(new Action(() => Logger.Information("Runtime: {runtime}", s.Elapsed)));
            }

            async Task ScriptRuns()
            {
                using (AssertRuntime())
                {
                    var result = await build.Run();
                    AssertExitCode(result, 0);
                    if (!result.Output.Contains(version.NuGetVersionV2))
                    {
                        throw new InvalidOperationException();
                    }
                    if (!String.IsNullOrEmpty(result.Error))
                    {
                        throw new InvalidOperationException(result.Error);
                    }
                }
            }

            async Task WhenSourceFileTimestampIsChangedScriptRebuilds()
            {
                using (AssertRuntime())
                {
                    var outdated = DateTime.UtcNow.AddDays(-1);
                    var sourceFile = testDir.Combine(name, "Program.cs");
                    new FileInfo(sourceFile).LastWriteTimeUtc = outdated;
                    var result = await build.Run();
                    AssertExitCode(result, 0);
                    AssertRebuild(result);
                }
            }

            async Task HelpIsDisplayed()
            {
                using (AssertRuntime())
                {
                    var result = await build.Run("--help");
                    AssertExitCode(result, 3);
                }
            }

            await ScriptRuns();
            await WhenSourceFileTimestampIsChangedScriptRebuilds();
            await HelpIsDisplayed();
        }

        static IDisposable OnDispose(Action a) => new OnDisposeAction(a);

        sealed class OnDisposeAction : IDisposable
        {
            private readonly Action action;

            public OnDisposeAction(Action action)
            {
                this.action = action;
            }
            public void Dispose()
            {
                action();
            }
        }



        private static async Task CreateEmptyNugetConfigFile(string nugetConfigFile)
        {
            await nugetConfigFile.WriteAllTextAsync(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
</configuration>");
        }

        private static async Task CreateNugetConfigFile(string nugetConfigFile, string packageSource)
        {
            await nugetConfigFile.WriteAllTextAsync($@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear /> 
    <add key=""EndToEndTestDefault"" value={packageSource.Quote()} />
  </packageSources>
</configuration>");
        }

        static async Task<T> WhenAnyIsCompletedSuccessfully<T>(IEnumerable<Task<T>> tasks)
        {
            var t = tasks.ToList();
            var failed = new List<Task<T>>();

            while (true)
            {
                var c = await Task.WhenAny(tasks);
                if (c.IsCompletedSuccessfully)
                {
                    return await c;
                }
                else
                {
                    failed.Add(c);
                    t.Remove(c);
                    if (t.Count == 0)
                    {
                        var exceptions = failed.Select(_ => _.Exception).Cast<Exception>().ToArray();
                        throw new AggregateException(exceptions);
                    }
                }
            }
        }

        ITool Nuget => Tools.Default.WithFileName("nuget.exe");

        [Once]
        protected virtual async Task<IEnumerable<string>> Push(string nugetPushSource)
        {
            await Git.EnsureNoPendingChanges();
            await Task.WhenAll(Test(), Pack(), EndToEndTest());
            var nupkgFiles = await Pack();
            var push = Nuget.WithArguments("push", "-NonInteractive");

            if (nugetPushSource != null)
            {
                push = push.WithArguments("-Source", nugetPushSource);
            }

            try
            {
                foreach (var nupkgFile in nupkgFiles)
                {
                    await push.Run(nupkgFile);
                }
                return new[] { nugetPushSource };
            }
            catch (Exception e)
            {
                return Enumerable.Empty<string>();
            }
        }

        [Once, Description("push all nuget packages")]
        protected virtual async Task<IEnumerable<string>> Push()
        {
            var pushed = (await Task.WhenAll(NugetPushSource.Select(Push)))
                .SelectMany(_ => _);

            Logger.Information("Pushed to {@pushed}", pushed);
            return pushed;
        }

        [Once, Description("Open in Visual Studio")]
        public virtual async Task OpenInVisualStudio()
        {
            foreach (var configuration in new[] { ConfigurationRelease, ConfigurationDebug })
            {
                var b = Once.Create<Program>();
                b.Configuration = configuration;
                await b.PrepareBuild();
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = Path.GetFullPath(SlnFile),
                UseShellExecute = true
            });
        }

        string Amgbuild => "amgbuild";

        [Once, Description("install amgbuild tool")]
        public virtual async Task Install()
        {
            var version = (await Git.GetVersion()).NuGetVersionV2;
            await Pack();

            await DotnetTool
                .DoNotCheckExitCode().Run(
                "tool", "uninstall",
                "--global",
                Amgbuild);

            await DotnetTool.Run(
                "tool", "install",
                "--add-source", this.PackagesDir,
                "--global",
                "--no-cache",
                "--version", version,
                Amgbuild);
        }

        [Once, Default, Description("Test")]
        public virtual async Task Default()
        {
            await Test();
        }

        static string IncreasePatchVersion(string version)
        {
            var i = version.Split('.').Select(_ => Int32.Parse(_)).ToArray();
            ++i[i.Length - 1];
            return i.Select(_ => _.ToString()).Join(".");
        }

        [Once]
        [Description("Build a release version and push to nuget.org")]
        public virtual async Task<string> Release()
        {
            await Git.EnsureNoPendingChanges();
            var git = Git.Create(this.Root);
            var v = await git.GetVersion();
            Logger.Information("Tagging with {version}", v.MajorMinorPatch);
            var gitTool = Git.GitTool;
            try
            {
                await gitTool.Run("tag", v.MajorMinorPatch);
            }
            catch (ToolException te)
            {
                if (te.Result.Error.Contains("already exists"))
                {
                    await gitTool.Run("tag", IncreasePatchVersion(v.MajorMinorPatch));
                }
            }
            await gitTool.Run("push", "--tags");
            await Push();
            return v.NuGetVersionV2;
        }

        [Once]
        [Description("Deletes all output files")]
        public virtual async Task Clean()
        {
            await this.OutDir.EnsureNotExists();
        }
    }
}