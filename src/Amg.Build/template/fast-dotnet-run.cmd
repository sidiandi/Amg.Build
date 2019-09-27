setlocal EnableDelayedExpansion
set TargetFramework=netcoreapp2.1
set configuration=Debug
set exitCodeRebuildRequired=2
set exitCodeAssemblyNotFound=-2147450740

for %%i in (%~dp0*.csproj) do set name=%%~ni
set outputDir=%~dp0bin\%configuration%\%TargetFramework%
set dll=%outputDir%\%name%.dll
echo %dll%

mkdir %dll%\.. 2>nul
echo startup time > %dll%.startup

if exist %dll% (
    dotnet %dll% %*
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
    set csprojFile=%~dp0%name%.csproj
	dotnet run --force  --configuration=%configuration% --framework=%TargetFramework% --project %csprojFile% -- --ignore-clean %*
	set buildScriptExitCode=!errorlevel!
	exit /b !buildScriptExitCode!
