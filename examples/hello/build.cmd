@echo off
setlocal EnableDelayedExpansion
set buildDll=%~dp0build\bin\Debug\netcoreapp2.2\build.dll
set exitCodeRebuildRequired=2

echo startup time > %buildDll%.startup

if exist %buildDll% (
    dotnet %buildDll% %*
    set buildScriptExitCode=!errorlevel!
    if !buildScriptExitCode! equ %exitCodeRebuildRequired% (
        call :rebuild %*
    )
    exit !buildScriptExitCode!
) else (
    call :rebuild %*
)
goto :eof

:rebuild
    echo Building %buildDll%
    dotnet run --force -vd --project %~dp0build -- --ignore-clean %*
	set buildScriptExitCode=!errorlevel!
	exit !buildScriptExitCode!

