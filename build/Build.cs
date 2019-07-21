using System.Threading.Tasks;
using System;
using System.IO;
using Csa.Build;
using System.Diagnostics;

partial class BuildTargets : Csa.Build.Targets
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    string productName => name;
    string year => DateTime.UtcNow.ToString("yyyy");
    string copyright => $"Copyright (c) {company} {year}";
    string configuration => "Debug";

    string outDir => $"out/{configuration}";
    string assemblyInformationFile => $"{outDir}/CommonAssemblyInfo.cs";
    string versionPropsFile => $"{outDir}/Version.props";

    string slnFile => $"src/{name}.sln";
    string libDir => $"src/{name}";

    Dotnet dotnet = new Dotnet();

    Target<GitVersion.VersionVariables> GetVersion => DefineTarget(() =>
    {
        return Task.Factory.StartNew(() =>
        {
            var gitVersionExecuteCore = new GitVersion.ExecuteCore(new GitVersion.Helpers.FileSystem());
            string templateString = @"GitVersion: {message}";
            GitVersion.Logger.SetLoggers(
            _ => Logger.Debug(templateString, _),
            _ => Logger.Information(templateString, _),
            _ => Logger.Warning(templateString, _),
            _ => Logger.Error(templateString, _));
            if (!gitVersionExecuteCore.TryGetVersion(".", out var versionVariables, true, null))
            {
                throw new System.Exception("Cannot read version");
            }
            return versionVariables;
        }, TaskCreationOptions.LongRunning);
    });

    Target Build => DefineTarget(async () =>
    {
        await WriteAssemblyInformationFile();
        await WriteVersionPropsFile();
        await dotnet.Run("build", slnFile);
    });

    Target<string> WriteAssemblyInformationFile => DefineTarget(async () =>
    {
        var v = await GetVersion();
        return await assemblyInformationFile.WriteAllTextIfChangedAsync(
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
        var v = await GetVersion();
        return await versionPropsFile.WriteAllTextIfChangedAsync(
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
        await dotnet.Run("test", slnFile, "--no-build");
    });

    Target<string> Pack => DefineTarget(async () =>
    {
        var version = (await GetVersion()).NuGetVersionV2;
        await Build();
        await dotnet.Run("pack",
            libDir,
            "--configuration", configuration,
            "--no-build",
            "--include-source",
            "--include-symbols");
        return $"src/{name}/bin/{configuration}/{name}.{version}.nupkg";
    });

    Target Push => DefineTarget(async () =>
    {
        await Task.WhenAll(Test(), Pack());
        var nupkgFile = await Pack();
        await dotnet.Run("nuget", "push",
            nupkgFile,
            "-s", nugetPushSource,
            "-ss", nugetPushSymbolSource
            );
    });

    Target OpenInVisualStudio => DefineTarget(async () =>
    {
        await Build();
        Process.Start(new ProcessStartInfo
        {
            FileName = Path.GetFullPath(slnFile),
            UseShellExecute = true
        });
    });
	
	Target Default => DefineTarget(async () =>
	{
		await Test();
	});
}

