using System.Threading.Tasks;

namespace Amg.Build
{
    class SourceCodeLayout
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string name;
        public string sourceFile;
        public string sourceDir;
        public string csprojFile;
        public string cmdFile;
        public string dllFile;

        public async Task Fix()
        {
            await FixFile(cmdFile, BootstrapperText);
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
        }

        async Task CheckFile(string file, string expected)
        {
            if (!((await file.ReadAllTextAsync()).Equals(expected)))
            {
                Logger.Warning("{cmdFile} does not have the expected contents. Use --fix to fix.", file);
            }
        }

        public string CsProjText => @"<Project Sdk=""Microsoft.NET.Sdk"">
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
set buildDll=%~dp0%~n0\bin\Debug\netcoreapp2.2\build.dll
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
    }
}
