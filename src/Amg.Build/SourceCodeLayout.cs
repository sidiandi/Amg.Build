using System;
using System.Linq;
using System.Threading.Tasks;

namespace Amg.Build
{
    class SourceCodeLayout
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string name;
        public string sourceFile;
        public string sourceDir;
        public string propsFile;
        public string csprojFile;
        public string cmdFile;
        public string dllFile;

        public async Task Fix()
        {
            await FixFile(cmdFile, BootstrapperText);
            await FixFile(propsFile, PropsText);
        }

        async Task FixFile(string file, string expected)
        {
            await file
                .EnsureParentDirectoryExists()
                .WriteAllTextIfChangedAsync(expected);
        }

        public async Task Check()
        {
            await CheckFile(cmdFile, BootstrapperText);
            await CheckFile(propsFile, PropsText);
            var csproj = await csprojFile.ReadAllTextAsync();
            var propsLine = @"<Import Project=""Amg.Build.props"" />";
            if (!csproj.Contains(propsLine))
            {
                Logger.Warning("{csprojFile} must containt {propsLine}", csprojFile, propsLine);
            }
        }

        async Task CheckFile(string file, string expected)
        {
            if (!string.Equals(await file.ReadAllTextAsync(), expected))
            {
                Logger.Warning("{cmdFile} does not have the expected contents. Use --fix to fix.", file);
            }
        }

        public string PropsText => @"<Project>
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp2.1</TargetFramework>
	<Configuration>Debug</Configuration>
	<AmgBuildVersion Condition=""$(AmgBuildVersion)==''"" >0.*</AmgBuildVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Amg.Build"" Version=""$(AmgBuildVersion)"" />
  </ItemGroup>
</Project>
";

        public string BootstrapperText => @"@echo off
rem Bootstrapper script of Amg.Build
rem Do not modify.
rem See https://github.com/sidiandi/Amg.Build
setlocal EnableDelayedExpansion
set buildDll=%~dp0%~n0\bin\Debug\netcoreapp2.1\build.dll
set exitCodeRebuildRequired=2
set exitCodeAssemblyNotFound=-2147450740

mkdir %buildDll%\.. 2>nul
echo startup time > %buildDll%.startup

if exist %buildDll% (
    dotnet %buildDll% %*
    set buildScriptExitCode=!errorlevel!
    if !buildScriptExitCode! equ %exitCodeRebuildRequired% (
        call :rebuild %*
    )
    if !buildScriptExitCode! equ %exitCodeAssemblyNotFound% (
        call :rebuild %*
    )
) else (
    call :rebuild %*
)
exit /b !buildScriptExitCode!
goto :eof

:rebuild
	echo Build script requires rebuild.
	dotnet run --force -vd --project %~dp0%~n0 -- --ignore-clean %*
	set buildScriptExitCode=!errorlevel!
	exit /b !buildScriptExitCode!
";

        public static SourceCodeLayout Get(Type targetsType)
        {
            return Get(targetsType.Assembly.Location);
        }

        /// <summary>
        /// Try to determine the source directory from which the assembly of targetType was built.
        /// </summary>
        /// build.cmd
        /// build\build.cs
        /// build\build.csproj
        /// build\bin\Debug\netcoreapp2.2\build.dll
        /// <returns></returns>
        public static SourceCodeLayout Get(string dllFile)
        {
            try
            {
                var sourceCodeLayout = new SourceCodeLayout
                {
                    dllFile = dllFile
                };
                sourceCodeLayout.name = sourceCodeLayout.dllFile.FileNameWithoutExtension();
                sourceCodeLayout.sourceDir = sourceCodeLayout.dllFile.Parent().Parent().Parent().Parent();
                sourceCodeLayout.sourceFile = sourceCodeLayout.sourceDir.Combine($"{sourceCodeLayout.name}.cs");
                sourceCodeLayout.csprojFile = sourceCodeLayout.sourceDir.Combine($"{sourceCodeLayout.name}.csproj");
                sourceCodeLayout.propsFile = sourceCodeLayout.sourceDir.Combine("Amg.Build.props");
                sourceCodeLayout.cmdFile = sourceCodeLayout.sourceDir.Parent().Combine($"{sourceCodeLayout.name}.cmd");

                var paths = new[] {
                    sourceCodeLayout.sourceDir,
                    sourceCodeLayout.sourceFile,
                    sourceCodeLayout.cmdFile,
                    sourceCodeLayout.csprojFile
                }.Select(path => new { path, exists = path.Exists() })
                .ToList();

                Logger.Debug("{@paths}", paths);
                var hasSources = paths.All(_ => _.exists);
                if (hasSources)
                {
                    Logger.Debug("sources: {@sourceCodeLayout}", sourceCodeLayout);
                }
                return hasSources ? sourceCodeLayout : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
