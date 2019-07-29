@echo off

set buildDll=%~dp0build\bin\Debug\netcoreapp2.2\build.dll

if exist %buildDll% (
    dotnet %buildDll%
    if %ERRORLEVEL% == 2 (
       call :rebuild
    )
) else (
    call :rebuild
)
goto :eof

:Rebuild
    echo Building %buildDll%
    dotnet run --force -vd --project %~dp0build -- %*
    goto :eof