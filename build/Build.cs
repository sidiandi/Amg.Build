using System.Threading.Tasks;
using System;
using System.IO;
using Amg.Build;
using System.Diagnostics;
using System.ComponentModel;

public partial class BuildTargets
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    string productName => name;
    string year => DateTime.UtcNow.ToString("yyyy");
    string copyright => $"Copyright (c) {company} {year}";
    string configuration => "Debug";

    string Root { get; set; } = ".".Absolute();
    string OutDir => Root.Combine("out", configuration);
    string PackagesDir => OutDir.Combine("packages");
    string SrcDir => Root.Combine("src");
    string CommonAssemblyInfoFile => OutDir.Combine("CommonAssemblyInfo.cs");
    string VersionPropsFile => OutDir.Combine("Version.props");

    string SlnFile => SrcDir.Combine($"{name}.sln");
    string LibDir => SrcDir.Combine(name);

    [Once]
    protected virtual Amg.Build.Builtin.Dotnet Dotnet => new Amg.Build.Builtin.Dotnet();

    [Once]
    protected virtual Amg.Build.Builtin.Git Git => new Amg.Build.Builtin.Git();

    [Once] [Description("Build")]
    public virtual async Task Build()
    {
        await WriteAssemblyInformationFile();
        await WriteVersionPropsFile();
        await (await Dotnet.Tool()).Run("build", SlnFile);
    }

    [Once]
    protected virtual async Task<string> WriteAssemblyInformationFile()
    {
        var v = await Git.GetVersion();
        return await CommonAssemblyInfoFile.WriteAllTextIfChangedAsync(
$@"// Generated. Changes will be lost.
[assembly: System.Reflection.AssemblyCopyright({copyright.Quote()})]
[assembly: System.Reflection.AssemblyCompany({company.Quote()})]
[assembly: System.Reflection.AssemblyProduct({productName.Quote()})]
[assembly: System.Reflection.AssemblyVersion({v.AssemblySemVer.Quote()})]
[assembly: System.Reflection.AssemblyFileVersion({v.AssemblySemFileVer.Quote()})]
[assembly: System.Reflection.AssemblyInformationalVersion({v.InformationalVersion.Quote()})]
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

    [Once] [Description("run unit tests")]
	public virtual async Task Test()
    {
        await Build();
        await (await Dotnet.Tool()).Run("test", SlnFile, "--no-build");
    }

    [Once] [Description("measure code coverage")]
    public virtual async Task CodeCoverage()
    {
        await Build();
        await (await Dotnet.Tool()).Run("test", SlnFile, "--no-build",
            "--collect:Code Coverage"
            );
    }

    [Once] [Description("pack nuget package")]
    public virtual async Task<string> Pack()
    {
        var version = (await Git.GetVersion()).NuGetVersionV2;
        await Build();
        await (await Dotnet.Tool()).Run("pack",
            LibDir,
            "--configuration", configuration,
            "--no-build",
            "--include-source",
            "--include-symbols",
            "--output", PackagesDir.EnsureDirectoryExists()
            );

        return PackagesDir.Combine($"{name}.{version}.nupkg");
    }

    [Once] [Description("push nuget package")]
    protected virtual async Task Push()
    {
        await Push(nugetPushSource);
    }

    [Once]
    [Description("push nuget package")]
    protected virtual async Task Push(string nugetPushSource)
    {
        await Git.EnsureNoPendingChanges();
        await Task.WhenAll(Test(), Pack());
        var nupkgFile = await Pack();
        var nuget = new Tool("nuget.exe");
        await nuget.Run(
            "push",
            nupkgFile,
            "-Source", nugetPushSource
            );
    }

    [Once] [Description("Open in Visual Studio")]
    public virtual async Task OpenInVisualStudio()
    {
        await Build();
        Process.Start(new ProcessStartInfo
        {
            FileName = Path.GetFullPath(SlnFile),
            UseShellExecute = true
        });
    }
	
    [Once][Description("Test")][Default]
    public virtual async Task Default()
	{
		await Test();
	}

    [Once][Description("Build a release version and push to nuget.org")]
    public virtual async Task Release()
    {
        await Git.EnsureNoPendingChanges();
        var v = await new Amg.Build.Builtin.Git().GetVersion();
        Logger.Information("Tagging with {version}", v.MajorMinorPatch);
        var tagResult = await Git.GitTool.DoNotCheckExitCode().Run("tag", v.MajorMinorPatch);
        if (!(tagResult.ExitCode == 0 || tagResult.Error.Contains("already exists")))
        {
            throw new Exception("Cannot tag release version.");
        }
        await Push("https://api.nuget.org/v3/index.json");
    }
}

