@echo off
rem Do not modify below this line
rem Bootstrapper script of Amg.Build
rem See https://github.com/sidiandi/Amg.Build
rem 
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
