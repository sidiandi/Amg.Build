using System.Threading.Tasks;
using System;
using System.IO;
using Amg.Build;
using System.Diagnostics;

partial class BuildTargets : Targets
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

    Tool dotnet = new Dotnet().Tool().Result;

    Git git = new Git();

    Target Build => DefineTarget(async () =>
    {
        await WriteAssemblyInformationFile();
        await WriteVersionPropsFile();
        await dotnet.Run("build", SlnFile);
    });

    Target<string> WriteAssemblyInformationFile => DefineTarget(async () =>
    {
        var v = await git.GetVersion();
        return await CommonAssemblyInfoFile.WriteAllTextIfChangedAsync(
$@"// Generated. Changes will be lost.
[assembly: System.Reflection.AssemblyCopyright({copyright.Quote()})]
[assembly: System.Reflection.AssemblyCompany({company.Quote()})]
[assembly: System.Reflection.AssemblyProduct({productName.Quote()})]
[assembly: System.Reflection.AssemblyVersion({v.AssemblySemVer.Quote()})]
[assembly: System.Reflection.AssemblyFileVersion({v.AssemblySemFileVer.Quote()})]
[assembly: System.Reflection.AssemblyInformationalVersion({v.InformationalVersion.Quote()})]
");
    });

    Target<string> WriteVersionPropsFile => DefineTarget(async () =>
    {
        var v = await git.GetVersion();
        return await VersionPropsFile.WriteAllTextIfChangedAsync(
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
    <PropertyGroup>
        <VersionPrefix>{v.MajorMinorPatch}</VersionPrefix>
        <VersionSuffix>{v.NuGetPreReleaseTagV2}</VersionSuffix>
    </PropertyGroup>
</Project>

");
    });

	Target Test => DefineTarget(async () =>
    {
        await Build();
        await dotnet.Run("test", SlnFile, "--no-build");
    });

    Target<string> Pack => DefineTarget(async () =>
    {
        var version = (await git.GetVersion()).NuGetVersionV2;
        await Build();
        await dotnet.Run("pack",
            LibDir,
            "--configuration", configuration,
            "--no-build",
            "--include-source",
            "--include-symbols",
            "--output", PackagesDir.EnsureDirectoryExists()
            );

        return PackagesDir.Combine($"{name}.{version}.nupkg");
    });

    Target Push => DefineTarget(async () =>
    {
        await git.EnsureNoPendingChanges();
        await Task.WhenAll(Test(), Pack());
        var nupkgFile = await Pack();
        var nuget = new Tool("nuget.exe");
        await nuget.Run(
            "push",
            nupkgFile,
            "-Source", nugetPushSource,
            "-SymbolSource", nugetPushSymbolSource
            );
    });

    Target OpenInVisualStudio => DefineTarget(async () =>
    {
        await Build();
        Process.Start(new ProcessStartInfo
        {
            FileName = Path.GetFullPath(SlnFile),
            UseShellExecute = true
        });
    });
	
	Target Default => DefineTarget(async () =>
	{
		await Test();
	});
}

