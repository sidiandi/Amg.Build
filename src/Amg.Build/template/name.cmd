@echo off
rem Do not modify below this line
rem Bootstrapper script of https://github.com/sidiandi/Amg.Build
set name=%~n0
set dll=%~dp0%name%\bin\Debug\netcoreapp2.1\%name%.dll
set project=%~dp0%name%\%name%.csproj
if exist %dll% ( dotnet %dll% -- %* ) else ( dotnet run --project %project% - %* )
