@echo off
rem Do not modify below this line
rem Bootstrapper script of https://github.com/sidiandi/Amg.Build
set TargetFramework=netcoreapp3.0
set name=%~n0
set dll=%~dp0%name%\bin\Debug\%TargetFramework%\%name%.dll
set project=%~dp0%name%\%name%.csproj
if exist %dll% ( dotnet %dll% %* ) else ( dotnet run --project %project% - %* )
